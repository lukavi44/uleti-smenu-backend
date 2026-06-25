using Core.Billing;
using Core.Models.Enums;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Stripe
{
    public class StripePaymentProvider : IPaymentProvider
    {
        private readonly StripeSettings _settings;
        private readonly IBillingCheckoutService _checkoutService;
        private readonly IBillingWebhookProcessor _webhookProcessor;

        public StripePaymentProvider(
            IOptions<StripeSettings> settings,
            IBillingCheckoutService checkoutService,
            IBillingWebhookProcessor webhookProcessor)
        {
            _settings = settings.Value;
            _checkoutService = checkoutService;
            _webhookProcessor = webhookProcessor;

            if (!string.IsNullOrWhiteSpace(_settings.SecretKey))
                StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public string ProviderName => "Stripe";
        public bool IsEnabled => _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.SecretKey);

        public async Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl)
        {
            if (!IsEnabled)
                return Result.Failure<string>("Stripe is not configured.");

            var contextResult = await _checkoutService.GetCheckoutContextAsync(employerId, planId);
            if (contextResult.IsFailure)
                return Result.Failure<string>(contextResult.Error);

            var context = contextResult.Value;
            var customerId = await EnsureStripeCustomerAsync(employerId, context.EmployerEmail, context.StripeCustomerId);

            var sessionOptions = new SessionCreateOptions
            {
                Customer = customerId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Mode = context.CheckoutMode,
                Metadata = new Dictionary<string, string>
                {
                    ["employerId"] = employerId.ToString(),
                    ["planId"] = planId.ToString(),
                    ["checkoutType"] = "subscription"
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = context.StripePriceId,
                        Quantity = 1
                    }
                ]
            };

            var session = await new SessionService().CreateAsync(sessionOptions);
            return Result.Success(session.Url ?? string.Empty);
        }

        public async Task<Result<string>> CreateWalletTopUpCheckoutSessionAsync(
            Guid employerId,
            decimal amount,
            string successUrl,
            string cancelUrl)
        {
            if (!IsEnabled)
                return Result.Failure<string>("Stripe is not configured.");

            var contextResult = await _checkoutService.GetWalletTopUpContextAsync(employerId, amount);
            if (contextResult.IsFailure)
                return Result.Failure<string>(contextResult.Error);

            var context = contextResult.Value;
            var customerId = await EnsureStripeCustomerAsync(employerId, context.EmployerEmail, context.StripeCustomerId);
            var currency = context.Currency.ToLowerInvariant();

            var sessionOptions = new SessionCreateOptions
            {
                Customer = customerId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Mode = "payment",
                Metadata = new Dictionary<string, string>
                {
                    ["employerId"] = employerId.ToString(),
                    ["checkoutType"] = "wallet_topup",
                    ["amount"] = context.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["currency"] = context.Currency
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = ToStripeUnitAmount(context.Amount, context.Currency),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Wallet top-up"
                            }
                        },
                        Quantity = 1
                    }
                ]
            };

            var session = await new SessionService().CreateAsync(sessionOptions);
            return Result.Success(session.Url ?? string.Empty);
        }

        public async Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl)
        {
            if (!IsEnabled)
                return Result.Failure<string>("Stripe is not configured.");

            var customerResult = await _checkoutService.GetStripeCustomerIdAsync(employerId);
            if (customerResult.IsFailure)
                return Result.Failure<string>(customerResult.Error);

            var portalSession = await new global::Stripe.BillingPortal.SessionService().CreateAsync(
                new global::Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = customerResult.Value,
                    ReturnUrl = returnUrl
                });

            return Result.Success(portalSession.Url);
        }

        public async Task<Result> HandleWebhookAsync(string jsonBody, string signatureHeader)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(_settings.WebhookSecret))
                return Result.Failure("Stripe webhooks are not configured.");

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(jsonBody, signatureHeader, _settings.WebhookSecret);
            }
            catch (Exception ex)
            {
                return Result.Failure($"Invalid Stripe webhook signature: {ex.Message}");
            }

            if (await _webhookProcessor.HasProcessedEventAsync(stripeEvent.Id))
                return Result.Success();

            var processResult = stripeEvent.Type switch
            {
                "checkout.session.completed" => await HandleCheckoutCompletedAsync(stripeEvent),
                "customer.subscription.created" or "customer.subscription.updated" => await HandleSubscriptionUpdatedAsync(stripeEvent),
                "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEvent),
                "invoice.payment_failed" => await HandleInvoicePaymentFailedAsync(stripeEvent),
                "invoice.payment_succeeded" => await HandleInvoicePaymentSucceededAsync(stripeEvent),
                _ => Result.Success()
            };

            if (processResult.IsFailure)
                return processResult;

            return await _webhookProcessor.RecordPaymentEventAsync(
                stripeEvent.Id,
                stripeEvent.Type,
                jsonBody);
        }

        public BillingStatus MapStripeSubscriptionStatus(string stripeStatus) => stripeStatus switch
        {
            "trialing" => BillingStatus.Trialing,
            "active" => BillingStatus.Active,
            "past_due" => BillingStatus.PastDue,
            "canceled" => BillingStatus.Canceled,
            "unpaid" or "incomplete_expired" => BillingStatus.Expired,
            "incomplete" => BillingStatus.Incomplete,
            _ => BillingStatus.Incomplete
        };

        private async Task<string> EnsureStripeCustomerAsync(Guid employerId, string email, string? existingCustomerId)
        {
            if (!string.IsNullOrWhiteSpace(existingCustomerId))
                return existingCustomerId;

            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string>
                {
                    ["employerId"] = employerId.ToString()
                }
            });

            await _checkoutService.RecordStripeCustomerIdAsync(employerId, customer.Id);
            return customer.Id;
        }

        private static long ToStripeUnitAmount(decimal amount, string currency)
        {
            var normalized = currency.ToUpperInvariant();
            if (normalized is "RSD" or "JPY" or "KRW" or "VND")
                return (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);

            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }

        private async Task<Result> HandleCheckoutCompletedAsync(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not Session session)
                return Result.Success();

            if (!session.Metadata.TryGetValue("employerId", out var employerIdRaw) ||
                !Guid.TryParse(employerIdRaw, out var employerId))
                return Result.Failure("Checkout session missing employerId metadata.");

            if (session.Metadata.TryGetValue("checkoutType", out var checkoutType) &&
                checkoutType == "wallet_topup")
            {
                if (!session.Metadata.TryGetValue("amount", out var amountRaw) ||
                    !decimal.TryParse(amountRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
                {
                    return Result.Failure("Checkout session missing wallet top-up amount metadata.");
                }

                return await _webhookProcessor.ProcessWalletTopUpCompletedAsync(
                    employerId,
                    amount,
                    session.Id,
                    session.CustomerId ?? string.Empty);
            }

            if (!session.Metadata.TryGetValue("planId", out var planIdRaw) ||
                !Guid.TryParse(planIdRaw, out var planId))
                return Result.Failure("Checkout session missing planId metadata.");

            DateTime? periodEnd = null;
            string? priceId = null;

            if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
            {
                var subscription = await new SubscriptionService().GetAsync(session.SubscriptionId);
                periodEnd = subscription.CurrentPeriodEnd;
                priceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id;
            }

            return await _webhookProcessor.ProcessCheckoutCompletedAsync(
                employerId,
                planId,
                session.CustomerId ?? string.Empty,
                session.SubscriptionId,
                priceId,
                periodEnd);
        }

        private async Task<Result> HandleSubscriptionUpdatedAsync(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not global::Stripe.Subscription subscription)
                return Result.Success();

            var status = MapStripeSubscriptionStatus(subscription.Status);
            var priceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id;

            return await _webhookProcessor.ProcessSubscriptionUpdatedAsync(
                subscription.Id,
                status,
                subscription.CurrentPeriodEnd,
                priceId);
        }

        private async Task<Result> HandleSubscriptionDeletedAsync(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not global::Stripe.Subscription subscription)
                return Result.Success();

            return await _webhookProcessor.ProcessSubscriptionDeletedAsync(subscription.Id);
        }

        private async Task<Result> HandleInvoicePaymentFailedAsync(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not Invoice invoice || string.IsNullOrWhiteSpace(invoice.SubscriptionId))
                return Result.Success();

            return await _webhookProcessor.ProcessInvoicePaymentFailedAsync(invoice.SubscriptionId);
        }

        private async Task<Result> HandleInvoicePaymentSucceededAsync(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not Invoice invoice || string.IsNullOrWhiteSpace(invoice.SubscriptionId))
                return Result.Success();

            return await _webhookProcessor.ProcessInvoicePaymentSucceededAsync(invoice.SubscriptionId);
        }
    }
}

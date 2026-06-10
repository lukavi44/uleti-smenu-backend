using Core.Models.Enums;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    /// <summary>
    /// Fallback provider when Stripe is not configured. Replaced by StripePaymentProvider when enabled.
    /// </summary>
    public class DisabledPaymentProvider : IPaymentProvider
    {
        public string ProviderName => "None";
        public bool IsEnabled => false;

        public Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl) =>
            Task.FromResult(Result.Failure<string>("Online payments are not configured."));

        public Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl) =>
            Task.FromResult(Result.Failure<string>("Online payments are not configured."));

        public Task<Result> HandleWebhookAsync(string jsonBody, string signatureHeader) =>
            Task.FromResult(Result.Failure("Payment webhooks are not configured."));

        public BillingStatus MapStripeSubscriptionStatus(string stripeStatus) => BillingStatus.Incomplete;
    }
}

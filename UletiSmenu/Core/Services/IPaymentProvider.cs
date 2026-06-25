using CSharpFunctionalExtensions;
using Core.Models.Enums;

namespace Core.Services
{
    public interface IPaymentProvider
    {
        string ProviderName { get; }
        bool IsEnabled { get; }

        Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl);

        Task<Result<string>> CreateWalletTopUpCheckoutSessionAsync(
            Guid employerId,
            decimal amount,
            string successUrl,
            string cancelUrl);

        Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl);

        Task<Result> HandleWebhookAsync(string jsonBody, string signatureHeader);

        BillingStatus MapStripeSubscriptionStatus(string stripeStatus);
    }
}

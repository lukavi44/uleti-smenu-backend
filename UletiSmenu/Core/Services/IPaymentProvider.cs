using CSharpFunctionalExtensions;

namespace Core.Services
{
    /// <summary>
    /// Payment provider abstraction. Stripe (or another provider) implements this interface.
    /// Not wired yet — see docs/PAYMENT_PROPOSAL.md.
    /// </summary>
    public interface IPaymentProvider
    {
        Task<Result<string>> CreateCheckoutSessionAsync(
            Guid employerId,
            Guid planId,
            string successUrl,
            string cancelUrl);

        Task<Result<string>> CreateCustomerPortalSessionAsync(Guid employerId, string returnUrl);

        Task<Result> HandleWebhookAsync(string jsonBody, string signatureHeader);
    }
}

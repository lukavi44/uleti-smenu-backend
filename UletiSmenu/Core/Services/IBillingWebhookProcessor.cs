using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IBillingWebhookProcessor
    {
        Task<Result> ProcessCheckoutCompletedAsync(
            Guid employerId,
            Guid planId,
            string customerId,
            string? subscriptionId,
            string? priceId,
            DateTime? periodEndUtc);

        Task<Result> ProcessWalletTopUpCompletedAsync(
            Guid employerId,
            decimal amount,
            string checkoutSessionId,
            string customerId);

        Task<Result> ProcessSubscriptionUpdatedAsync(
            string subscriptionId,
            BillingStatus status,
            DateTime? periodEndUtc,
            string? priceId);

        Task<Result> ProcessSubscriptionDeletedAsync(string subscriptionId);
        Task<Result> ProcessInvoicePaymentFailedAsync(string subscriptionId);
        Task<Result> ProcessInvoicePaymentSucceededAsync(string subscriptionId);
        Task<Result> RecordPaymentEventAsync(string providerEventId, string eventType, string payload);
        Task<bool> HasProcessedEventAsync(string providerEventId);
    }
}

namespace Core.Models.Entities
{
    public class PaymentEvent
    {
        public Guid Id { get; private set; }
        public string ProviderEventId { get; private set; } = string.Empty;
        public string EventType { get; private set; } = string.Empty;
        public string Payload { get; private set; } = string.Empty;
        public DateTime ProcessedAtUtc { get; private set; }

        private PaymentEvent() { }

        public static PaymentEvent Create(Guid id, string providerEventId, string eventType, string payload, DateTime processedAtUtc)
        {
            return new PaymentEvent
            {
                Id = id,
                ProviderEventId = providerEventId,
                EventType = eventType,
                Payload = payload,
                ProcessedAtUtc = processedAtUtc
            };
        }
    }
}

namespace RestaurantService.Model
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty; // F.eks. "OrderAccepted"
        public string Payload { get; set; } = string.Empty; // Selve JSON beskeden
        public DateTime CreatedAt { get; set; }
        public bool IsProcessed { get; set; }

        public OutboxMessage(Guid id, string eventType, string payload, DateTime createdAt, bool isProcessed)
        {
            Id = id;
            EventType = eventType;
            Payload = payload;
            CreatedAt = createdAt;
            IsProcessed = isProcessed;
        }

        public OutboxMessage() { }
    }
}

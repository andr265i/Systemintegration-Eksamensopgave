namespace OrderService.Model
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } // F.eks. "OrderPlacedEvent"
        public string Payload { get; set; }   // Selve beskeden som JSON-tekst
        public bool IsProcessed { get; set; } // Er den sendt til RabbitMQ endnu?
        public DateTime CreatedAt { get; set; }

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

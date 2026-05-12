namespace Shared
{
    // 1. Sendes når kunden har betalt/bestilt
    public class OrderPlacedEvent
    {
        public Guid OrderId { get; set; }
        public DateTime Timestamp { get; set; }
        public int RestaurantId { get; set; } //Kunne være GUID men for enkelhedens skyld bruger vi en int

        public OrderPlacedEvent(Guid orderId, DateTime timestamp, int restaurantId)
        {
            OrderId = orderId;
            Timestamp = timestamp;
            RestaurantId = restaurantId;
        }

        public OrderPlacedEvent() { }
    }

}

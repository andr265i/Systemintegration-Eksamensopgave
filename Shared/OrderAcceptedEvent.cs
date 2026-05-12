namespace Shared
{
    // Dette event skal sendes, når restauranten accepterer ordren.
    public class OrderAcceptedEvent
    {
        public Guid OrderId { get; set; }
        public int RestaurantId { get; set; }
        public DateTime AcceptedAt { get; set; }

        public OrderAcceptedEvent(Guid orderId, DateTime acceptedAt, int restaurantId)
        {
            OrderId = orderId;
            AcceptedAt = acceptedAt;
            RestaurantId = restaurantId;
        }
        public OrderAcceptedEvent() { }
    }

}

namespace Shared
{
    // Dette event skal sendes, når restauranten accepterer ordren.
    public class OrderAcceptedEvent
    {
        public Guid OrderId { get; set; }
        public int RestaurantId { get; set; }
        public DateTime AcceptedAt { get; set; }
        public string ZipCode { get; set; }

        public OrderAcceptedEvent(Guid orderId, DateTime acceptedAt, int restaurantId, string zipCode)
        {
            OrderId = orderId;
            AcceptedAt = acceptedAt;
            RestaurantId = restaurantId;
            ZipCode = zipCode;
        }
        public OrderAcceptedEvent() { }
    }

}

namespace RestaurantService.Model
{
    public class RestaurantOrder
    {
        public Guid Id { get; set; } // Skal matche OrderId fra kunden
        public string Status { get; set; } = "Received";
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public RestaurantOrder(Guid id, string status, DateTime receivedAt)
        {
            Id = id;
            Status = status;
            ReceivedAt = receivedAt;
        }

        public RestaurantOrder()
        {
        }
    }
}

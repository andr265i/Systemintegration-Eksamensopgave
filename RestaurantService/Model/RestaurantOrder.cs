namespace RestaurantService.Model
{
    public class RestaurantOrder
    {
        public Guid Id { get; set; } // Skal matche OrderId fra kunden
        public string Status { get; set; } = "Received";
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public int ResturantId { get; set; } // For at kunne filtrere på restaurant

        public RestaurantOrder(Guid id, string status, DateTime receivedAt, int resturantId)
        {
            Id = id;
            Status = status;
            ReceivedAt = receivedAt;
            ResturantId = resturantId;
        }

        public RestaurantOrder()
        {
        }
    }
}

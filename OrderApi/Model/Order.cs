namespace OrderService.Model
{
    public class Order
    {
        public Guid Id { get; set; }
        public string Status { get; set; } // F.eks. "Pending", "Accepted"
        public DateTime CreatedAt { get; set; }

        public Order(Guid id, string status, DateTime createdAt)
        {
            Id = id;
            Status = status;
            CreatedAt = createdAt;
        }

        public Order() { }
    }


}

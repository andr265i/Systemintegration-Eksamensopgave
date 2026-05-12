namespace Shared
{
    // 3. Sendes når et bud har vundet "først-til-mølle" ræset
    public class DeliveryAssignedEvent
    {
        public Guid OrderId { get; set; }
        public Guid CourierId { get; set; } // Det vindende bud
        public DateTime Timestamp { get; set; }
    }
}

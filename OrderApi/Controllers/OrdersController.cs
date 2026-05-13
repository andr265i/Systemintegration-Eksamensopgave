using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Model;
using Shared;


namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _dbContext;

        // Dependency Injection: Vi får DbContext serveret af systemet
        public OrdersController(OrderDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] int restaurantId)
        {
            // 1. Opret den nye ordre (Entiteten)
            var orderId = Guid.NewGuid();
            var newOrder = new Order(orderId, "Pending", DateTime.UtcNow);

            // 2. Opret hændelsen (Eventet), som skal sendes til andre systemer
            var orderEvent = new OrderPlacedEvent(orderId, DateTime.UtcNow, restaurantId);


            // 3. Gør beskeden klar til Outbox (Konverter til JSON), nameof(OrderPlacedEvent) Giver teksten "OrderPlacedEvent"
            var outboxMessage = new OutboxMessage(Guid.NewGuid(), nameof(OrderPlacedEvent), System.Text.Json.JsonSerializer.Serialize(orderEvent), DateTime.UtcNow, false);

            // 4. Tilføj BÅDE ordren og beskeden til databasen
            _dbContext.Orders.Add(newOrder);
            _dbContext.OutboxMessages.Add(outboxMessage);

            // 5. Gem i én samlet transaktion (Data Consistency)
            // Hvis strømmen går præcis her, gemmes INGEN af dem. 
            // Vi undgår at have en ordre uden en besked, eller en besked uden en ordre.
            await _dbContext.SaveChangesAsync();

            return Accepted(new { Message = "Order received", OrderId = orderId });
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderStatus(Guid orderId)
        {
            // Slå ordren op i databasen
            var order = await _dbContext.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound("Ordren findes ikke.");
            }

            // Returner ordrens aktuelle status til kunden
            return Ok(new
            {
                OrderId = order.Id,
                Status = order.Status,
            });
        }
    }
}

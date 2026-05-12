using Microsoft.AspNetCore.Mvc;
using RestaurantService.Data;
using RestaurantService.Model;
using Shared;
using System.Text.Json;

namespace RestaurantService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly RestaurantDbContext _dbContext;

        public OrdersController(RestaurantDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("{orderId}/accept")]
        public async Task<IActionResult> AcceptOrder(Guid orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Ordren findes ikke.");
            if (order.Status != "Received") return BadRequest("Ordren kan ikke accepteres, da den ikke er i status 'Received'");

            // 1. Ændr status
            order.Status = "In Progress";

            // 2. Skab beskeden (vores "huskeseddel")
            var acceptedEvent = new OrderAcceptedEvent
            {
                OrderId = orderId,
                RestaurantId = 1, // I et ægte system ville dette være restaurantens ID
                AcceptedAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "OrderAcceptedEvent",
                Payload = JsonSerializer.Serialize(acceptedEvent),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            // 3. Tilføj til databasen i ÉN SAMLET TRANSAKTION
            _dbContext.OutboxMessages.Add(outboxMessage);

            // Nu gemmer vi både ordrens nye status og beskeden samtidig.
            // Hvis strømmen går lige før dette, sker der intet.
            // Hvis strømmen går lige efter dette, er vi sikre på at beskeden findes i databasen!
            await _dbContext.SaveChangesAsync();


            return Ok(new { Message = "Accepted" });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? status)
        {
            // Vi starter med at kigge på alle ordrer
            var query = _dbContext.Orders.AsQueryable();

            // Hvis der er blevet bedt om en specifik status (f.eks. ?status=Accepted), filtrerer vi listen
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Vi sorterer dem, så de ældste ordrer ligger øverst (First-in, First-out)
            var orders = query.OrderBy(o => o.ReceivedAt).ToList();

            return Ok(orders);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> AcceptOrder(Guid orderId, [FromQuery] int restaurantId, [FromQuery] string zipCode)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Ordren findes ikke.");

            // Hvis ordren ligger i databasen, men tilhører en anden restaurant, så afviser vi handlingen
            if (order.ResturantId != restaurantId)
            {
                // Status 403 Forbidden betyder: "Jeg ved godt hvem du er, men det der har du ikke lov til"
                return StatusCode(403, "Du har ikke rettigheder til at acceptere en anden restaurants ordre");
            }

            if (order.Status != "Received") return BadRequest("Ordren kan ikke accepteres, da den ikke er i status 'Received'");

            // 1. Ændr status
            //order.Status = "In Progress";
            order.Status = "Accepted";

            // 2. Skab beskeden (vores "huskeseddel")
            var acceptedEvent = new OrderAcceptedEvent
            {
                OrderId = orderId,
                RestaurantId = order.ResturantId,
                AcceptedAt = DateTime.UtcNow,
                ZipCode = zipCode
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
        public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int restaurantId)
        {
            // Vi starter med at kigge på alle ordrer
            var query = _dbContext.Orders.AsQueryable();

            query = query.Where(o => o.ResturantId == restaurantId);

            // Hvis der er blevet bedt om en specifik status (f.eks. ?status=Accepted), filtrerer vi listen
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Vi sorterer dem, så de ældste ordrer ligger øverst (First-in, First-out)
            //var orders = query.OrderBy(o => o.ReceivedAt).ToList();
            var orders = await query.OrderBy(o => o.ReceivedAt).ToListAsync();

            return Ok(orders);
        }
    }
}

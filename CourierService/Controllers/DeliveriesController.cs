using CourierService.Data;
using CourierService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using System.Text.Json;

namespace CourierService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveriesController : ControllerBase
    {
        private readonly CourierDbContext _dbContext;

        public DeliveriesController(CourierDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Et simpelt objekt til at modtage budets ID
        public class AcceptDeliveryRequest
        {
            public Guid CourierId { get; set; }
        }


        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptDelivery(Guid id, [FromBody] AcceptDeliveryRequest request)
        {
            var offer = await _dbContext.DeliveryOffers.FindAsync(id);

            // 1. Findes udbuddet?
            if (offer == null) return NotFound("Udbuddet findes ikke.");

            // 2. DOMMEREN TRÆDER IND: Er udbuddet stadig ledigt?
            if (offer.Status != "Free")
            {
                return BadRequest("Beklager! Et andet bud var hurtigere og har allerede taget opgaven.");
            }

            // 3. Budet var først! Vi låser opgaven til dem.
            offer.Status = "Taken";
            offer.AssignedCourierId = request.CourierId;

            var deliveryAssignedEvent = new DeliveryAssignedEvent
            {
                OrderId = id,
                CourierId = request.CourierId
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "DeliveryAssignedEvent",
                Payload = JsonSerializer.Serialize(deliveryAssignedEvent),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            _dbContext.OutboxMessages.Add(outboxMessage);

            // Gem ændringen. Hvis to bude rammer denne linje fuldstændig samtidig, 
            // vil databasen kun lade den første gemme succesfuldt.
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Beklager! Et andet bud var hurtigere og har allerede taget opgaven.");
            }

            return Ok(new
            {
                Message = "Tillykke, opgaven er din!",
                CourierId = request.CourierId
            });
        }

        [HttpGet]
        public IActionResult GetAvailableOffers([FromQuery] string? zipCode)
        {
            // En metode til appen, så den kan vise listen af ledige opgaver
            var query = _dbContext.DeliveryOffers.Where(o => o.Status == "Free");

            if (!string.IsNullOrEmpty(zipCode))
            {
                query = query.Where(o => o.ZipCode == zipCode);
            }

            var offers = query.ToList();
            return Ok(offers);
        }

    }
}

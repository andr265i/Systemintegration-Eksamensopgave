using CourierService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            // Gem ændringen. Hvis to bude rammer denne linje fuldstændig samtidig, 
            // vil databasen kun lade den første gemme succesfuldt.
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest("Beklager! Et andet bud var hurtigere og har allerede taget opgaven.");
            }

            return Ok(new
            {
                Message = "Tillykke, opgaven er din!",
                CourierId = request.CourierId
            });
        }

        [HttpGet]
        public IActionResult GetAvailableOffers()
        {
            // En metode til appen, så den kan vise listen af ledige opgaver
            var offers = _dbContext.DeliveryOffers.Where(o => o.Status == "Free").ToList();
            return Ok(offers);
        }

    }
}

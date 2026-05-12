using CourierService.Model;
using Microsoft.EntityFrameworkCore;

namespace CourierService.Data
{
    public class CourierDbContext : DbContext
    {
        public CourierDbContext(DbContextOptions<CourierDbContext> options) : base(options) { }
        public DbSet<DeliveryOffer> DeliveryOffers { get; set; }
    }
}

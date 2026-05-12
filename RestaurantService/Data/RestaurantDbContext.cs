using Microsoft.EntityFrameworkCore;
using RestaurantService.Model;

namespace RestaurantService.Data
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options) { }
        public DbSet<RestaurantOrder> Orders { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}

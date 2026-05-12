
using Microsoft.EntityFrameworkCore;
using RestaurantService.Data;
using RestaurantService.Workers;
using Scalar.AspNetCore;

namespace RestaurantService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<RestaurantDbContext>(options =>
            options.UseSqlite("Data Source=eaatRestaurantOrder.db"));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            //Services for listening to messages from the message bus and processing them
            builder.Services.AddHostedService<OrderListener>();
            // Services for publishing messages from the outbox table to the message bus
            builder.Services.AddHostedService<RestaurantOutboxPublisher>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                // Dette sikrer at tabellerne altid findes, når programmet starter!
                dbContext.Database.EnsureCreated();
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

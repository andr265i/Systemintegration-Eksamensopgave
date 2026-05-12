
using CourierService.Data;
using CourierService.Workers;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace CourierService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<CourierDbContext>(options =>
           options.UseSqlite("Data Source=eaatCourierOrder.db"));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Vores baggrundsarbejder, der lytter efter accepterede ordrer og opretter leveringsudbud
            builder.Services.AddHostedService<DeliveryOfferListener>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CourierDbContext>();
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

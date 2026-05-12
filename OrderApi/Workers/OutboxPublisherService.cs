using OrderService.Data;
using RabbitMQ.Client;
using System.Text;

namespace OrderService.Workers
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxPublisherService> _logger;

        // Vi injicerer IServiceProvider fordi en BackgroundService lever for evigt (Singleton),
        // men vores DbContext kun lever kortvarigt pr. request (Scoped).
        public OutboxPublisherService(IServiceProvider serviceProvider, ILogger<OutboxPublisherService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Publisher er startet...");

            // Opsætning af RabbitMQ forbindelse (til localhost under udvikling)
            var factory = new ConnectionFactory { HostName = "localhost" };

            // CreateConnectionAsync returnerer Task<IConnection>, så vi skal await'e den.
            // IConnection her har en CreateChannelAsync-metode (asynkron), så brug den i stedet for CreateModel.
            await using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Opretter en 'Direct' exchange. Den fungerer som en megafon: 
            // Den råber beskeden ud til alle de services, der gider lytte.
            await channel.ExchangeDeclareAsync(exchange: "eaat_events", type: ExchangeType.Direct);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Opret et midlertidigt "scope" til at hente databasen
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    // Find alle beskeder, der endnu ikke er sendt
                    var unsentMessages = dbContext.OutboxMessages
                        .Where(m => !m.IsProcessed)
                        .OrderBy(m => m.CreatedAt)
                        .ToList();

                    foreach (var message in unsentMessages)
                    {
                        try
                        {
                            //Konverter JSON - teksten til bytes, som RabbitMQ forstår
                            var body = Encoding.UTF8.GetBytes(message.Payload);
                            // Vi kan også parse JSON'en for at hente RestaurantId, hvis vi vil logge det eller bruge det i routing (hvis vi brugte en Direct exchange)
                            var jsonDoc = System.Text.Json.JsonDocument.Parse(message.Payload);
                            var restaurantId = jsonDoc.RootElement.GetProperty("RestaurantId").ToString();

                            // Publicer beskeden til RabbitMQ
                            await channel.BasicPublishAsync(
                                 exchange: "eaat_events",
                                 routingKey: restaurantId, // RoutingKey bruges til at sende til specifikke køer baseret på RestaurantId
                                 body: body,
                                 cancellationToken: stoppingToken);

                            // Marker beskeden som sendt og gem i databasen
                            message.IsProcessed = true;
                            await dbContext.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation($"Sendte event {message.EventType} for besked ID {message.Id}");
                        }
                        catch (Exception ex)
                        {
                            // Hvis RabbitMQ er nede, fanger vi fejlen her. 
                            // Beskeden forbliver 'IsProcessed = false', og systemet prøver igen om 5 sekunder!
                            _logger.LogError(ex, "Kunne ikke sende besked til RabbitMQ. Prøver igen senere.");
                        }
                    }
                }
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

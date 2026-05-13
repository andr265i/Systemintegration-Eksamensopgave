using OrderService.Data;
using RabbitMQ.Client;
using System.Text;

namespace OrderService.Workers
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxPublisherService> _logger;
        private readonly IConnection _connection;

        // Vi injicerer IServiceProvider fordi en BackgroundService lever for evigt (Singleton),
        // men vores DbContext kun lever kortvarigt pr. request (Scoped).
        public OutboxPublisherService(IServiceProvider serviceProvider, ILogger<OutboxPublisherService> logger, IConnection connection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _connection = connection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Publisher er startet...");

            // Opretter en kanal til RabbitMQ. Vi genbruger den, så vi ikke åbner og lukker forbindelsen hele tiden.
            using var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Opretter en 'Direct' exchange. Den fungerer som en megafon: 
            // Den råber beskeden ud til alle de services, der gider lytte.
            await channel.ExchangeDeclareAsync(exchange: "eaat_events", type: ExchangeType.Direct, cancellationToken: stoppingToken);

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
                            //var restaurantId = jsonDoc.RootElement.GetProperty("RestaurantId").ToString();

                            // Publicer beskeden til RabbitMQ
                            await channel.BasicPublishAsync(
                                 exchange: "eaat_events",
                                 routingKey: "new_restaurant_order", // Vi kan bruge routingKey til at sende beskeden til specifikke køer, hvis vi havde en Direct exchange
                                 body: body,
                                 cancellationToken: stoppingToken);

                            // Marker beskeden som sendt
                            message.IsProcessed = true;
                            _logger.LogInformation($"Sendte event {message.EventType} for besked ID {message.Id}");

                            // Gem ændringen i databasen, så vi ikke sender den igen
                            if (unsentMessages.Count != 0)
                            {
                                await dbContext.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation($"Opdaterede {unsentMessages.Count} beskeder som 'processed' i databasen.");
                            }
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

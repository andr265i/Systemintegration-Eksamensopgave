using CourierService.Data;
using RabbitMQ.Client;
using System.Text;

namespace CourierService.Workers
{
    public class CourierOutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CourierOutboxPublisherService> _logger;
        private readonly IConnection _connection;

        public CourierOutboxPublisherService(IServiceProvider serviceProvider, ILogger<CourierOutboxPublisherService> logger, IConnection connection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _connection = connection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Courier Outbox Publisher er startet...");

            using var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Vi opretter en NY megafon (Fanout) til at fortælle, at opgaven er taget
            await channel.ExchangeDeclareAsync("eaat_delivery_assigned", ExchangeType.Fanout, cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<CourierDbContext>();

                    // Find alle beskeder, der endnu ikke er sendt
                    var messages = dbContext.OutboxMessages
                        .Where(m => !m.IsProcessed)
                        .OrderBy(m => m.CreatedAt)
                        .ToList();

                    foreach (var message in messages)
                    {
                        var body = Encoding.UTF8.GetBytes(message.Payload);

                        // Skyd beskeden ud i den nye megafon!
                        await channel.BasicPublishAsync(
                            exchange: "eaat_delivery_assigned",
                            routingKey: "", // Fanout bruger ikke routing key
                            body: body,
                            cancellationToken: stoppingToken);

                        // Marker som sendt
                        message.IsProcessed = true;
                        _logger.LogInformation($"Sendte outbox besked {message.Id} fra CourierService til RabbitMQ.");

                    }
                    // Gem ændringen i databasen, så vi ikke sender den igen
                    if (messages.Count != 0)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Opdaterede {messages.Count} beskeder som 'processed' i databasen.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Fejl i CourierOutboxPublisher: {ex.Message}");
                }

                // Vent 5 sekunder, før vi tjekker databasen igen
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

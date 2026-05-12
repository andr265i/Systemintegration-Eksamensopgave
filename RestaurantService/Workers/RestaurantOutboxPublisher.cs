using RabbitMQ.Client;
using RestaurantService.Data;
using System.Text;

namespace RestaurantService.Workers
{
    public class RestaurantOutboxPublisher : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RestaurantOutboxPublisher> _logger;

        public RestaurantOutboxPublisher(IServiceProvider serviceProvider, ILogger<RestaurantOutboxPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Restaurant Outbox Publisher kører...");

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Deklarer megafonen
            await channel.ExchangeDeclareAsync("accepted_orders", ExchangeType.Fanout, cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                    // Hent op til 50 uafsendte beskeder
                    var unsentMessages = dbContext.OutboxMessages
                        .Where(m => !m.IsProcessed)
                        .OrderBy(m => m.CreatedAt)
                        .ToList();

                    foreach (var message in unsentMessages)
                    {
                        var body = Encoding.UTF8.GetBytes(message.Payload);

                        // Send ud på Fanout (Ingen routing key nødvendig)
                        await channel.BasicPublishAsync(
                            exchange: "accepted_orders",
                            routingKey: "",
                            body: body,
                            cancellationToken: stoppingToken);

                        // Marker som sendt
                        message.IsProcessed = true;
                    }

                    if (unsentMessages.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Sendte {unsentMessages.Count} outbox-beskeder ud på megafonen.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fejl i Restaurant Outbox Publisher. Prøver igen om lidt.");
                }

                // Vent 5 sekunder før den kigger i databasen igen
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

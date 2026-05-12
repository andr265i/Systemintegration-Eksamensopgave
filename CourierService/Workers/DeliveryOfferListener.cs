using CourierService.Data;
using CourierService.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System.Text;
using System.Text.Json;

namespace CourierService.Workers
{
    public class DeliveryOfferListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeliveryOfferListener> _logger;

        public DeliveryOfferListener(IServiceProvider serviceProvider, ILogger<DeliveryOfferListener> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Courier Listener lytter efter accepterede ordrer...");

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Forbind til den samme megafon som OrderService lytter på!
            await channel.ExchangeDeclareAsync("accepted_orders", ExchangeType.Fanout, cancellationToken: stoppingToken);

            // Budenes egen postkasse
            var queueName = "courier_offers_queue";
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(queueName, "accepted_orders", routingKey: "", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var acceptedEvent = JsonSerializer.Deserialize<OrderAcceptedEvent>(message);

                if (acceptedEvent != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<CourierDbContext>();

                    // Idempotent: Tjek om vi allerede har oprettet dette udbud
                    if (!dbContext.DeliveryOffers.Any(d => d.Id == acceptedEvent.OrderId))
                    {
                        var newOffer = new DeliveryOffer { Id = acceptedEvent.OrderId };
                        dbContext.DeliveryOffers.Add(newOffer);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"Nyt leveringsudbud Ordre: {acceptedEvent.OrderId}");
                    }
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };

            await channel.BasicConsumeAsync(queueName, false, consumer, stoppingToken);
            await Task.Delay(-1, stoppingToken);
        }
    }
}

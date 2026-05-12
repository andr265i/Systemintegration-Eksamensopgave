
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestaurantService.Data;
using RestaurantService.Model;
using Shared;
using System.Text;
using System.Text.Json;

namespace RestaurantService.Workers;

public class OrderListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderListener> _logger;
    private readonly string _myRestaurantId = "1"; // Eksempel ID

    public OrderListener(IServiceProvider serviceProvider, ILogger<OrderListener> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync("eaat_events", ExchangeType.Direct, cancellationToken: stoppingToken);
        var queueName = "restaurant_orders_queue";
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        // Bind kun til ordrer med vores ID
        await channel.QueueBindAsync(queueName, "eaat_events", routingKey: _myRestaurantId, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(message);


            if (orderEvent != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                dbContext.Orders.Add(new RestaurantOrder { Id = orderEvent.OrderId });
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Ordre {orderEvent.OrderId} modtaget og gemt!");
            }
            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await channel.BasicConsumeAsync(queueName, false, consumer, stoppingToken);
        await Task.Delay(-1, stoppingToken);
    }
}

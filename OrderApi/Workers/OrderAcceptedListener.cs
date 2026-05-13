using OrderService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System.Text;
using System.Text.Json;

namespace OrderService.Workers;

public class OrderAcceptedListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderAcceptedListener> _logger;
    private readonly IConnection _connection;

    // Vi injicerer IServiceProvider fordi en BackgroundService lever for evigt (Singleton),
    // men vores DbContext kun lever kortvarigt pr. request (Scoped).
    public OrderAcceptedListener(IServiceProvider serviceProvider, ILogger<OrderAcceptedListener> logger, IConnection connection)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderService lytter nu efter accepterede ordrer...");

        using var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Vi lytter på den FÆLLES megafon for accepterede ordrer (Model A: Fanout)
        await channel.ExchangeDeclareAsync("accepted_orders", ExchangeType.Fanout, cancellationToken: stoppingToken);

        // OrderService's egen postkasse
        var queueName = "order_service_updates_queue";
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(queueName, "accepted_orders", routingKey: "", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messagePayload = Encoding.UTF8.GetString(body);

            try
            {
                var acceptedEvent = JsonSerializer.Deserialize<OrderAcceptedEvent>(messagePayload);

                if (acceptedEvent != null)
                {
                    // 1. Find kundens ordre i OrderDB og opdater status
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    var order = await dbContext.Orders.FindAsync(acceptedEvent.OrderId, stoppingToken);
                    if (order != null)
                    {
                        order.Status = "Accepted"; // Kunden kan nu se dette i appen!
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation($"Kunde-ordre {order.Id} er opdateret til 'Accepted'!");
                    }
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved opdatering af kundens ordre");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queueName, false, consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
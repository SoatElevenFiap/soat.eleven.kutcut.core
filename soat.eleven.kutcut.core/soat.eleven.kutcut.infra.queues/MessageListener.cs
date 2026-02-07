using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using soat.eleven.kutcut.infra.queues.Interfaces;
using System.Text;

namespace soat.eleven.kutcut.infra.queues
{
    public class MessageListener : IMessageListener, IDisposable
    {
        private readonly ILogger<MessageListener> _logger;
        private readonly RabbitMQConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _consumerTag;

        public MessageListener(
            ILogger<MessageListener> logger,
            RabbitMQConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public void StartListening(string queueName, Func<string, Task> onMessageReceived)
        {
            try
            {
                _logger.LogInformation("Starting to listen on queue: {QueueName}", queueName);

                // Create connection and channel synchronously for simplicity
                _connection = _connectionFactory.GetConnectionAsync().Result;
                _channel = _connectionFactory.CreateChannelAsync().Result;

                // Declare queue (idempotent)
                _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null).Wait();

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        
                        _logger.LogInformation("Message received from queue {QueueName}: {Message}", 
                            queueName, message);

                        await onMessageReceived(message);

                        // Manual acknowledgment after successful processing
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                        
                        _logger.LogInformation("Message processed and acknowledged successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                        
                        // Reject message and requeue on error
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _consumerTag = _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer).Result;

                _logger.LogInformation("Successfully started listening on queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start listening on queue {QueueName}", queueName);
                throw;
            }
        }

        public void StopListening()
        {
            try
            {
                if (_consumerTag != null && _channel != null)
                {
                    _channel.BasicCancelAsync(_consumerTag).Wait();
                    _logger.LogInformation("Stopped listening on queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping listener");
            }
        }

        public void Dispose()
        {
            StopListening();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}

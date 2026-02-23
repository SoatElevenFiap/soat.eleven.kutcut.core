using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using soat.eleven.kutcut.infra.queues.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace soat.eleven.kutcut.infra.queues
{
    [ExcludeFromCodeCoverage(Justification = "Requires live RabbitMQ broker — covered by integration tests")]
    public class MessageSender : IMessageSender
    {
        private readonly ILogger<MessageSender> _logger;
        private readonly RabbitMQConnectionFactory _connectionFactory;

        public MessageSender(
            ILogger<MessageSender> logger,
            RabbitMQConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async Task SendMessage<Message>(string queueName, Message message)
        {
            IChannel? channel = null;

            try
            {
                _logger.LogInformation("Sending message to queue: {QueueName}", queueName);

                channel = await _connectionFactory.CreateChannelAsync();

                // Declare queue (idempotent)
                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Serialize message to JSON
                var jsonMessage = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                // Configure message properties
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json"
                };

                // Publish message
                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Message sent successfully to queue {QueueName}: {Message}", 
                    queueName, jsonMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to queue {QueueName}", queueName);
                throw;
            }
            finally
            {
                if (channel != null)
                {
                    await channel.CloseAsync();
                    channel.Dispose();
                }
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using soat.eleven.kutcut.infra.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace soat.eleven.kutcut.infra.queues
{
    [ExcludeFromCodeCoverage(Justification = "Requires live RabbitMQ broker — covered by integration tests")]
    public class RabbitMQConnectionFactory : IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQConnectionFactory> _logger;
        private IConnection? _connection;

        public RabbitMQConnectionFactory(
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQConnectionFactory> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password
                };

                _connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("RabbitMQ connection created successfully to {HostName}:{Port}", 
                    _settings.HostName, _settings.Port);
            }

            return _connection;
        }

        public async Task<IChannel> CreateChannelAsync()
        {
            var connection = await GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            
            _logger.LogInformation("RabbitMQ channel created successfully");
            
            return channel;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}

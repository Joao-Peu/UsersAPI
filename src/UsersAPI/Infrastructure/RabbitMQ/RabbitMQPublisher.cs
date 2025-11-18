using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace UsersAPI.Infrastructure
{
    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly RabbitMQ.RabbitMQSettings _settings;
        private readonly IConnection _connection;

        public RabbitMQPublisher(IOptions<RabbitMQ.RabbitMQSettings> options)
        {
            _settings = options.Value;
            var factory = new ConnectionFactory() { HostName = _settings.HostName, UserName = _settings.UserName, Password = _settings.Password };
            _connection = factory.CreateConnection();
        }

        public void Publish(string routingKey, string message)
        {
            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Topic, true);
            var body = Encoding.UTF8.GetBytes(message);
            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2;
            channel.BasicPublish(_settings.Exchange, routingKey, props, body);
        }
    }
}

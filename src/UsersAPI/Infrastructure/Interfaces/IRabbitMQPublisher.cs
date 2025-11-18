namespace UsersAPI.Infrastructure
{
    public interface IRabbitMQPublisher
    {
        void Publish(string routingKey, string message);
    }
}

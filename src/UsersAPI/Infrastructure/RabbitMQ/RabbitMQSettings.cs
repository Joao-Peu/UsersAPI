namespace UsersAPI.Infrastructure.RabbitMQ
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "users.exchange";
        public string UserCreatedRoutingKey { get; set; } = "user.created";
    }
}



using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Repositories.Model.AdminModels;
using Repositories.Model.chat;

namespace API.Services
{
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName = "task_notifications";

        public RabbitMQService(IConfiguration configuration)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    UserName = configuration["RabbitMQ:Username"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare the exchange (no need for a single queue anymore)
                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true);

                Console.WriteLine("RabbitMQ connection established");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing RabbitMQ: {ex.Message}");
                throw;
            }
        }

        public void PublishNotification(NotificationModel notification)
        {
            try
            {
                // Use c_related_user_id as the routing key to target the specific user's queue
                string routingKey = $"user_{notification.c_related_user_id}";

                // Declare a queue for this user dynamically (if it doesn’t exist)
                string queueName = $"{routingKey}_notifications";
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queueName, _exchangeName, routingKey);

                var message = JsonSerializer.Serialize(notification);
                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body);

                Console.WriteLine($"Published notification to {queueName}: {notification.c_message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing message to RabbitMQ: {ex.Message}");
            }
        }

            public void PublishNotificationUser(NotificationModel notification)
        {
            try
            {
                // Use c_related_user_id as the routing key to target the specific user's queue
                string routingKey = $"user_{notification.c_related_user_id}";

                // Declare a queue for this user dynamically (if it doesn’t exist)
                string queueName = $"{routingKey}_notifications";
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queueName, _exchangeName, routingKey);

                var message = JsonSerializer.Serialize(notification);
                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body);

                Console.WriteLine($"Published notification to {queueName}: {notification.c_message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing message to RabbitMQ: {ex.Message}");
            }
        }



        public void PublishChatMessage(ChatMessage message)
        {
            try
            {
                string routingKey = $"user_{message.c_recipient_id}";
                string queueName = $"chat_{routingKey}";
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queueName, _exchangeName, routingKey);

                var messageJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageJson);
                _channel.BasicPublish(_exchangeName, routingKey, null, body);
                Console.WriteLine($"Published chat message to {queueName}: {message.c_content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing chat message: {ex.Message}");
            }
        }


        public IModel CreateConsumerChannel()
        {
            return _connection.CreateModel(); // For consumers to use
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
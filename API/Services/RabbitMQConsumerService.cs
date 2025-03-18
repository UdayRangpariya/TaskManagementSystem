

using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Model.AdminModels;

namespace API.Services
{
    public class RabbitMQConsumerService 
   // : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMQService _rabbitMQService;

        public RabbitMQConsumerService(IServiceProvider serviceProvider, RabbitMQService rabbitMQService)
        {
            _serviceProvider = serviceProvider;
            _rabbitMQService = rabbitMQService;
        }

     
        public void StartConsumerForUser(int userId)
        {
            var channel = _rabbitMQService.CreateConsumerChannel();
            var queueName = $"user_{userId}_notifications";

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(queueName, "task_notifications", $"user_{userId}");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var notification = JsonSerializer.Deserialize<NotificationModel>(message);

                // Log receipt but don’t store in Redis yet
                Console.WriteLine($"Received notification for user {userId}: {notification.c_message}");

                // Don’t acknowledge yet; let the API handle consumption
                // channel.BasicAck(ea.DeliveryTag, multiple: false); // Removed
            };

            channel.BasicConsume(queueName, autoAck: false, consumer); // Manual ACK
            Console.WriteLine($"Started consumer for {queueName}");
        }
    }
}
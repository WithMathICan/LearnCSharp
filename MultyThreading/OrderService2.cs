using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace MultyThreading {

    /*Какие проблемы возникнут при использовании этого кода в многопоточной среде? Учти работу с RabbitMQ и счётчик.
Отрефактори этот код, чтобы он стал потокобезопасным. Используй подходящий механизм синхронизации и объясни, почему ты его выбрал.
Ловушка: Как создание IModel (channel) в каждом вызове ProcessOrder влияет на производительность? Как бы ты оптимизировал работу с RabbitMQ?*/
    public class OrderService2 {
        //private readonly IConnection _connection;
        private readonly IConnectionFactory _connectionFactory;
        private int _orderCount = 0;

        public OrderService2(IConnectionFactory connectionFactory) {
            _connectionFactory = connectionFactory;
        }

        public void ProcessOrder(string order) {
            var _connection = _connectionFactory.CreateConnection();
            using var channel = _connection.CreateModel();
            channel.QueueDeclare("orders", durable: true, exclusive: false, autoDelete: false);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order));
            channel.BasicPublish(exchange: "", routingKey: "orders", body: body);
            _orderCount++;
        }

        public int GetOrderCount() {
            return _orderCount;
        }
    }

    public class OrderService2_Refactored : IDisposable {
        private readonly IConnection _connection;
        private readonly RabbitMQ.Client.IModel _channel;
        private int _orderCount = 0;

        public OrderService2_Refactored(IConnectionFactory connectionFactory) {
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare("orders", durable: true, exclusive: false, autoDelete: false);
        }

        public async Task ProcessOrderAsync(string order) {
            if (string.IsNullOrEmpty(order))
                throw new ArgumentNullException(nameof(order));

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order));
            await Task.Run(() => _channel.BasicPublish(exchange: "", routingKey: "orders", body: body));
            Interlocked.Increment(ref _orderCount);
        }

        public Task<int> GetOrderCountAsync() {
            return Task.FromResult(Volatile.Read(ref _orderCount));
        }

        public void Dispose() {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}

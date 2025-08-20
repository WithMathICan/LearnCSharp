using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RabbitMQ.Client;
using System.Text.Json;

namespace MultyThreading {

    /*Какие проблемы возникнут при использовании этого кода в многопоточной среде? Учти работу с SQLite и RabbitMQ.
    Отрефактори этот код, чтобы он стал потокобезопасным и оптимизированным. 
    Используй подходящие механизмы синхронизации и асинхронные API. Объясни свои решения.
    Ловушка: Как SQLite обрабатывает одновременные записи в базу данных? Как это влияет на твой рефакторинг?*/
    public class OrderPersistenceService {
        private readonly string _connectionString = "Data Source=orders.db";
        private readonly IConnection _rabbitConnection;
        private int _processedOrders = 0;

        public OrderPersistenceService(IConnectionFactory connectionFactory) {
            // Создаём соединение с RabbitMQ
            _rabbitConnection = connectionFactory.CreateConnection();
            // Инициализация базы данных SQLite
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS Orders (Id INTEGER PRIMARY KEY AUTOINCREMENT, Description TEXT)";
            command.ExecuteNonQuery();
        }

        public void SaveAndPublishOrder(string description) {
            using var channel = _rabbitConnection.CreateModel();
            channel.QueueDeclare("orders", durable: true, exclusive: false, autoDelete: false);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(description));

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Orders (Description) VALUES ($description)";
            command.Parameters.AddWithValue("$description", description);
            command.ExecuteNonQuery();

            channel.BasicPublish(exchange: "", routingKey: "orders", body: body);
            _processedOrders++;
        }

        public int GetProcessedOrders() {
            return _processedOrders;
        }
    }
}

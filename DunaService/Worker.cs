using System.Security.Cryptography;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DunaService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly EventingBasicConsumer consumer;

    private readonly MongoClient client;
    private IMongoDatabase database;
    private IMongoCollection<BsonDocument> collection;

    public Worker(ILogger<Worker> logger)
    {
        var mongo_address = Environment.GetEnvironmentVariable("MONGO_URL");
        var hostname = Environment.GetEnvironmentVariable("HOSTNAME");
        var username = Environment.GetEnvironmentVariable("USER");
        var password = Environment.GetEnvironmentVariable("PASSWORD");
        if (mongo_address == null || hostname == null || username == null || password == null)
        {
            Console.WriteLine("Установите параметры командной строки");
            return;
        }

        client = new MongoClient(mongo_address);
        database = client.GetDatabase("duna");
        collection = database.GetCollection<BsonDocument>("files");

        ConnectionFactory factory = new ConnectionFactory
            { HostName = hostname, UserName = username, Password = password };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        _logger = logger;

        channel.QueueDeclare(queue: "rpc_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        consumer = new EventingBasicConsumer(channel);
        consumer.Received += Received;
    }

    private void Received(object? model, BasicDeliverEventArgs? ea)
    {
        // if (ea == null) return;
        var props = ea.BasicProperties;
        var replyProps = channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;

        var body = ea.Body.ToArray();
        // 8 байта - вес файла в байтах, остальное - имя файла и сам файл
        var weight = BitConverter.ToInt32(body[..8]);
        var name = Encoding.UTF8.GetString(body[8..^weight]);
        var file = body[^weight..];

        // пробросить всё содержимое сообщения через хэш-функцию, таким образом сгенерировать новое имя файла (токен) в 64 символа
        // сохранить файл в папку /files
        // добавить запись в базу данных такого содержания:
        // ip, название файла, токен, размер, UNIX-время, счётчик до удаления (24)

        var token = Hash(body);
        SaveFile(token, file);
        SaveToDatabase(name, token, weight);
        var responseBytes = Encoding.UTF8.GetBytes(token);
        // Console.WriteLine(name);
        channel.BasicPublish(exchange: string.Empty,
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            body: responseBytes);
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }


    private string Hash(byte[] data)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(data);

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    private void SaveFile(string token, byte[] data)
    {
        var directory = Environment.GetEnvironmentVariable("FILE_DIRECTORY");
        if (directory == null)
        {
            Console.WriteLine("Укажите директорию для файлов в ENV");
            return;
        }
        
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllBytes($"{directory}/{token}", data);
    }

    // сохранить данные в таблицу MongoDB
    private void SaveToDatabase(string name, string token, int weight)
    {
        var document = new BsonDocument
        {
            { "name", name },
            { "token", token },
            { "weight", weight },
            { "time", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() },
            { "counter", 24 }
        };

        collection.InsertOne(document);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // if (_logger.IsEnabled(LogLevel.Information))
            // {
            //     _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            // }

            channel.BasicConsume(queue: "rpc_queue",
                autoAck: false,
                consumer: consumer);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
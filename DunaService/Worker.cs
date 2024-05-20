using System.Security.Cryptography;
using System.Text;
using Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DunaService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly MongoHandler _mongo;
    private readonly RabbitServiceHandler _rabbit;
    private readonly FileHandler _file;

    public Worker(ILogger<Worker> logger, MongoHandler mongo, RabbitServiceHandler rabbit, FileHandler file)
    {
        _mongo = mongo;
        _rabbit = rabbit;
        _file = file;

        _logger = logger;

        _rabbit.AddReceivedHandler(Received);
    }

    private void Received(object? model, BasicDeliverEventArgs? ea)
    {
        // if (ea == null) return;
        var props = ea.BasicProperties;
        var replyProps = _rabbit.channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;

        var body = ea.Body.ToArray();
        _file.SplitFile(body);

        // пробросить всё содержимое сообщения через хэш-функцию, таким образом сгенерировать новое имя файла (токен) в 64 символа
        // сохранить файл в папку /files
        // добавить запись в базу данных такого содержания:
        // ip, название файла, токен, размер, UNIX-время, счётчик до удаления (24)

        _file.Hash(body);
        _file.SaveFile(_file._token);
        _mongo.SaveToDatabase(_file._filename, _file._token, _file._weight);
        var responseBytes = Encoding.UTF8.GetBytes(_file._token);
        // Console.WriteLine(name);
        _rabbit.channel.BasicPublish(exchange: string.Empty,
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            body: responseBytes);
        _rabbit.channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _rabbit.Work();
            await Task.Delay(1000, stoppingToken);
        }
    }
}
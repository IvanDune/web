using System.Text;
using System.Text.Json;
using DunaConverter.Handlers;
using DunaConverter.Handlers.DataTypes;
using RabbitMQ.Client.Events;

namespace DunaConverter.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly RabbitServiceHandler _rabbit;

    public Worker(ILogger<Worker> logger, RabbitServiceHandler rabbit)
    {
        _rabbit = rabbit;

        _logger = logger;

        _rabbit.AddReceivedHandler(Received);
    }

    private async Task<int> TryConvert(WorkerTaskMessage convertMessage)
    {
        string[] oldPathSplit = convertMessage.FilePath.Split(".");
        oldPathSplit[oldPathSplit.Length - 1] = "";
        string newPath = String.Join(".", oldPathSplit);
        newPath += convertMessage.OutputType;

        try
        {
            await ffmpegHandler.Convert(convertMessage.FilePath, newPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error converting file");
            return -1;
        }

        return 0;
    }

    private async Task<int> TryCompress(WorkerTaskMessage compressMessage)
    {
        string outputType = compressMessage.OutputType == "audio" ? "mp3" : "mp4";
        string[] oldPathSplit = compressMessage.FilePath.Split(".");
        oldPathSplit[^1] = "new.";
        string newPath = String.Join(".", oldPathSplit);
        newPath += outputType;

        try
        {
            if (compressMessage.OutputType == "audio")
            {
                await ffmpegHandler.CompressAudio(compressMessage.FilePath, newPath);
            }
            else
            {
                await ffmpegHandler.CompressVideo(compressMessage.FilePath, newPath);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error converting file");
            return -1;
        }

        return 0;
    }

    private async void Received(object? model, BasicDeliverEventArgs? ea)
    {
        if (ea == null) return;
        var props = ea.BasicProperties;
        var replyProps = _rabbit.channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;

        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var convertMessage = JsonSerializer.Deserialize<WorkerTaskMessage>(message);

        if (convertMessage == null) return;

        int result = -1;
        
        switch (convertMessage.TaskType)
        {
            case "convert":
                result = await TryConvert(convertMessage);
                break;
            case "compress":
                result = await TryCompress(convertMessage);
                break;
        }


        _rabbit.channel.BasicPublish(exchange: string.Empty,
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            mandatory: false,
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new WorkerResponse
            {
                Result = result
            })));
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
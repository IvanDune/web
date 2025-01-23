using System.Text;
using System.Text.Json;
using DunaConverter.Handlers;
using DunaConverter.Handlers.DataTypes;
using RabbitMQ.Client.Events;

namespace DunaConverter.Orchestrator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private static MongoHandler _mongo;
    private static RabbitServerHandler _rabbit;

    public Worker(ILogger<Worker> logger, MongoHandler mongo, RabbitServerHandler rabbit)
    {
        _mongo = mongo;
        _rabbit = rabbit;

        _logger = logger;

        _rabbit.AddReceivedHandler(Received);
    }

    private static void Received(object? _, BasicDeliverEventArgs? ea)
    {
        if (!_rabbit.callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
            return;
        var body = ea.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WorkerTaskMessage? task = await _mongo.StartTask();
            if (task == null)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            var message = JsonSerializer.Serialize(task);
            var response = await _rabbit.CallAsync(Encoding.UTF8.GetBytes(message), stoppingToken);
            var workerResponse = JsonSerializer.Deserialize<WorkerResponse>(response);
            if (workerResponse == null || workerResponse.Result == -1)
            {
                await _mongo.FinishTask(task.TaskId, -1);
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            if (workerResponse.Result == 1 || workerResponse.Result == 2)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            await _mongo.FinishTask(task.TaskId, 2);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
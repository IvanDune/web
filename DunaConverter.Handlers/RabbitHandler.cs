using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DunaConverter.Handlers;

public class RabbitHandler
{
    protected const string QUEUE_NAME = "rpc_queue";
    public readonly IModel channel;
    public readonly EventingBasicConsumer consumer;
    public readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

    public RabbitHandler(string hostname, string username, string password)
    {
        var factory = new ConnectionFactory
            { HostName = hostname, UserName = username, Password = password };
        var connection = factory.CreateConnection();
        channel = connection.CreateModel();
        consumer = new EventingBasicConsumer(channel);
    }

    public void AddReceivedHandler(EventHandler<BasicDeliverEventArgs?> received)
    {
        consumer.Received += received;
    }
}

public class RabbitServiceHandler : RabbitHandler
{
    public RabbitServiceHandler(string hostname, string username, string password) : base(hostname, username, password)
    {
        channel.QueueDeclare(queue: QUEUE_NAME,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    public void Work()
    {
        channel.BasicConsume(consumer: consumer,
            queue: QUEUE_NAME,
            autoAck: false);
    }
}

public class RabbitServerHandler : RabbitHandler
{
    private readonly string replyQueueName;

    public RabbitServerHandler(string hostname, string username, string password) : base(hostname, username, password)
    {
        replyQueueName = channel.QueueDeclare().QueueName;
        channel.BasicConsume(consumer: consumer,
            queue: replyQueueName,
            autoAck: true);
    }

    public Task<string> CallAsync(byte[] message, CancellationToken cancellationToken = default)
    {
        IBasicProperties props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;
        var tcs = new TaskCompletionSource<string>();
        callbackMapper.TryAdd(correlationId, tcs);

        Console.WriteLine(message.Length);

        channel.BasicPublish(exchange: string.Empty,
            routingKey: QUEUE_NAME,
            basicProperties: props,
            body: message);

        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
        return tcs.Task;
    }
}
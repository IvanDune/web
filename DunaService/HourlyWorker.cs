using Handlers;

namespace DunaService;

class HourlyWorker : BackgroundService
{
    private readonly ILogger<HourlyWorker> _logger;
    private readonly MongoHandler mongo;
    private readonly FileHandler file;

    public HourlyWorker(ILogger<HourlyWorker> logger, MongoHandler _mongo, FileHandler _file)
    {
        mongo = _mongo;
        file = _file;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            mongo.DecrementCounter();
            // удаляем все файлы, у которых счётчик = 0
            var filenames = await mongo.GetReadyFiles();
            file.DeleteFiles(filenames);

            _logger.LogInformation("All counters decremented");

            await Task.Delay(60 * 60 * 1000, stoppingToken);
        }
    }
}
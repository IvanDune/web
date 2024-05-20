using DunaService;
using Handlers;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var Credentials = new CredentialsHandler(configuration);
var mongo_address = Credentials.MongoURL;
var username = Credentials.Username;
var hostname = Credentials.Hostname;
var password = Credentials.Password;
var file_directory = Credentials.FileDirectory;

var mongoHandler = new MongoHandler(mongo_address, username, "files");
var fileHandler = new FileHandler(file_directory);

var rabbitHandler = new RabbitServiceHandler(hostname, username, password);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>(
    serviceProvider => new Worker(
        serviceProvider.GetRequiredService<ILogger<Worker>>(), mongoHandler, rabbitHandler, fileHandler
    ));
builder.Services.AddHostedService<HourlyWorker>(
    serviceProvider => new HourlyWorker(
        serviceProvider.GetRequiredService<ILogger<HourlyWorker>>(), mongoHandler, fileHandler
    ));

var host = builder.Build();
host.Run();
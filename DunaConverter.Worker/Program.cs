using DunaConverter.Worker;
using DunaConverter.Handlers;
using OnlineCompiler.Handlers.Hash;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var credentials = new CredentialsHandler(configuration);
var username = credentials.Username;
var hostname = credentials.Hostname;
var password = credentials.Password;

var rabbitHandler = new RabbitServiceHandler(hostname, username, password);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>(
    serviceProvider => new Worker(
        serviceProvider.GetRequiredService<ILogger<Worker>>(), rabbitHandler
    ));

var host = builder.Build();
host.Run();
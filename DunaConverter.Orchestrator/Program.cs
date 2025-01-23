using DunaConverter.Handlers;
using DunaConverter.Orchestrator;
using OnlineCompiler.Handlers.Hash;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var credentials = new CredentialsHandler(configuration);
var mongoAddress = credentials.MongoURL;
var username = credentials.Username;
var hostname = credentials.Hostname;
var password = credentials.Password;

var rabbitHandler = new RabbitServerHandler(hostname, username, password);
var mongoHandler = new MongoHandler(mongoAddress, username, "outbox");

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>(
    
serviceProvider => new Worker(
        serviceProvider.GetRequiredService<ILogger<Worker>>(), mongoHandler, rabbitHandler
    ));    

var host = builder.Build();
host.Run();
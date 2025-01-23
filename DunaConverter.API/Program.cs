using System.Text;
using DunaConverter.API.Routes;
using DunaConverter.Handlers;
using RabbitMQ.Client.Events;

namespace DunaConverter.API;

class Program
{
    private static MongoHandler? _mongo;
    private static FileHandler? _file;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var credentials = new CredentialsHandler(configuration);
        var mongoAddress = credentials.MongoURL;
        var username = credentials.Username;
        var fileDirectory = credentials.FileDirectory;

        var origins = "origins";

        _mongo = new MongoHandler(mongoAddress, username, "outbox");
        _file = new FileHandler(fileDirectory);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: origins,
                policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
        });

        builder.Services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = null; });

        var app = builder.Build();

        app.UseCors(origins);

        var routes = new List<IRoute>
        {
            new ConvertRoute(_file, _mongo),
            new CompressRoute(_file, _mongo),
            new CheckTaskRoute(_mongo),
            new GetFileRoute(_mongo)
        };

        foreach (var route in routes)
        {
            route.MapRoutes(app);
        }

        app.Run();
    }
}

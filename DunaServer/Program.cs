using System.Text;
using System.Configuration;
using Handlers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using RabbitMQ.Client.Events;

namespace DunaServer;

class Program
{
    private static MongoHandler? mongo;
    private static RabbitServerHandler? rabbit;
    private static FileHandler? file;

    private static void Received(object? _, BasicDeliverEventArgs? ea)
    {
        if (!rabbit.callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
            return;
        var body = ea.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var Credentials = new CredentialsHandler(configuration);
        var mongo_address = Credentials.MongoURL;
        var username = Credentials.Username;
        var hostname = Credentials.Hostname;
        var password = Credentials.Password;
        var file_directory = Credentials.FileDirectory;
        
        var origins = "origins";

        mongo = new MongoHandler(mongo_address, username, "files");
        rabbit = new RabbitServerHandler(hostname, username, password);
        file = new FileHandler(file_directory);
        
        rabbit.AddReceivedHandler(Received);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: origins,
                policy => { policy.AllowAnyOrigin().AllowAnyHeader(); });
        });

        builder.Services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = null; });

        var app = builder.Build();

        app.UseCors(origins);

        app.MapPost("/api/upload", async (HttpContext c) =>
            {
                var fileData = c.Request.Body;
                // file is a stream that hasn't ended, so we need to read it
                // you can't use file.Length because it's not supported by the stream
                var fileByte = new List<byte>();

                var buffer = new byte[1024];
                var bytesRead = 0;
                while ((bytesRead = await fileData.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    fileByte.AddRange(buffer.Take(bytesRead));
                }

                var result = await rabbit.CallAsync(fileByte.ToArray());
                var writer = c.Response.BodyWriter;
                await writer.WriteAsync(Encoding.UTF8.GetBytes(result));

                c.Connection.RequestClose();
            })
            .WithName("UploadFile");
        
        app.MapGet("/api/getfile", async (HttpContext c) =>
        {
            // В c.Header лежит hash = хэш файла. Нужно найти файл по хэшу и вернуть информацию о нём
            var hash = c.Request.Query["hash"].ToString();
            var result = await mongo.GetDocument(hash);
            if (result == [])
            {
                c.Response.StatusCode = 404;
                return;
            }

            result.Remove("_id");

            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            await c.Response.WriteAsync(result.ToJson(jsonWriterSettings));
        }).WithName("GetFile");
        
        // сделать метод для получения файла по хэшу
        app.MapGet("/api/download", async (HttpContext c) =>
        {
            var insides = file.GetFile(c.Request.Query["hash"].ToString());
            await c.Response.Body.WriteAsync(insides);
        }).WithName("DownloadFile");

        app.Run();
    }
}
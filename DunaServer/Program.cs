using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace DunaServer;

class Program
{
    public static void Main(string[] args)
    {
        
        var builder = WebApplication.CreateBuilder(args);
        string? mongo_address = Environment.GetEnvironmentVariable("MONGO_URL");

        if (mongo_address == null)
        {
            Console.WriteLine("Установите параметр MONGO_URL");
            return;
        }
        
        var origins = "origins";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: origins,
                policy => { policy.AllowAnyOrigin().AllowAnyHeader(); });
        });

        builder.Services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = null; });

        // builder.Services.AddEndpointsApiExplorer();
        // builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseCors(origins);

        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseSwagger();
        //     app.UseSwaggerUI();
        // }

        // app.UseHttpsRedirection();
        
        MongoClient mongo_c = new MongoClient(mongo_address);
        IMongoDatabase database = mongo_c.GetDatabase("duna");
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("files");

        var client = new RpcClient();

        app.MapPost("/api/upload", async (HttpContext c) =>
            {
                var file = c.Request.Body;
                // file is a stream that hasn't ended, so we need to read it
                // you can't use file.Length because it's not supported by the stream
                var fileByte = new List<byte>();

                var buffer = new byte[1024];
                var bytesRead = 0;
                while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    fileByte.AddRange(buffer.Take(bytesRead));
                }

                var result = await client.CallAsync(fileByte.ToArray());
                var writer = c.Response.BodyWriter;
                await writer.WriteAsync(Encoding.UTF8.GetBytes(result));

                c.Connection.RequestClose();
            })
            .WithName("UploadFile");
        // .WithOpenApi();

        BsonDocument result;
        app.MapGet("/api/getfile", async (HttpContext c) =>
        {
            // В c.Header лежит hash = хэш файла. Нужно найти файл по хэшу и вернуть информацию о нём
            var hash = c.Request.Query["hash"].ToString();
            var filter = Builders<BsonDocument>.Filter.Eq("token", hash);
            result = await collection.Find(filter).FirstOrDefaultAsync();
            if (result == null)
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
            var hash = c.Request.Query["hash"].ToString();
            var filter = Builders<BsonDocument>.Filter.Eq("token", hash);
            result = await collection.Find(filter).FirstOrDefaultAsync();
            if (result == null)
            {
                c.Response.StatusCode = 404;
                return;
            }

            var directory = Environment.GetEnvironmentVariable("FILE_DIRECTORY");
            if (directory == null)
            {
                Console.WriteLine("Укажите директорию для файлов в ENV");
                return;
            }

            var file = File.ReadAllBytes($"{directory}/{hash}");
            await c.Response.Body.WriteAsync(file);
        }).WithName("DownloadFile");

        app.Run();
    }
}
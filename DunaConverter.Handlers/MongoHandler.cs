using DunaConverter.Handlers.DataTypes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DunaConverter.Handlers;

public class MongoHandler
{
    private readonly MongoClient? _client;
    private IMongoDatabase? _database;
    private IMongoCollection<BsonDocument> _outboxCollection;

    public MongoHandler(string mongoAddress, string username, string outboxCollection)
    {
        _client = new MongoClient(mongoAddress);
        _database = _client.GetDatabase(username);
        _outboxCollection = _database.GetCollection<BsonDocument>(outboxCollection);
    }

    public async Task<string> Enqueue(string task, string outputType, string filePath)
    {
        var document = new BsonDocument
        {
            { "task", task },
            { "outputType", outputType },
            { "filePath", filePath },
            { "status", 0 }
        };

        await _outboxCollection.InsertOneAsync(document);
        return document["_id"].ToString() ?? throw new InvalidOperationException();
    }

    public async Task<WorkerTaskMessage?> StartTask()
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("status", 0)
        );

        var update = Builders<BsonDocument>.Update.Set("status", 1);

        var result = await _outboxCollection.FindOneAndUpdateAsync(filter, update);
        Console.WriteLine(result);


        if (result == null)
        {
            return null;
        }

        BsonObjectId id = (BsonObjectId)result["_id"];

        return new WorkerTaskMessage()
        {
            FilePath = (string)result["filePath"],
            OutputType = (string)result["outputType"],
            TaskType = (string)result["task"],
            TaskId = id.ToString()
        };
    }

    public async Task FinishTask(string id, int result)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))
        );

        var update = Builders<BsonDocument>.Update.Set("status", result);

        await _outboxCollection.UpdateOneAsync(filter, update);
    }

    public async Task<int> CheckTask(string id)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))
        );

        var result = await _outboxCollection.Find(filter).FirstOrDefaultAsync();

        if (result == null)
        {
            return -2;
        }

        return (int)result["status"];
    }

    public async Task<WorkerTaskMessage?> GetTask(string id)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id))
        );

        var result = await _outboxCollection.Find(filter).FirstOrDefaultAsync();

        if (result == null)
        {
            return null;
        }

        // convert the result to WorkerTaskMessage
        BsonObjectId objectId = (BsonObjectId)result["_id"];
        return new WorkerTaskMessage()
        {
            FilePath = (string)result["filePath"],
            OutputType = (string)result["outputType"],
            TaskType = (string)result["task"],
            TaskId = objectId.ToString()
        };
    }
}
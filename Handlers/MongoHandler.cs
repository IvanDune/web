using MongoDB.Bson;
using MongoDB.Driver;

namespace Handlers;

public class MongoHandler
{
    private readonly MongoClient? client;
    private IMongoDatabase? database;
    private IMongoCollection<BsonDocument> collection;
    
    public MongoHandler(string mongo_address, string username, string collection_name)
    {

        client = new MongoClient(mongo_address);
        database = client.GetDatabase(username);
        collection = database.GetCollection<BsonDocument>(collection_name);
    }
    
    public async void SaveToDatabase(string name, string token, int weight)
    {
        var document = new BsonDocument
        {
            { "name", name },
            { "token", token },
            { "weight", weight },
            { "time", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() },
            { "counter", 24 }
        };
        
        await collection.InsertOneAsync(document);
    }

    public async void DecrementCounter()
    {
        var filter = Builders<BsonDocument>.Filter.Empty;
        var update = Builders<BsonDocument>.Update.Inc("counter", -1);
        await collection.UpdateManyAsync(filter, update);
    }
    
    public async Task<BsonDocument> GetDocument(string token)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("token", token);
        var document = await (await collection.FindAsync(filter)).FirstOrDefaultAsync();
        document.Remove("_id");
        return document;
    }
    
    public async Task<List<BsonDocument>> GetReadyFiles()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("counter", 0);
        var filenames = await (await collection.FindAsync(filter)).ToListAsync();
        await collection.DeleteManyAsync(filter);
        return filenames;
    }

}
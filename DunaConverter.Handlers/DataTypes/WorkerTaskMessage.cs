using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DunaConverter.Handlers.DataTypes;

public class WorkerTaskMessage
{
    public string FilePath { get; set; }
    public string TaskType { get; set; }
    public string OutputType { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string TaskId { get; set; }
}
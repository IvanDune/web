using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DunaConverter.Handlers;

namespace DunaConverter.API.Routes;

public class CompressRoute : IRoute
{
    private readonly FileHandler _fileHandler;
    private readonly MongoHandler _mongoHandler;

    public CompressRoute(FileHandler fileHandler, MongoHandler mongoHandler)
    {
        _fileHandler = fileHandler;
        _mongoHandler = mongoHandler;
    }


    private async Task<IResult> Compress(HttpRequest request)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Invalid content type");
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest("No file uploaded");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();
        var filePath = _fileHandler.SaveFile(fileData, file.FileName);

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        var outputType = fileExtension switch
        {
            ".mp4" or ".avi" or ".mkv" => "video",
            ".mp3" or ".wav" or ".flac" => "audio",
            _ => "unknown"
        };

        if (outputType == "unknown")
        {
            return Results.BadRequest("Unsupported file type");
        }

        var taskId = await _mongoHandler.Enqueue("compress", outputType, filePath);

        return Results.Ok(new { taskId });
    }

    public void MapRoutes(WebApplication app)
    {
        app.MapPost("/compress", Compress).WithName("Compress");
    }
}
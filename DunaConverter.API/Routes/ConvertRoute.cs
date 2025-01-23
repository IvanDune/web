using DunaConverter.Handlers;

namespace DunaConverter.API.Routes;

public class ConvertRoute : IRoute
{
    private readonly FileHandler _fileHandler;
    private readonly MongoHandler _mongoHandler;

    public ConvertRoute(FileHandler fileHandler, MongoHandler mongoHandler)
    {
        _fileHandler = fileHandler;
        _mongoHandler = mongoHandler;
    }

    private async Task<IResult> Convert(HttpRequest request)
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

        var outputType = form["outputType"];
        if (string.IsNullOrEmpty(outputType))
        {
            return Results.BadRequest("No output type specified");
        }

        var taskId = await _mongoHandler.Enqueue("convert", outputType!, filePath);

        return Results.Ok(new { taskId });
    }

    public void MapRoutes(WebApplication app)
    {
        app.MapPost("/convert", Convert).WithName("Convert");
    }
}
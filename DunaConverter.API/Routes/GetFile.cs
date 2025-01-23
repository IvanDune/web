using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DunaConverter.Handlers;
using System.IO;

namespace DunaConverter.API.Routes;

public class GetFileRoute : IRoute
{
    private readonly MongoHandler _mongoHandler;

    public GetFileRoute(MongoHandler mongoHandler)
    {
        _mongoHandler = mongoHandler;
    }

    private async Task<IResult> GetFile(HttpRequest request)
    {
        if (!request.Query.ContainsKey("id"))
        {
            return Results.BadRequest("File ID is required");
        }

        var fileId = request.Query["id"].ToString();
        var task = await _mongoHandler.CheckTask(fileId);

        switch (task)
        {
            case -2:
                return Results.NotFound("File not found");
            case -1:
                return Results.Problem("There has been a conversion error");
            case 0:
            case 1:
                return Results.BadRequest("The file is still being processed");
        }

        var taskMessage = await _mongoHandler.GetTask(fileId);
        if (taskMessage == null)
        {
            return Results.NotFound("The file is not found");
        }

        var fileName = Path.GetFileNameWithoutExtension(taskMessage.FilePath);

        if (taskMessage.TaskType == "convert")
        {
            fileName += "." + taskMessage.OutputType;
        }
        else
        {
            fileName += ".new.";
            if (taskMessage.OutputType == "video")
            {
                fileName += "mp4";
            }
            else
            {
                fileName += "mp3";
            }
        }
        
        var filePath = Path.GetDirectoryName(taskMessage.FilePath);


        var fileBytes = await File.ReadAllBytesAsync(Path.Join(filePath, fileName));
        return Results.File(fileBytes, "application/octet-stream", fileName);
    }

    public void MapRoutes(WebApplication app)
    {
        app.MapGet("/getfile", GetFile).WithName("GetFile");
    }
}
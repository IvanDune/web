using Microsoft.AspNetCore.Http;
using DunaConverter.Handlers;
using System.Text.Json;

namespace DunaConverter.API.Routes;

public class CheckTaskRoute : IRoute
{
    private readonly MongoHandler _mongoHandler;

    public CheckTaskRoute(MongoHandler mongoHandler)
    {
        _mongoHandler = mongoHandler;
    }
    
    private async Task<IResult> CheckTask(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        JsonDocument jsonDoc;
        try
        {
            jsonDoc = JsonDocument.Parse(body);
        }
        catch (Exception)
        {
            return Results.BadRequest("No valid JSON is provided");
        }
        if (!jsonDoc.RootElement.TryGetProperty("id", out var idElement))
        {
            return Results.BadRequest("Task ID is required");
        }

        var taskId = idElement.GetString();
        var status = await _mongoHandler.CheckTask(taskId);

        if (status == -2)
        {
            return Results.NotFound("Task not found");
        }

        return Results.Ok(new { status });
    } 

    public void MapRoutes(WebApplication app)
    {
        app.MapPost("/checktask", CheckTask).WithName("CheckTask");
    }
}
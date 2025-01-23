using Microsoft.Extensions.Configuration;

namespace DunaConverter.Handlers;

public class CredentialsHandler
{
    private string[] ParamList = ["mongo_url", "username", "hostname", "password", "file_directory"];

    public CredentialsHandler(IConfigurationRoot config)
    {
        foreach (var param in ParamList)
        {
            var value = config[param];
            if (value == null)
            {
                throw new ArgumentException($"Не хватает параметра {param}");
            }
        }

        MongoURL = config["mongo_url"];
        Username = config["username"];
        Hostname = config["hostname"];
        Password = config["password"];
        FileDirectory = config["file_directory"];
    }

    public string MongoURL { get; }
    public string Username { get; }
    public string Hostname { get; }
    public string Password { get; }
    public string FileDirectory { get; }
}
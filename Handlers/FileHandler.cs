using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace Handlers;

public class FileHandler
{
    public string _filename;
    private byte[] _file;
    private string _directory;
    public int _weight;
    public string _token;
    
    public FileHandler(string directory)
    {
        _directory = directory;
        if (!Directory.Exists(_directory)) Directory.CreateDirectory(_directory);
        
    }
    public void SplitFile(byte[] body)
    {
        _weight = BitConverter.ToInt32(body[..8]);
        _filename = Encoding.UTF8.GetString(body[8..^_weight]);
        _file = body[^_weight..];
    }

    public void SaveFile(string hash)
    {
        File.WriteAllBytes($"{_directory}/{hash}", _file);
    }
    
    public void DeleteFiles(List<BsonDocument> filenames)
    {
        foreach (var filename in filenames)
        {
            var file = filename["token"].AsString;
            File.Delete($"{_directory}/{file}");
        }
    }
    
    public byte[] GetFile(string token)
    {
        return File.ReadAllBytes($"{_directory}/{token}");
    }
    
    public void Hash(byte[] data)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(data);

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            _token = builder.ToString();
        }
    }
}
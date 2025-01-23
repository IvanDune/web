using System.Security.Cryptography;
using System.Text;
using Handlers;

namespace OnlineCompiler.Handlers.Hash;

public class SHA256Hasher : IHasher
{
    public string Hash(byte[] data)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(data);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public string Hash(string data)
    {
        return Hash(Encoding.UTF8.GetBytes(data));
    }
}
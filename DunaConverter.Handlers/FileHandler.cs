using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DunaConverter.Handlers
{
    public class FileHandler(string path)
    {
        public string SaveFile(byte[] data, string fileName)
        {
            var folderName = GenerateFolderName();
            var folderPath = Path.Combine(path, folderName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            File.WriteAllBytes(filePath, data);
            return filePath;
        }

        private string GenerateFolderName()
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
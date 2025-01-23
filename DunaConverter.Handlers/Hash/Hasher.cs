namespace Handlers;

public interface IHasher
{
    public string Hash(byte[] data);
    public string Hash(string data);
}
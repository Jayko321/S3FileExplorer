namespace S3FE.Client.Models;

public sealed class Bucket(string name)
{
    public string Name { get; private set; } = name;

    internal void RenameLocal(string name)
    {
        Name = name;
    }
}

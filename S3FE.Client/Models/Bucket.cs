namespace S3FE.Client.Models;

public sealed class Bucket(string name, bool isVersioned)
{
    public string Name { get; private set; } = name;

    public bool IsVersioned { get; } = isVersioned;
}

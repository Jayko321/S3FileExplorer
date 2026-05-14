namespace S3FE.Client.Models;

public sealed class FolderItem(string displayName, string fullPrefix)
{
    public string DisplayName { get; } = displayName;
    public string FullPrefix { get; } = fullPrefix;
}

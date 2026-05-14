namespace S3FE.Client.Models;

using System.Collections.Generic;

public sealed class ListObjectsResult(IReadOnlyList<FolderItem> folders, IReadOnlyList<S3Object> files)
{
    public IReadOnlyList<FolderItem> Folders { get; } = folders;
    public IReadOnlyList<S3Object> Files { get; } = files;
}

namespace S3FE.Client.Models;

using System;

public sealed class S3Object(string key, long? size, DateTime? lastModified, string etag)
{
    public string Key { get; private set; } = key;

    public long? Size { get; private set; } = size;

    public DateTime? LastModified { get; private set; } = lastModified;

    public string ETag { get; private set; } = etag;

    internal void RenameLocal(string key)
    {
        Key = key;
    }

    internal void RefreshMetadata(long? size, DateTime? lastModified, string etag)
    {
        Size = size;
        LastModified = lastModified;
        ETag = etag;
    }
}

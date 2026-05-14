using System;
using System.Collections.Generic;

namespace S3FE.Client.Models;

public sealed class S3Object(string key, long? size, DateTime? lastModified, string etag, List<string>? versionIds = null)
{
    public string Key { get; private set; } = key;

    public long? Size { get; private set; } = size;

    public DateTime? LastModified { get; private set; } = lastModified;

    public string ETag { get; private set; } = etag;

    public List<string>? VersionIds { get; } = versionIds;

    public string? VersionId { get; set; }

    public bool IsLatest { get; set; }

    public bool IsDeleteMarker { get; set; }

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
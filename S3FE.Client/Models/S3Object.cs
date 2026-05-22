using System;
using System.Collections.Generic;

namespace S3FE.Client.Models;

public sealed class S3Object(string key, long? size, DateTime? lastModified, string etag)
{
    public string Key { get; } = key;

    public long? Size { get; } = size;

    public DateTime? LastModified { get; } = lastModified;

    public string ETag { get; } = etag;

    public List<string>? VersionIds { get; set; }

    public string? VersionId { get; set; }

    public bool IsLatest { get; set; }

    public bool IsDeleteMarker { get; set; }
}

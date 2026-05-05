namespace S3FE.Shared.DTOs;

public class S3ObjectDTO
{
    public string Key { get; set; } = string.Empty;

    public long? Size { get; set; }

    public DateTime? LastModified { get; set; }

    public string ETag { get; set; } = string.Empty;
}

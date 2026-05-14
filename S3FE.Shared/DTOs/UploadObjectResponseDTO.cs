namespace S3FE.Shared.DTOs;

public class UploadObjectResponseDTO
{
    public string Key { get; set; } = string.Empty;

    public string? VersionId { get; set; }
}

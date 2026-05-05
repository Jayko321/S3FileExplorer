namespace S3FE.Shared.DTOs;

public class ObjectListingDTO
{
    public List<string> Folders { get; set; } = [];

    public List<S3ObjectDTO> Files { get; set; } = [];
}

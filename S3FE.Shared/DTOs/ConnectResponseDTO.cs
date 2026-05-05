namespace S3FE.Shared.DTOs;

public class ConnectResponseDTO
{
    public string Token { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Message { get; set; } = string.Empty;
}

namespace S3FE.Client.Services;

using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public interface IAuthApiClient
{
    Task<ConnectResponseDTO> ConnectAsync(ConnectRequestDTO request);
}

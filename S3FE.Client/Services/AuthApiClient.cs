namespace S3FE.Client.Services;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using S3FE.Shared.DTOs;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:12000")
    };

    public async Task<ConnectResponseDTO> ConnectAsync(ConnectRequestDTO request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/connect", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? "Failed to connect to MinIO."
                : error);
        }

        var result = await response.Content.ReadFromJsonAsync<ConnectResponseDTO>();
        return result ?? throw new InvalidOperationException("Server returned an empty response.");
    }
}

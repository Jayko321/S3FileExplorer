namespace S3FE.Server.Controllers;

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using S3FE.Server.Services;
using S3FE.Shared.DTOs;

[ApiController]
[Route("api/auth")]
public class AuthController(IS3SessionStore sessionStore) : ControllerBase
{
    private readonly IS3SessionStore _sessionStore = sessionStore;

    [AllowAnonymous]
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectAsync([FromBody] ConnectRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest("Endpoint is required.");

        if (string.IsNullOrWhiteSpace(request.AccessKey))
            return BadRequest("Access key is required.");

        if (string.IsNullOrWhiteSpace(request.SecretKey))
            return BadRequest("Secret key is required.");

        var endpoint = request.Endpoint.Trim().TrimEnd('/');

        var client = new AmazonS3Client(
            request.AccessKey,
            request.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            });

        try
        {
            await client.ListBucketsAsync();
        }
        catch (AmazonServiceException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            client.Dispose();
            return Unauthorized("Invalid MinIO credentials.");
        }
        catch (AmazonServiceException ex)
        {
            client.Dispose();
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"MinIO responded with an error: {ex.Message}");
        }
        catch (AmazonClientException ex)
        {
            client.Dispose();
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Could not connect to MinIO: {ex.Message}");
        }
        catch (Exception ex) when (IsConnectionFailure(ex))
        {
            client.Dispose();
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Could not connect to MinIO at '{endpoint}'. Make sure the MinIO server is running.");
        }

        var session = _sessionStore.CreateSession(endpoint, client);

        return Ok(new ConnectResponseDTO
        {
            Token = session.Token,
            Endpoint = session.Endpoint,
            ExpiresAtUtc = session.ExpiresAtUtc,
            Message = "Connected to MinIO successfully."
        });
    }

    private static bool IsConnectionFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException)
                return true;
        }

        return false;
    }
}

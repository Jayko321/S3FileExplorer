namespace S3FE.Client.Services;

using System.IO;

public sealed class FilePickResult(Stream stream, string fileName, string contentType)
{
    public Stream Stream { get; } = stream;
    public string FileName { get; } = fileName;
    public string ContentType { get; } = contentType;
}

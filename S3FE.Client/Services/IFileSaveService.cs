namespace S3FE.Client.Services;

using System.IO;
using System.Threading.Tasks;

public interface IFileSaveService
{
    Task<string?> SaveFileAsync(string defaultFileName, Stream contentStream);
}

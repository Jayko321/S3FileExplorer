namespace S3FE.Client.Services;

using System.Threading.Tasks;

public interface IFilePickerService
{
    Task<FilePickResult?> PickFileAsync();
}

namespace S3FE.Client.Services;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.AspNetCore.StaticFiles;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<FilePickResult?> PickFileAsync()
    {
        var topLevel = GetTopLevel();

        if (topLevel is null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            //TODO: Allow multiple files
            AllowMultiple = false
        });

        var file = files.FirstOrDefault();

        if (file is null)
            return null;

        var stream = await file.OpenReadAsync();
        var contentType = GetContentType(file.Name);

        return new FilePickResult(stream, file.Name, contentType);
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }

    private static string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();

        return provider.TryGetContentType(fileName, out var contentType) ? contentType : "application/octet-stream";
    }
}

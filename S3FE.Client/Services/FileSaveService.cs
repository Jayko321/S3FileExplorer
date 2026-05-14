namespace S3FE.Client.Services;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

public sealed class FileSaveService : IFileSaveService
{
    public async Task<string?> SaveFileAsync(string defaultFileName, Stream contentStream)
    {
        var topLevel = GetTopLevel();

        if (topLevel is null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = defaultFileName
        });

        if (file is null)
            return null;

        try
        {
            await using var outputStream = await file.OpenWriteAsync();
            await contentStream.CopyToAsync(outputStream);
            return file.Name;
        }
        finally
        {
            await contentStream.DisposeAsync();
        }
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }
}

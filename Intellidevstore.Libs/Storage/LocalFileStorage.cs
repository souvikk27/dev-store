using Intellidevstore.Libs.Types;
using SharpGrip.FileSystem;

namespace Intellidevstore.Libs.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly IFileSystem _fileSystem;
    private readonly string _adapterPrefix;

    public LocalFileStorage(IFileSystem fileSystem, string adapterPrefix)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _adapterPrefix = adapterPrefix;
    }

    private string GetVirtualPath(string relativePath)
    {
        var sanitized = relativePath.Replace('\\', '/').TrimStart('/');
        return $"{_adapterPrefix}://{sanitized}";
    }

    public async Task<Result<Unit, string>> SaveAsync(
        string relativePath,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (content is null)
                return new Err<Unit, string>("Invalid input");

            var virtualPath = GetVirtualPath(relativePath);
            // var directoryPath = Path.GetDirectoryName(virtualPath)?.Replace('\\', '/');

            await _fileSystem.WriteFileAsync(
                virtualPath,
                content,
                cancellationToken: cancellationToken
            );
            return Unit.Value;
        }
        catch (Exception ex)
        {
            return new Err<Unit, string>($"Failed to save file: {ex.Message}");
        }
    }

    public async Task<Stream> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default
    )
    {
        var virtualPath = GetVirtualPath(relativePath);
        //Sanitize url encoding issues
        if (virtualPath.Contains('%'))
        {
            virtualPath = Uri.UnescapeDataString(virtualPath);
        }
        // SharpGrip returns bytes, we wrap them in a MemoryStream for the Stream contract
        var fileContent = await _fileSystem.ReadFileAsync(virtualPath, cancellationToken);
        return new MemoryStream(fileContent);
    }

    public async Task DeleteAsync(string relativePath)
    {
        var virtualPath = GetVirtualPath(relativePath);
        //Sanitize url encoding issues
        if (virtualPath.Contains('%'))
        {
            virtualPath = Uri.UnescapeDataString(virtualPath);
        }
        await _fileSystem.DeleteFileAsync(virtualPath);
    }
}

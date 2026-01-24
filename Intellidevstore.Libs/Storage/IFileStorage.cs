using Intellidevstore.Libs.Types;

namespace Intellidevstore.Libs.Storage;

public interface IFileStorage
{
    Task<Result<Unit, string>> SaveAsync(
        string path,
        Stream content,
        CancellationToken cancellationToken = default
    );

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path);
}

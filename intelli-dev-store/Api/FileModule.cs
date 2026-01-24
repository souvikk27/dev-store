using Carter;
using Intellidevstore.Libs.Storage;
using Microsoft.AspNetCore.Mvc;

namespace intelli_dev_store.Api;

public sealed class FileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("Files");

        // -----------------------------
        // UPLOAD
        // -----------------------------
        group
            .MapPost("/", UploadAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        // -----------------------------
        // DOWNLOAD
        // -----------------------------
        group
            .MapGet("/{**path}", DownloadAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // -----------------------------
        // DELETE
        // -----------------------------
        group
            .MapDelete("/{**path}", DeleteAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    // =============================
    // Handlers
    // =============================

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        [FromServices] IFileStorage storage,
        CancellationToken ct
    )
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Multipart form-data expected.");

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
            return Results.BadRequest("File is missing.");

        var safeFileName = Path.GetFileName(file.FileName);
        var relativePath = $"uploads/{Guid.NewGuid()}_{safeFileName}";

        await using var stream = file.OpenReadStream();

        var response = await storage.SaveAsync(relativePath, stream, ct);

        if (!response.IsSuccess)
            return Results.Problem($"Failed to save file. {response.ErrorOrDefault}");

        return Results.Created($"/api/files/{relativePath}", new { path = relativePath });
    }

    private static async Task<IResult> DownloadAsync(
        string path,
        [FromServices] IFileStorage storage,
        CancellationToken ct
    )
    {
        try
        {
            var stream = await storage.OpenReadAsync(path, ct);

            var fileName = Path.GetFileName(path);
            return Results.File(
                stream,
                contentType: "application/octet-stream",
                fileDownloadName: fileName
            );
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteAsync(
        string path,
        [FromServices] IFileStorage storage,
        CancellationToken ct
    )
    {
        try
        {
            await storage.DeleteAsync(path);
            return Results.NoContent();
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

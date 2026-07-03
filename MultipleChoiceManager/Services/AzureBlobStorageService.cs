using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace MultipleChoiceManager.Services;

// Legt Dateien in einem privaten Azure-Blob-Container ab. In der Datenbank wird die
// Blob-URI ohne Zugriffstoken gespeichert; für den Abruf im Browser erzeugt
// GetDownloadUrl eine zeitlich begrenzte SAS-URL.
public class AzureBlobStorageService : IFileStorageService
{
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    private readonly BlobContainerClient _containerClient;
    private readonly IWebHostEnvironment _environment;

    public AzureBlobStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _environment = environment;
        var connectionString = configuration.GetConnectionString("AzureBlobStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Der Connection-String 'AzureBlobStorage' ist nicht konfiguriert. Lokal per " +
                "'dotnet user-secrets set \"ConnectionStrings:AzureBlobStorage\" \"<Wert>\"' setzen.");
        }

        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "chapter-slides";
        _containerClient = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        await _containerClient.CreateIfNotExistsAsync();

        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (TryGetBlobName(fileUrl) is { } blobName)
        {
            await _containerClient.DeleteBlobIfExistsAsync(blobName);
        }
    }

    public async Task<byte[]> ReadAsync(string fileUrl)
    {
        if (TryGetBlobName(fileUrl) is { } blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToArray();
        }

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetByteArrayAsync(uri);
        }

        var relativePath = fileUrl.Replace("~", string.Empty, StringComparison.Ordinal).TrimStart('/');
        var filePath = Path.Combine(_environment.WebRootPath, relativePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Die hinterlegte PDF-Datei wurde nicht gefunden.", filePath);
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    public string GetDownloadUrl(string fileUrl)
    {
        if (TryGetBlobName(fileUrl) is not { } blobName)
        {
            return fileUrl;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);

        return blobClient
            .GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(DownloadUrlLifetime))
            .ToString();
    }

    // Extrahiert den Blob-Namen aus einer gespeicherten URL; null bei Fremd-URLs
    // (z. B. Alt-Einträge der lokalen Dummy-Ablage wie "/uploads/…").
    private string? TryGetBlobName(string fileUrl)
    {
        var containerPrefix = $"{_containerClient.Uri}/";

        if (!fileUrl.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fileUrl[containerPrefix.Length..];
    }
}

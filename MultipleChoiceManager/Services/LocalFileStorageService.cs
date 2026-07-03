namespace MultipleChoiceManager.Services;

// Dummy-Implementierung: legt Dateien unter wwwroot/uploads ab.
public class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private const string UploadsFolder = "uploads";

    private readonly IWebHostEnvironment _environment = environment;

    public async Task<string> SaveAsync(IFormFile file)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, UploadsFolder);
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        return $"/{UploadsFolder}/{fileName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_environment.WebRootPath, UploadsFolder, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> ReadAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_environment.WebRootPath, UploadsFolder, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Die hinterlegte PDF-Datei wurde nicht gefunden.", filePath);
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    public string GetDownloadUrl(string fileUrl)
    {
        return fileUrl;
    }
}

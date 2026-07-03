namespace MultipleChoiceManager.Services;

// Abstraktion für den Dateispeicher, damit die lokale Dummy-Ablage später
// ohne Controller-Änderungen gegen Azure Blob Storage getauscht werden kann.
public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file);

    Task DeleteAsync(string fileUrl);
}

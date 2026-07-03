namespace MultipleChoiceManager.Services;

// Abstraktion für den Dateispeicher (Azure Blob Storage oder lokale Dummy-Ablage),
// damit die Implementierung ohne Controller-Änderungen austauschbar bleibt.
public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file);

    Task DeleteAsync(string fileUrl);

    Task<byte[]> ReadAsync(string fileUrl);

    // Wandelt die gespeicherte Datei-Referenz in eine im Browser abrufbare URL um
    // (bei Azure Blob Storage eine zeitlich begrenzte SAS-URL, lokal die URL selbst).
    string GetDownloadUrl(string fileUrl);
}

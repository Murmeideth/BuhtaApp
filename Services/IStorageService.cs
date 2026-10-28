namespace BuhtaApp.Services;

public interface IStorageService
{
    string GetPath(string fileName);
    bool Exists(string fileName);
    Task<T?> LoadAsync<T>(string fileName) where T : class;
    Task SaveAsync<T>(string fileName, T data) where T : class;
    Task DeleteAsync(string fileName);
}
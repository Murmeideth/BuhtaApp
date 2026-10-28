using System.Diagnostics;
using System.Text.Json;

namespace BuhtaApp.Services;

public class JsonStorageService : IStorageService
{
    public string GetPath(string fileName) =>
        Path.Combine(FileSystem.AppDataDirectory, fileName);

    public bool Exists(string fileName) => File.Exists(GetPath(fileName));

    public async Task<T?> LoadAsync<T>(string fileName) where T : class
    {
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return null;

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, AppJson.Options);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Storage] Load failed for '{fileName}': {ex.Message}");
            return null;
        }
    }

    public async Task SaveAsync<T>(string fileName, T data) where T : class
    {
        var path = GetPath(fileName);
        var tmp = path + ".tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, data, AppJson.Options);
                await stream.FlushAsync();
            }

            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Storage] Save failed for '{fileName}': {ex.Message}");
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }
            throw;
        }
    }

    public Task DeleteAsync(string fileName)
    {
        var path = GetPath(fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
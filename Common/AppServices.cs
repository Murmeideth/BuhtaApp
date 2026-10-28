using Microsoft.Extensions.DependencyInjection;

namespace BuhtaApp.Common;

/// <summary>
/// Сервис-локатор для страниц без DI (например, ContentTemplate в Shell).
/// В постоянных экранах используем конструктор-инъекцию.
/// </summary>
public static class AppServices
{
    public static IServiceProvider Provider =>
        IPlatformApplication.Current?.Services
        ?? throw new InvalidOperationException("ServiceProvider not initialized");

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
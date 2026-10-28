using BuhtaApp.Models;

namespace BuhtaApp.Services;

public interface IMenuService
{
    /// <summary>Загружает menu.json из бандла (кешируется после первого вызова).</summary>
    Task<MenuConfig> LoadAsync();

    /// <summary>Уже загруженное меню (null до первого LoadAsync).</summary>
    MenuConfig? Current { get; }
}
using Microsoft.Maui.Graphics;

namespace BuhtaApp.Common;

public static class ProfilePresets
{
    /// <summary>Палитра цветов имени профиля, подобранная под тёмный фон.</summary>
    public static readonly string[] NameColors =
    {
        "#E96A6A",  // коралл
        "#E8A857",  // янтарь
        "#E5C96B",  // золото
        "#7DCBA6",  // мята
        "#6FB8D6",  // лазурь
        "#8CA5D9",  // лаванда-синий
        "#C08BD9",  // сирень
        "#E88BB8",  // роза
        "#D9A566",  // тыква
        "#9DBF7C",  // шалфей
        "#C9A0DC",  // аметист
        "#A0AEC0"   // серебро
    };

    /// <summary>Набор emoji-аватаров (морская/барная тематика).</summary>
    public static readonly string[] Avatars =
    {
        "👤", "👨", "👩", "🧑", "👨‍🍳", "👩‍🍳", "🧑‍🍳", "🧔",
        "👱", "🧑‍🦰", "🦊", "🐻", "🐺", "🦁", "🐙", "🦈",
        "⚓", "🏴‍☠️", "🍺", "🍹", "🥃", "🗡️", "⚔️", "🔱"
    };

    /// <summary>Безопасный парсинг hex-цвета в MAUI Color. При ошибке возвращает дефолт.</summary>
    public static Color ToColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb("#E96A6A");
        try { return Color.FromArgb(hex); }
        catch { return Color.FromArgb("#E96A6A"); }
    }
}
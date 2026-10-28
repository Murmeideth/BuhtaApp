using BuhtaApp.Common;
using BuhtaApp.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuhtaApp.ViewModels;

public class ColorOption
{
    public string Hex { get; }
    public Color Color => ProfilePresets.ToColor(Hex);
    public ColorOption(string hex) => Hex = hex;
}

public class AvatarOption
{
    public string Emoji { get; }
    public AvatarOption(string emoji) => Emoji = emoji;
}

public partial class RoleOption : ObservableObject
{
    public UserRole Role { get; }
    public string DisplayName { get; }

    public RoleOption(UserRole role, string displayName)
    {
        Role = role;
        DisplayName = displayName;
    }
}
using BuhtaApp.Common;
using BuhtaApp.Enums;
using BuhtaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using static Java.Util.Jar.Attributes;

namespace BuhtaApp.ViewModels;

public partial class ProfileEditorViewModel : BaseViewModel
{
    private readonly IProfileService _profiles;

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private ColorOption? selectedColor;

    [ObservableProperty]
    private AvatarOption? selectedAvatar;

    [ObservableProperty]
    private RoleOption? selectedRole;

    public ObservableCollection<ColorOption> Colors { get; } = new();
    public ObservableCollection<AvatarOption> Avatars { get; } = new();
    public ObservableCollection<RoleOption> Roles { get; } = new();

    public ProfileEditorViewModel(IProfileService profiles)
    {
        _profiles = profiles;
        Title = "Новый профиль";

        foreach (var hex in ProfilePresets.NameColors) Colors.Add(new ColorOption(hex));
        foreach (var emoji in ProfilePresets.Avatars) Avatars.Add(new AvatarOption(emoji));

        Roles.Add(new RoleOption(UserRole.Waiter, "Официант"));
        Roles.Add(new RoleOption(UserRole.Barman, "Бармен"));
        Roles.Add(new RoleOption(UserRole.Cook, "Повар"));
        Roles.Add(new RoleOption(UserRole.Admin, "Администратор"));

        SelectedColor = Colors[0];
        SelectedAvatar = Avatars[0];
        SelectedRole = Roles[0];
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Name) || SelectedColor is null || SelectedAvatar is null || SelectedRole is null)
            return;

        IsBusy = true;
        try
        {
            var profile = await _profiles.CreateProfileAsync(
                Name.Trim(),
                SelectedRole.Role,
                SelectedAvatar.Emoji,
                SelectedColor.Hex);

            await _profiles.SetDefaultAsync(profile.Id);
            await _profiles.SetCurrentAsync(profile.Id);

            await Shell.Current.GoToAsync("//lobby");
        }
        finally { IsBusy = false; }
    }
}
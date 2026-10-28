using BuhtaApp.ViewModels;

namespace BuhtaApp.Views;

public partial class ProfileEditorPage : ContentPage
{
    public ProfileEditorPage(ProfileEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
using BuhtaApp.ViewModels;

namespace BuhtaApp.Views;

public partial class LobbyPage : ContentPage
{
    private readonly LobbyViewModel _vm;

    public LobbyPage(LobbyViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
    }
}
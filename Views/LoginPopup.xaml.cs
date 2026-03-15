using HoundsBite.Services;
using Microsoft.Maui.Storage;

namespace HoundsBite.Views;

public partial class LoginPopup : ContentPage
{
    private readonly SupabaseService _supa;

    public LoginPopup(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Username and password are required.", "OK");
            return;
        }

        var user = await _supa.LoginAsync(username, password);

        if (user == null)
        {
            await DisplayAlert("Error", "Invalid username or password.", "OK");
            return;
        }

        Preferences.Set("LoggedUserId", user.Id);
        Preferences.Set("LoggedUsername", user.Username);
        Preferences.Set("IsAdmin", user.IsAdmin);

        MessagingCenter.Send<object>(this, "LoginChanged");

        await DisplayAlert("Success", $"Welcome, {user.Username}!", "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new RegisterPopup(_supa));
}

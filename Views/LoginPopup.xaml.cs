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
        var email = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Email and password are required.", "OK");
            return;
        }

        var user = await _supa.LoginAsync(email, password);

        if (user == null)
        {
            await DisplayAlert("Error", "Invalid email or password.", "OK");
            return;
        }

        MessagingCenter.Send<object>(this, "LoginChanged");

        var welcome = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName!;
        await DisplayAlert("Success", $"Welcome, {welcome}!", "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new RegisterPopup(_supa));
}

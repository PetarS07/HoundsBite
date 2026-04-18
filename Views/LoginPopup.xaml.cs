using HoundsBite.Models;
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

        // Disable the button to prevent double-tap freezing
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Logging in…";

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));

            LoginResult? result = null;
            try
            {
                result = await _supa.LoginWithResultAsync(email, password).WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                await DisplayAlert("Timeout", "The server took too long to respond. Check your connection and try again.", "OK");
                return;
            }

            if (result?.User == null)
            {
                await DisplayAlert("Error", result?.ErrorMessage ?? "Login failed.", "OK");
                return;
            }

            var welcome = string.IsNullOrWhiteSpace(result.User.DisplayName) ? result.User.Username : result.User.DisplayName!;
            await DisplayAlert("Success", $"Welcome, {welcome}!", "OK");
            await Navigation.PopModalAsync();
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Login";
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new RegisterPopup(_supa));
}

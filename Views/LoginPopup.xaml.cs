using HoundsBite.Models;
using HoundsBite.Services;
using Microsoft.Maui.Storage;

namespace HoundsBite.Views;

public partial class LoginPopup : ContentPage
{
    private readonly DatabaseService _db;

    public LoginPopup(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
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

        var user = await _db.Connection.Table<User>()
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

        if (user == null)
        {
            await DisplayAlert("Error", "Invalid username or password.", "OK");
            return;
        }

        Preferences.Set("LoggedUserId", user.Id); // ✅ запазваме кой е логнат

        await DisplayAlert("Success", $"Welcome, {user.Username}!", "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new RegisterPopup(_db));
}

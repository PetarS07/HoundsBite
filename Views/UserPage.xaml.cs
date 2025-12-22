using HoundsBite.Models;
using HoundsBite.Services;
using Microsoft.Maui.Storage;

namespace HoundsBite.Views;

public partial class UserPage : ContentPage
{
    private readonly DatabaseService _db;

    public UserPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;

        // Listen for login changes to update the UI
        MessagingCenter.Subscribe<object>(this, "LoginChanged", (sender) => UpdateUI());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUI();
    }

    private async void UpdateUI()
    {
        int userId = Preferences.Get("LoggedUserId", 0);
        bool isLoggedIn = userId > 0;

        LoginView.IsVisible = !isLoggedIn;
        ProfileView.IsVisible = isLoggedIn;

        if (isLoggedIn)
        {
            var user = await _db.GetUserByIdAsync(userId);
            PageTitle.Text = user != null ? user.Username : "Profile";
            WelcomeLabel.Text = user != null ? $"Logged in as {user.Username}" : "Welcome back";
        }
        else
        {
            PageTitle.Text = "Login";
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
        }
    }

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

        // Login Success
        Preferences.Set("LoggedUserId", user.Id);
        Preferences.Set("LoggedUsername", user.Username); // Store for Tab Title
        Preferences.Set("IsAdmin", user.IsAdmin);
        
        // Notify app
        MessagingCenter.Send<object>(this, "LoginChanged");
        
        // Clear fields
        UsernameEntry.Text = "";
        PasswordEntry.Text = "";
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Reuse existing Register logic via popup or create new page?
        // User asked to move login button, not necessarily rewrite everything.
        // But reusing the existing RegisterPopup is easiest.
        await Navigation.PushModalAsync(new RegisterPopup(_db));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
        if (confirm)
        {
            Preferences.Remove("LoggedUserId");
            MessagingCenter.Send<object>(this, "LoginChanged");
        }
    }
}

using HoundsBite.Services;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;

namespace HoundsBite.Views;

public partial class UserPage : ContentPage
{
    private readonly SupabaseService _supa;
    private bool _isDisplayNameValid = false;
    private bool _isPasswordValid = false;
    private bool _doPasswordsMatch = false;

    public UserPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;

        MessagingCenter.Subscribe<object>(this, "LoginChanged", (sender) => UpdateUI());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUI();
    }

    private async void UpdateUI()
    {
        await _supa.RestoreSessionFromStorageAsync();

        int userId = Preferences.Get("LoggedUserId", 0);
        bool isLoggedIn = userId > 0;

        LoginView.IsVisible = !isLoggedIn;
        ProfileView.IsVisible = isLoggedIn;

        if (isLoggedIn)
        {
            var user = await _supa.GetUserByIdAsync(userId);
            if (user != null)
            {
                // Fallback username logic
                string displayLabel = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
                
                PageTitle.Text = "Profile";
                WelcomeLabel.Text = $"Welcome back, {displayLabel}!";
                ReadOnlyEmailLabel.Text = user.Username; // Username represents the Email address under the hood
                
                DisplayNameEntry.Text = user.DisplayName;
            }
        }
        else
        {
            PageTitle.Text = "Login";
            LoginUsernameEntry.Text = string.Empty;
            LoginPasswordEntry.Text = string.Empty;
        }

        // Clear password fields logic on UI refresh
        OldPasswordEntry.Text = string.Empty;
        NewPasswordEntry.Text = string.Empty;
        ConfirmPasswordEntry.Text = string.Empty;
        PasswordReqPanel.IsVisible = false;
        PasswordReqToggle.IsVisible = false;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = LoginUsernameEntry.Text?.Trim() ?? "";
        var password = LoginPasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Email and password are required.", "OK");
            return;
        }

        var result = await _supa.LoginWithResultAsync(email, password);
        if (result.User == null)
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Login failed.", "OK");
            return;
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPopup(_supa));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
        if (confirm)
            await PerformLogoutAsync();
    }

    private async Task PerformLogoutAsync()
    {
        await _supa.SignOutAsync();
        Preferences.Remove("LoggedUserId");
        Preferences.Remove("LoggedEmail");
        Preferences.Remove("LoggedDisplayName");
        Preferences.Remove("IsAdminUser");
        MessagingCenter.Send<object>(this, "LoginChanged");
    }

    // ────────────────────────────────────────────────────────────────
    // DISPLAY NAME / USERNAME LOGIC
    // ────────────────────────────────────────────────────────────────

    private void OnDisplayNameTextChanged(object sender, TextChangedEventArgs e)
    {
        var name = e.NewTextValue ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            // Empty is allowed (falls back to email)
            UsernameReqToggle.IsVisible = false;
            UsernameReqPanel.IsVisible = false;
            _isDisplayNameValid = true;
            return;
        }

        UsernameReqToggle.IsVisible = true;

        bool hasLength = name.Length >= 3 && name.Length <= 20;
        bool hasValidChars = Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");

        ReqUserLength.TextColor = hasLength ? Colors.Green : Colors.Red;
        ReqUserChars.TextColor = hasValidChars ? Colors.Green : Colors.Red;

        _isDisplayNameValid = hasLength && hasValidChars;

        if (!_isDisplayNameValid)
        {
            UsernameReqPanel.IsVisible = true;
        }
    }

    private void OnUsernameReqTapped(object sender, TappedEventArgs e)
    {
        UsernameReqPanel.IsVisible = !UsernameReqPanel.IsVisible;
    }

    private async void OnSaveDisplayNameClicked(object sender, EventArgs e)
    {
        var name = DisplayNameEntry.Text?.Trim() ?? "";

        if (!string.IsNullOrEmpty(name) && !_isDisplayNameValid)
        {
            UsernameReqPanel.IsVisible = true;
            await DisplayAlert("Error", "Username does not meet requirements.", "OK");
            return;
        }

        int userId = Preferences.Get("LoggedUserId", 0);
        if (userId == 0) return;

        try
        {
            await _supa.UpdateUserDisplayNameAsync(userId, name);
            
            // Re-eval fallback display name
            string userEmail = Preferences.Get("LoggedEmail", "Profile");
            string finalDisplay = string.IsNullOrWhiteSpace(name) ? userEmail : name;
            Preferences.Set("LoggedDisplayName", finalDisplay);
            
            WelcomeLabel.Text = $"Welcome back, {finalDisplay}!";
            MessagingCenter.Send<object>(this, "LoginChanged"); // Notify other pages to update tabs/UI
            
            await DisplayAlert("Success", "Username updated successfully.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not save username: {ex.Message}", "OK");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // PASSWORD LOGIC
    // ────────────────────────────────────────────────────────────────

    private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        var password = e.NewTextValue ?? string.Empty;

        if (password.Length > 0)
        {
            PasswordReqToggle.IsVisible = true;
            
            bool hasLength = password.Length >= 8;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasNumber = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            ReqLength.TextColor = hasLength ? Colors.Green : Colors.Red;
            ReqUpper.TextColor = hasUpper ? Colors.Green : Colors.Red;
            ReqLower.TextColor = hasLower ? Colors.Green : Colors.Red;
            ReqNumber.TextColor = hasNumber ? Colors.Green : Colors.Red;
            ReqSpecial.TextColor = hasSpecial ? Colors.Green : Colors.Red;

            _isPasswordValid = hasLength && hasUpper && hasLower && hasNumber && hasSpecial;

            if (!_isPasswordValid)
            {
                PasswordReqPanel.IsVisible = true;
            }
        }
        else
        {
            PasswordReqToggle.IsVisible = false;
            PasswordReqPanel.IsVisible = false;
            _isPasswordValid = false;
        }

        CheckPasswordsMatch();
    }

    private void OnConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        CheckPasswordsMatch();
    }

    private void CheckPasswordsMatch()
    {
        var p1 = NewPasswordEntry.Text ?? "";
        var p2 = ConfirmPasswordEntry.Text ?? "";

        _doPasswordsMatch = (p1 == p2 && p1.Length > 0);
        
        if (p2.Length > 0 && !_doPasswordsMatch)
        {
            ConfirmPasswordError.IsVisible = true;
        }
        else
        {
            ConfirmPasswordError.IsVisible = false;
        }
    }

    private void OnPasswordReqTapped(object sender, TappedEventArgs e)
    {
        PasswordReqPanel.IsVisible = !PasswordReqPanel.IsVisible;
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        var oldPass = OldPasswordEntry.Text ?? "";
        var newPass = NewPasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
        {
            await DisplayAlert("Error", "All password fields are required.", "OK");
            return;
        }

        if (!_isPasswordValid)
        {
            PasswordReqPanel.IsVisible = true;
            await DisplayAlert("Error", "New password does not meet requirements.", "OK");
            return;
        }

        if (!_doPasswordsMatch)
        {
            await DisplayAlert("Error", "New passwords do not match.", "OK");
            return;
        }

        int userId = Preferences.Get("LoggedUserId", 0);
        if (userId == 0) return;

        try
        {
            var email = Preferences.Get("LoggedEmail", "");
            if (string.IsNullOrEmpty(email))
            {
                await DisplayAlert("Error", "Could not read your email. Please log in again.", "OK");
                return;
            }

            var ok = await _supa.ChangePasswordWithAuthAsync(email, oldPass, newPass);
            if (!ok)
            {
                await DisplayAlert("Error", "Incorrect current password or the new password could not be saved.", "OK");
                return;
            }

            await DisplayAlert("Success", "Password changed successfully. Please log in again.", "OK");

            await PerformLogoutAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not update password: {ex.Message}", "OK");
        }
    }
}

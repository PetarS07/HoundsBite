using HoundsBite.Services;
using System.Text.RegularExpressions;

namespace HoundsBite.Views;

public partial class RegisterPopup : ContentPage
{
    private readonly SupabaseService _supa;
    private bool _isEmailValid = false;
    private bool _isPasswordValid = false;

    public RegisterPopup(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
    {
        var email = e.NewTextValue ?? string.Empty;
        
        // Standard email Regex
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        
        if (string.IsNullOrWhiteSpace(email))
        {
            EmailErrorLabel.IsVisible = false;
            _isEmailValid = false;
        }
        else
        {
            _isEmailValid = Regex.IsMatch(email, emailPattern);
            EmailErrorLabel.IsVisible = !_isEmailValid;
        }
    }

    private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        var password = e.NewTextValue ?? string.Empty;
        
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

        if (!_isPasswordValid && password.Length > 0)
        {
            RequirementsPanel.IsVisible = true;
        }
    }

    private void OnRequirementsTapped(object sender, TappedEventArgs e)
    {
        RequirementsPanel.IsVisible = !RequirementsPanel.IsVisible;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "All fields are required.", "OK");
            return;
        }

        if (!_isEmailValid)
        {
            await DisplayAlert("Error", "Please enter a valid email address.", "OK");
            return;
        }

        if (!_isPasswordValid)
        {
            RequirementsPanel.IsVisible = true;
            await DisplayAlert("Error", "Password does not meet all requirements.", "OK");
            return;
        }

        var result = await _supa.RegisterUserAsync(email, password);

        if (result.NeedsEmailConfirmation)
        {
            await DisplayAlert(
                "Check your email",
                "We sent a confirmation link. After you confirm, you can sign in.",
                "OK");
            await Navigation.PopModalAsync();
            return;
        }

        if (result.User == null)
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Registration failed.", "OK");
            return;
        }

        await DisplayAlert(
            "Success",
            "Account created! You can now log in.",
            "OK");

        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();
}

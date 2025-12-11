using HoundsBite.Models;
using HoundsBite.Services;

namespace HoundsBite.Views;

public partial class RegisterPopup : ContentPage
{
    private readonly DatabaseService _db;

    public RegisterPopup(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "All fields are required.", "OK");
            return;
        }

        var exists = await _db.Connection.Table<User>()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (exists != null)
        {
            await DisplayAlert("Error", "Username already exists.", "OK");
            return;
        }

        // Check if there are any users in the DB — if none, this will be the first (make admin)
        var usersCount = await _db.Connection.Table<User>().CountAsync();
        var newUser = new User
        {
            Username = username,
            Password = password,
            IsAdmin = usersCount == 0 // първият става админ
        };

        await _db.Connection.InsertAsync(newUser);

        await DisplayAlert("Success", usersCount == 0 ? "Account created. You are the admin." : "Account created!", "OK");
        await Navigation.PopModalAsync();
    }
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

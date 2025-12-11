using HoundsBite.Services;

namespace HoundsBite.Views;

public partial class HomePage : ContentPage
{
    private readonly DatabaseService _db;

    public HomePage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshLoginStatus();
    }

    private async Task RefreshLoginStatus()
    {
        int userId = Preferences.Get("LoggedUserId", 0);

        if (userId == 0)
        {
            UserStatusLabel.Text = "Not logged in";
            LoginButton.IsVisible = true;
            LogoutButton.IsVisible = false;
            AdminIngredientsButton.IsVisible = false;
            AdminRecipesButton.IsVisible = false;
            return;
        }

        var user = await _db.GetUserByIdAsync(userId);

        if (user == null)
        {
            // Ако потребителят е изтрит или има проблем — почистваме
            Preferences.Remove("LoggedUserId");
            UserStatusLabel.Text = "Not logged in";
            LoginButton.IsVisible = true;
            LogoutButton.IsVisible = false;
            AdminIngredientsButton.IsVisible = false;
            AdminRecipesButton.IsVisible = false;
            return;
        }

    private async void OpenLogin(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new LoginPopup(_db));

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("LoggedUserId");
        await RefreshLoginStatus();
        await DisplayAlert("Logout", "You are now logged out.", "OK");
    }

    // Навигация: админските страници (админ бутоните отгоре)
    private async void GoToIngredients(object sender, EventArgs e)
    {
        // използваме shell route "ingredients" (не забравяй, че IngredientsPage трябва да е достъпна)
        await Shell.Current.GoToAsync("//ingredients");
    }

    private async void GoToRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//recipes");
}

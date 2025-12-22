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
            LoginButton.IsVisible = true;
            IngredientsCard.IsVisible = false; // Hide for non-users
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;
            return;
        }

        var user = await _db.GetUserByIdAsync(userId);

        if (user == null)
        {
            Preferences.Remove("LoggedUserId");
            LoginButton.IsVisible = true;
            IngredientsCard.IsVisible = false; // Hide for non-users
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;
            return;
        }

        LoginButton.IsVisible = false;
        
        // Show ingredients card for logged users
        IngredientsCard.IsVisible = true;

        // admin check
        AdminIngredientsCard.IsVisible = user.IsAdmin;
        AdminRecipesCard.IsVisible = user.IsAdmin;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//user");

    private async void GoToIngredients(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//admin-ingredients");

    private async void GoToRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//admin-recipes");

    private async void GoToDisplayIngredients(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//display-ingredients");

    private async void GoToDisplayRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//display-recipes");
}

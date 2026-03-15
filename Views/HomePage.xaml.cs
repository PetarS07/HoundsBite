using HoundsBite.Services;

namespace HoundsBite.Views;

public partial class HomePage : ContentPage
{
    private readonly SupabaseService _supa;

    public HomePage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
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
            IngredientsCard.IsVisible = false;
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;
            return;
        }

        var user = await _supa.GetUserByIdAsync(userId);

        if (user == null)
        {
            Preferences.Remove("LoggedUserId");
            LoginButton.IsVisible = true;
            IngredientsCard.IsVisible = false;
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;
            return;
        }

        LoginButton.IsVisible = false;
        IngredientsCard.IsVisible = true;
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

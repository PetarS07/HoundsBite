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
        await _supa.RestoreSessionFromStorageAsync();

        int userId = Preferences.Get("LoggedUserId", 0);

        if (userId == 0)
        {
            IngredientsCard.IsVisible = false;
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;

            // Center Recipes card when it's the only one
            Grid.SetColumn(RecipesCard, 0);
            Grid.SetColumnSpan(RecipesCard, 2);
            RecipesCard.HorizontalOptions = LayoutOptions.Center;
            RecipesCard.WidthRequest = 180;
            return;
        }

        var user = await _supa.GetUserByIdAsync(userId);

        if (user == null)
        {
            Preferences.Remove("LoggedUserId");
            IngredientsCard.IsVisible = false;
            AdminIngredientsCard.IsVisible = false;
            AdminRecipesCard.IsVisible = false;

            // Reset centering
            Grid.SetColumn(RecipesCard, 0);
            Grid.SetColumnSpan(RecipesCard, 2);
            RecipesCard.HorizontalOptions = LayoutOptions.Center;
            RecipesCard.WidthRequest = 180;
            return;
        }

        // Standard Layout
        IngredientsCard.IsVisible = true;
        Grid.SetColumn(RecipesCard, 1);
        Grid.SetColumnSpan(RecipesCard, 1);
        RecipesCard.HorizontalOptions = LayoutOptions.Fill;
        RecipesCard.WidthRequest = -1;

        AdminIngredientsCard.IsVisible = user.IsAdmin;
        AdminRecipesCard.IsVisible = user.IsAdmin;
    }

    private async void GoToIngredients(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//admin-ingredients");

    private async void GoToRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//admin-recipes");

    private async void GoToDisplayIngredients(object sender, EventArgs e)
    {
        if (Preferences.Get("LoggedUserId", 0) == 0)
        {
            await DisplayAlert(
                "Login required",
                "Please log in from Profile to view your kitchen inventory.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync("//display-ingredients");
    }

    private async void GoToDisplayRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//display-recipes");
}

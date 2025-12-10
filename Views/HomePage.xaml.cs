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

    private async void OpenLogin(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new LoginPopup(_db));

    private async void GoToIngredients(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//ingredients");

    private async void GoToRecipes(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//recipes");
}

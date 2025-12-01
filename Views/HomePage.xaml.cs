namespace HoundsBite.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void GoToIngredients(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ingredients");
    }

    private async void GoToRecipes(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//recipes");
    }
}
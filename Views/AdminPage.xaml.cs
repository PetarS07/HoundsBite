using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class AdminPage : ContentPage
{
    private readonly SupabaseService _supa;

    public AdminPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }



    private async void OnManageIngredientsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//admin-ingredients");
    }

    private async void OnManageRecipesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//admin-recipes");
    }
}

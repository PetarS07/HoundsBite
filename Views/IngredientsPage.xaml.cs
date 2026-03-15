using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class IngredientsPage : ContentPage
{
    private readonly SupabaseService _supa;
    private Ingredient? _editingIngredient = null;

    public IngredientsPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        IngredientList.ItemsSource = await _supa.GetAllIngredientsAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = IngredientEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        if (_editingIngredient == null)
        {
            await _supa.AddIngredientAsync(new Ingredient { Name = name });
        }
        else
        {
            _editingIngredient.Name = name;
            await _supa.UpdateIngredientAsync(_editingIngredient);
            _editingIngredient = null;
        }

        IngredientEntry.Text = "";
        await RefreshIngredients();
    }

    private void OnEditIngredient(object sender, EventArgs e)
    {
        var ingredient = (Ingredient)((Button)sender).CommandParameter;
        _editingIngredient = ingredient;
        IngredientEntry.Text = ingredient.Name;
    }

    private async Task RefreshIngredients()
    {
        IngredientList.ItemsSource = await _supa.GetAllIngredientsAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var ing = (Ingredient)((Button)sender).CommandParameter;
        await _supa.DeleteIngredientAsync(ing.Id);
        IngredientList.ItemsSource = await _supa.GetAllIngredientsAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}
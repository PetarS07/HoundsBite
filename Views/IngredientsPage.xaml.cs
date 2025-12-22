using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class IngredientsPage : ContentPage
{
    private readonly DatabaseService _db;
    private Ingredient _editingIngredient = null;

    public IngredientsPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        IngredientList.ItemsSource = await _db.Connection.Table<Ingredient>().ToListAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var name = IngredientEntry.Text?.Trim();

        if (string.IsNullOrEmpty(name))
            return;

        if (_editingIngredient == null)
        {
            // CREATE
            await _db.Connection.InsertAsync(new Ingredient { Name = name });
        }
        else
        {
            // UPDATE
            _editingIngredient.Name = name;
            await _db.Connection.UpdateAsync(_editingIngredient);

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
        IngredientList.ItemsSource = await _db.Connection.Table<Ingredient>().ToListAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var ing = (Ingredient)((Button)sender).CommandParameter;

        // delete links first
        var links = await _db.Connection.Table<RecipeIngredient>()
            .Where(x => x.IngredientId == ing.Id)
            .ToListAsync();

        foreach (var link in links)
            await _db.Connection.DeleteAsync(link);

        // delete ingredient
        await _db.Connection.DeleteAsync(ing);

        IngredientList.ItemsSource = await _db.Connection.Table<Ingredient>().ToListAsync();
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}
using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite;

public partial class RecipesPage : ContentPage
{
    private readonly DatabaseService _db;

    public RecipesPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        RecipeList.ItemsSource = await _db.Connection.Table<Recipe>().ToListAsync();
    }

    private async void OnAddRecipe(object sender, EventArgs e)
    {
        await _db.Connection.InsertAsync(new Recipe {
            Name = RecipeName.Text,
            Type = RecipeType.Text,
            Description = RecipeDesc.Text
        });

        RecipeList.ItemsSource = await _db.Connection.Table<Recipe>().ToListAsync();

        RecipeName.Text = "";
        RecipeType.Text = "";
        RecipeDesc.Text = "";
    }

    private async void OnDeleteRecipe(object sender, EventArgs e)
    {
        var recipe = (Recipe)((Button)sender).CommandParameter;
        await _db.Connection.DeleteAsync(recipe);
        RecipeList.ItemsSource = await _db.Connection.Table<Recipe>().ToListAsync();
    }
}
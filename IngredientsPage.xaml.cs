using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite;

public partial class IngredientsPage : ContentPage
{
    private readonly DatabaseService _db;

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
        await _db.Connection.InsertAsync(new Ingredient { Name = IngredientEntry.Text });
        IngredientList.ItemsSource = await _db.Connection.Table<Ingredient>().ToListAsync();
        IngredientEntry.Text = "";
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var ing = (Ingredient)((Button)sender).CommandParameter;
        await _db.Connection.DeleteAsync(ing);
        IngredientList.ItemsSource = await _db.Connection.Table<Ingredient>().ToListAsync();
    }
}
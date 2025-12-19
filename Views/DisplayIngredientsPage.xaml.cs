using HoundsBite.Services;
using System.Collections.ObjectModel;

namespace HoundsBite.Views;

public partial class DisplayIngredientsPage : ContentPage
{
    private readonly DatabaseService _db;
    public ObservableCollection<IngredientCheckModel> Ingredients { get; set; } = new();
    
    private int _currentUserId = 0;

    public class IngredientCheckModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public bool IsLoggedIn { get; set; }
    }

    public DisplayIngredientsPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
        IngredientList.ItemsSource = Ingredients;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadIngredients();
    }

    private async Task LoadIngredients()
    {
        Ingredients.Clear();
        var ingredients = await _db.Connection.Table<Models.Ingredient>().ToListAsync();
        
        // Check if user is logged in
        _currentUserId = Preferences.Get("LoggedUserId", 0);
        bool isLoggedIn = _currentUserId > 0;
        
        // Load user's selected ingredients from database
        List<int> userIngredientIds = new();
        if (isLoggedIn)
        {
            userIngredientIds = await _db.GetUserIngredientIdsAsync(_currentUserId);
        }
        
        foreach (var ingredient in ingredients)
        {
            Ingredients.Add(new IngredientCheckModel
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                IsSelected = userIngredientIds.Contains(ingredient.Id),
                IsLoggedIn = isLoggedIn
            });
        }
    }

    private async void OnIngredientCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_currentUserId == 0) return; // Not logged in
        
        var checkBox = (CheckBox)sender;
        var item = (IngredientCheckModel)checkBox.BindingContext;
        
        if (e.Value)
        {
            await _db.AddUserIngredientAsync(_currentUserId, item.Id);
        }
        else
        {
            await _db.RemoveUserIngredientAsync(_currentUserId, item.Id);
        }
    }
}

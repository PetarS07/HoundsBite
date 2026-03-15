namespace HoundsBite.Controls;

public enum NavTab { Home, Ingredients, Recipes, Profile }

public partial class NavBar : ContentView
{
    // ── Bindable Property ──────────────────────────────────────────────────
    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(NavTab), typeof(NavBar),
            NavTab.Home, propertyChanged: OnActiveTabChanged);

    public NavTab ActiveTab
    {
        get => (NavTab)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldVal, object newVal)
        => ((NavBar)bindable).ApplyActiveTab((NavTab)newVal);

    // ── Primary accent colour (matches app theme) ─────────────────────────
    private static readonly Color AccentBg = Color.FromArgb("#2A1F6E");   // deep violet tint
    private static readonly Color AccentFg = Color.FromArgb("#7C6FFF");   // primary purple
    private static readonly Color InactiveFg = Color.FromArgb("#888888");

    public NavBar()
    {
        InitializeComponent();
        ApplyActiveTab(NavTab.Home);
    }

    // ── Tab styling ────────────────────────────────────────────────────────
    private void ApplyActiveTab(NavTab tab)
    {
        // Reset all
        SetTab(HomePill, HomeIcon, HomeLabel, false);
        SetTab(IngredientsPill, IngredientsIcon, IngredientsLabel, false);
        SetTab(RecipesPill, RecipesIcon, RecipesLabel, false);
        SetTab(ProfilePill, ProfileIcon, ProfileLabel, false);

        // Highlight active
        switch (tab)
        {
            case NavTab.Home:        SetTab(HomePill, HomeIcon, HomeLabel, true); break;
            case NavTab.Ingredients: SetTab(IngredientsPill, IngredientsIcon, IngredientsLabel, true); break;
            case NavTab.Recipes:     SetTab(RecipesPill, RecipesIcon, RecipesLabel, true); break;
            case NavTab.Profile:     SetTab(ProfilePill, ProfileIcon, ProfileLabel, true); break;
        }
    }

    private static void SetTab(Border pill, Label icon, Label label, bool active)
    {
        pill.BackgroundColor = active ? AccentBg : Colors.Transparent;
        icon.TextColor = active ? AccentFg : InactiveFg;   // tint emoji container border colour
        label.TextColor = active ? AccentFg : InactiveFg;
        label.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
    }

    // ── Navigation handlers ────────────────────────────────────────────────
    private async void OnHomeTapped(object sender, TappedEventArgs e)
    {
        if (ActiveTab == NavTab.Home) return;
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnIngredientsTapped(object sender, TappedEventArgs e)
    {
        if (ActiveTab == NavTab.Ingredients) return;
        await Shell.Current.GoToAsync("//display-ingredients");
    }

    private async void OnRecipesTapped(object sender, TappedEventArgs e)
    {
        if (ActiveTab == NavTab.Recipes) return;
        await Shell.Current.GoToAsync("//display-recipes");
    }

    private async void OnProfileTapped(object sender, TappedEventArgs e)
    {
        if (ActiveTab == NavTab.Profile) return;
        await Shell.Current.GoToAsync("//user");
    }
}

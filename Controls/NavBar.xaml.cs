namespace HoundsBite.Controls;

public enum NavTab { Home, Ingredients, Recipes, Profile, Admin }

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
        UpdateLayout();
        ApplyActiveTab(NavTab.Home);

        // Listen for login changes to update tab visibility and column layout
        MessagingCenter.Subscribe<object>(this, "LoginChanged", (sender) =>
        {
            MainThread.BeginInvokeOnMainThread(UpdateLayout);
        });
    }

    private void UpdateLayout()
    {
        bool isLoggedIn = Preferences.Get("LoggedUserId", 0) != 0;
        bool isAdmin    = Preferences.Get("IsAdminUser", false);

        IngredientsTab.IsVisible = isLoggedIn;
        AdminTab.IsVisible       = isAdmin;

        // Dynamically reposition tabs and set the correct number of columns
        // so the bar always fills evenly with no gaps.
        if (isLoggedIn && isAdmin)
        {
            // 5 tabs: Home(0) Ingredients(1) Recipes(2) Profile(3) Admin(4)
            Grid.SetColumn(IngredientsTab, 1);
            Grid.SetColumn(RecipesTab,     2);
            Grid.SetColumn(ProfileTab,     3);
            Grid.SetColumn(AdminTab,       4);
            SetColumns(5);
        }
        else if (isLoggedIn)
        {
            // 4 tabs: Home(0) Ingredients(1) Recipes(2) Profile(3)
            Grid.SetColumn(IngredientsTab, 1);
            Grid.SetColumn(RecipesTab,     2);
            Grid.SetColumn(ProfileTab,     3);
            SetColumns(4);
        }
        else if (isAdmin)
        {
            // 4 tabs: Home(0) Recipes(1) Profile(2) Admin(3)
            Grid.SetColumn(RecipesTab, 1);
            Grid.SetColumn(ProfileTab, 2);
            Grid.SetColumn(AdminTab,   3);
            SetColumns(4);
        }
        else
        {
            // 3 tabs: Home(0) Recipes(1) Profile(2)
            Grid.SetColumn(RecipesTab, 1);
            Grid.SetColumn(ProfileTab, 2);
            SetColumns(3);
        }

    }

    private void SetColumns(int count)
    {
        var cols = new ColumnDefinitionCollection();
        for (int i = 0; i < count; i++)
            cols.Add(new ColumnDefinition(GridLength.Star));
        RootGrid.ColumnDefinitions = cols;
    }

    // ── Tab styling ────────────────────────────────────────────────────────
    private void ApplyActiveTab(NavTab tab)
    {
        // Reset all
        SetTab(HomePill, HomeIcon, HomeLabel, false);
        SetTab(IngredientsPill, IngredientsIcon, IngredientsLabel, false);
        SetTab(RecipesPill, RecipesIcon, RecipesLabel, false);
        SetTab(ProfilePill, ProfileIcon, ProfileLabel, false);
        SetTab(AdminPill, AdminIcon, AdminLabel, false);

        // Highlight active
        switch (tab)
        {
            case NavTab.Home:        SetTab(HomePill, HomeIcon, HomeLabel, true); break;
            case NavTab.Ingredients: SetTab(IngredientsPill, IngredientsIcon, IngredientsLabel, true); break;
            case NavTab.Recipes:     SetTab(RecipesPill, RecipesIcon, RecipesLabel, true); break;
            case NavTab.Profile:     SetTab(ProfilePill, ProfileIcon, ProfileLabel, true); break;
            case NavTab.Admin:       SetTab(AdminPill, AdminIcon, AdminLabel, true); break;
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
        if (Preferences.Get("LoggedUserId", 0) == 0)
        {
            await Shell.Current.DisplayAlert(
                "Login required",
                "Please log in from Profile to view your kitchen inventory.",
                "OK");
            return;
        }

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

    private async void OnAdminTapped(object sender, TappedEventArgs e)
    {
        if (ActiveTab == NavTab.Admin) return;
        await Shell.Current.GoToAsync("//admin-dashboard");
    }
}

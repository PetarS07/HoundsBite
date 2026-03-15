namespace HoundsBite
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register recipe detail route
            Routing.RegisterRoute("recipe-detail", typeof(Views.RecipeDetailPage));
            
            // Note: The previous TabBar visibility/title logic has been removed
            // because we now use a custom NavBar component on the individual pages.
        }
    }
}

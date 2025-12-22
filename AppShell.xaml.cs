namespace HoundsBite
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Subscribe to login changes
            MessagingCenter.Subscribe<object>(this, "LoginChanged", (sender) =>
            {
                MainThread.BeginInvokeOnMainThread(UpdateTabVisibility);
            });

            // Initial check safer on main thread
            MainThread.BeginInvokeOnMainThread(UpdateTabVisibility);
        }

        private async void UpdateTabVisibility()
        {
            try
            {
                int userId = Preferences.Get("LoggedUserId", 0);
                bool isLoggedIn = userId > 0;
            
                // Show/Hide Ingredients tab based on login status
                if (IngredientsTab != null)
                {
                    IngredientsTab.IsVisible = isLoggedIn;
                }

                // Update Login Tab Title
                if (UserTab != null)
                {
                    if (isLoggedIn)
                    {
                        string username = Preferences.Get("LoggedUsername", "Profile");
                        UserTab.Title = username;
                    }
                    else
                    {
                        UserTab.Title = "Login";
                    }
                }
            }
            catch (Exception ex)
            {
                // Prevent crash during startup
                System.Diagnostics.Debug.WriteLine($"Error updating tab visibility: {ex.Message}");
            }
        }
    }
}

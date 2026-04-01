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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPendingRecipes();
    }

    private async Task LoadPendingRecipes()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        PendingList.IsVisible = false;
        NoPendingLabel.IsVisible = false;

        try
        {
            var pending = await _supa.GetPendingRecipesAsync();
            PendingList.ItemsSource = pending;
            
            if (pending.Count == 0)
                NoPendingLabel.IsVisible = true;
            else
                PendingList.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load pending recipes: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnApproveClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Recipe recipe)
        {
            bool confirm = await DisplayAlert("Approve", $"Are you sure you want to approve '{recipe.Name}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                await _supa.UpdateRecipeStatusAsync(recipe.Id, "Approved");
                await LoadPendingRecipes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private async void OnRejectClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Recipe recipe)
        {
            bool confirm = await DisplayAlert("Reject", $"Are you sure you want to reject '{recipe.Name}'?", "Archive/Reject", "Cancel");
            if (!confirm) return;

            try
            {
                await _supa.UpdateRecipeStatusAsync(recipe.Id, "Rejected");
                await LoadPendingRecipes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
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

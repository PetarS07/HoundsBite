using HoundsBite.Services;
using HoundsBite.Views;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace HoundsBite;

public static class MauiProgram
{
    private const string SupabaseUrl = "https://oafubquvdrraaahzjtds.supabase.co";
    private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9hZnVicXV2ZHJyYWFhaHpqdGRzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzE3OTY1NjYsImV4cCI6MjA4NzM3MjU2Nn0.D8l4BsyDSfjv2vTbGB3aD2O7SwKDUskbiSHAQIm2r_0";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        // Register SupabaseService as a shared singleton (all devices use the same cloud DB)
        builder.Services.AddSingleton(new SupabaseService(SupabaseUrl, SupabaseKey));

        // Register pages so they receive SupabaseService via constructor injection
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<IngredientsPage>();
        builder.Services.AddTransient<RecipesPage>();
        builder.Services.AddTransient<DisplayIngredientsPage>();
        builder.Services.AddTransient<DisplayRecipesPage>();
        builder.Services.AddTransient<UserPage>();
        builder.Services.AddTransient<RecipeDetailPage>();

        return builder.Build();
    }
}
using HoundsBite.Services;
using HoundsBite.Views;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace HoundsBite;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "houndsbite.db");
        builder.Services.AddSingleton(new DatabaseService(dbPath));

        builder.Services.AddTransient<Views.IngredientsPage>();
        builder.Services.AddTransient<Views.RecipesPage>();
        builder.Services.AddTransient<Views.HomePage>();

        return builder.Build();
    }
}
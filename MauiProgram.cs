using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using HoundsBite.Services;

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

        builder.Services.AddTransient<IngredientsPage>();
        builder.Services.AddTransient<RecipesPage>();

        return builder.Build();
    }
}
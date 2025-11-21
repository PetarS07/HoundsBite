using SQLite;
using HoundsBite.Models;

namespace HoundsBite.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public DatabaseService(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
        _db.CreateTableAsync<Ingredient>().Wait();
        _db.CreateTableAsync<Recipe>().Wait();
        _db.CreateTableAsync<RecipeIngredient>().Wait();
    }

    public SQLiteAsyncConnection Connection => _db;
}
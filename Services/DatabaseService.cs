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
        _db.CreateTableAsync<User>().Wait();
    }

    public SQLiteAsyncConnection Connection => _db;

    public Task<User?> GetUserByIdAsync(int id)
    {
        return _db.Table<User>().FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        return _db.Table<User>().FirstOrDefaultAsync(u => u.Username == username);
    }
}
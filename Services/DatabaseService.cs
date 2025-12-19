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
        _db.CreateTableAsync<UserIngredient>().Wait();

        // Ensure IsAdmin column exists
        try
        {
            var check = _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE tbl_name = 'User' AND sql LIKE '%IsAdmin%'"
            ).Result;

            if (check == 0)
            {
                _db.ExecuteAsync("ALTER TABLE User ADD COLUMN IsAdmin INTEGER DEFAULT 0").Wait();
            }

            // Ensure UserId column exists in Recipe
            var checkRecipe = _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE tbl_name = 'Recipe' AND sql LIKE '%UserId%'"
            ).Result;

            if (checkRecipe == 0)
            {
                _db.ExecuteAsync("ALTER TABLE Recipe ADD COLUMN UserId INTEGER DEFAULT 0").Wait();
            }
        }
        catch
        {
            // ignore, column may already exist
        }
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

    public Task<int> GetUsersCountAsync()
    {
        return _db.Table<User>().CountAsync();
    }

    // UserIngredient methods
    public async Task<List<int>> GetUserIngredientIdsAsync(int userId)
    {
        var userIngredients = await _db.Table<UserIngredient>()
            .Where(ui => ui.UserId == userId)
            .ToListAsync();
        return userIngredients.Select(ui => ui.IngredientId).ToList();
    }

    public async Task AddUserIngredientAsync(int userId, int ingredientId)
    {
        var existing = await _db.Table<UserIngredient>()
            .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IngredientId == ingredientId);
        
        if (existing == null)
        {
            await _db.InsertAsync(new UserIngredient
            {
                UserId = userId,
                IngredientId = ingredientId
            });
        }
    }

    public async Task RemoveUserIngredientAsync(int userId, int ingredientId)
    {
        var existing = await _db.Table<UserIngredient>()
            .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IngredientId == ingredientId);
        
        if (existing != null)
        {
            await _db.DeleteAsync(existing);
        }
    }
}

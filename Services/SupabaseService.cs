using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoundsBite.Models;
using BC = BCrypt.Net.BCrypt;

namespace HoundsBite.Services;

public class SupabaseService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public SupabaseService(string supabaseUrl, string supabaseKey)
    {
        _baseUrl = supabaseUrl.TrimEnd('/') + "/rest/v1";
        _apiKey = supabaseKey;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("apikey", _apiKey);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _http.DefaultRequestHeaders.Add("Prefer", "return=representation");
    }

    private static JsonSerializerOptions JsonOpts => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ───────────── INGREDIENTS ─────────────

    public async Task<List<Ingredient>> GetAllIngredientsAsync()
    {
        var resp = await _http.GetAsync($"{_baseUrl}/ingredients?select=*&order=id");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<Ingredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts) ?? new();
    }

    public async Task<Ingredient?> GetIngredientByExternalIdAsync(string externalId)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/ingredients?external_id=eq.{Uri.EscapeDataString(externalId)}&select=*");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<Ingredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault();
    }

    public async Task<Ingredient> AddIngredientAsync(Ingredient ingredient)
    {
        var payload = new 
        {
            ingredient.Name,
            ingredient.ExternalId,
            ingredient.Category,
            ingredient.ImageUrl,
            ingredient.Calories,
            ingredient.Protein,
            ingredient.Carbs,
            ingredient.Fat
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_baseUrl}/ingredients", content);
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<Ingredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault() ?? ingredient;
    }

    public async Task UpdateIngredientAsync(Ingredient ingredient)
    {
        var json = JsonSerializer.Serialize(new { ingredient.Name }, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/ingredients?id=eq.{ingredient.Id}")
        {
            Content = content
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteIngredientAsync(int ingredientId)
    {
        var resp = await _http.DeleteAsync($"{_baseUrl}/recipe_ingredients?ingredient_id=eq.{ingredientId}");
        resp.EnsureSuccessStatusCode();
        resp = await _http.DeleteAsync($"{_baseUrl}/user_ingredients?ingredient_id=eq.{ingredientId}");
        resp.EnsureSuccessStatusCode();
        resp = await _http.DeleteAsync($"{_baseUrl}/ingredients?id=eq.{ingredientId}");
        resp.EnsureSuccessStatusCode();
    }

    // ───────────── RECIPES ─────────────

    public async Task<List<Recipe>> GetAllRecipesAsync()
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipes?select=*&order=id");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<Recipe>>(await resp.Content.ReadAsStringAsync(), JsonOpts) ?? new();
    }

    public async Task<Recipe?> GetRecipeByIdAsync(int id)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipes?id=eq.{id}&select=*");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<Recipe>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault();
    }

    public async Task<Recipe> AddRecipeAsync(Recipe recipe)
    {
        var payload = new
        {
            recipe.Name,
            recipe.Description,
            recipe.Type,
            recipe.UserId,
            recipe.Instructions,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            recipe.Difficulty,
            recipe.ImagePath
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_baseUrl}/recipes", content);
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<Recipe>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault() ?? recipe;
    }

    public async Task UpdateRecipeAsync(Recipe recipe)
    {
        var payload = new
        {
            recipe.Name,
            recipe.Description,
            recipe.Type,
            recipe.Instructions,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            recipe.Difficulty
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/recipes?id=eq.{recipe.Id}")
        {
            Content = content
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteRecipeAsync(int recipeId)
    {
        await _http.DeleteAsync($"{_baseUrl}/recipe_ingredients?recipe_id=eq.{recipeId}");
        await _http.DeleteAsync($"{_baseUrl}/recipes?id=eq.{recipeId}");
    }

    // ───────────── RECIPE INGREDIENTS ─────────────

    public async Task<List<RecipeIngredient>> GetAllRecipeIngredientsAsync()
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipe_ingredients?select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<RecipeIngredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts) ?? new();
    }

    public async Task<List<RecipeIngredient>> GetRecipeIngredientsByRecipeIdAsync(int recipeId)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipe_ingredients?recipe_id=eq.{recipeId}&select=*");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<RecipeIngredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts) ?? new();
    }

    public async Task DeleteRecipeIngredientsByRecipeIdAsync(int recipeId)
    {
        await _http.DeleteAsync($"{_baseUrl}/recipe_ingredients?recipe_id=eq.{recipeId}");
    }

    public async Task AddRecipeIngredientAsync(RecipeIngredient ri)
    {
        var payload = new { ri.RecipeId, ri.IngredientId, ri.Amount, ri.Unit };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await _http.PostAsync($"{_baseUrl}/recipe_ingredients", content);
    }

    // ───────────── FAVORITE RECIPES ─────────────

    public async Task<List<int>> GetFavoriteRecipeIdsAsync(int userId)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/favorite_recipes?user_id=eq.{userId}&select=recipe_id");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<FavoriteRecipe>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.Select(fr => fr.RecipeId).ToList() ?? new();
    }

    public async Task AddFavoriteRecipeAsync(int userId, int recipeId)
    {
        var payload = new { UserId = userId, RecipeId = recipeId };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/favorite_recipes")
        {
            Content = content
        };
        req.Headers.Remove("Prefer");
        req.Headers.Add("Prefer", "resolution=ignore-duplicates,return=representation");
        await _http.SendAsync(req);
    }

    public async Task RemoveFavoriteRecipeAsync(int userId, int recipeId)
    {
        await _http.DeleteAsync($"{_baseUrl}/favorite_recipes?user_id=eq.{userId}&recipe_id=eq.{recipeId}");
    }

    // ───────────── USERS ─────────────

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/users?id=eq.{id}&select=*");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<User>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault();
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/users?username=eq.{Uri.EscapeDataString(username)}&select=*");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<User>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault();
    }

    /// <summary>
    /// Fetches user by username, then verifies the BCrypt password hash client-side.
    /// Returns null if user is not found or the password is incorrect.
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null) return null;

        bool valid = BC.Verify(password, user.PasswordHash);
        return valid ? user : null;
    }

    public async Task<int> GetUsersCountAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Head, $"{_baseUrl}/users?select=*");
        req.Headers.Add("Prefer", "count=exact");
        var resp = await _http.SendAsync(req);
        if (resp.Headers.TryGetValues("content-range", out var vals))
        {
            var range = vals.FirstOrDefault() ?? "";
            var parts = range.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out var count))
                return count;
        }
        return 0;
    }

    /// <summary>
    /// Hashes the password with BCrypt before inserting into the database.
    /// The first registered user automatically becomes admin.
    /// </summary>
    public async Task<User> RegisterUserAsync(string username, string password)
    {
        var usersCount = await GetUsersCountAsync();
        var passwordHash = BC.HashPassword(password);

        var payload = new
        {
            Username = username,
            PasswordHash = passwordHash,
            IsAdmin = usersCount == 0  // first user is admin
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_baseUrl}/users", content);
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<User>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault() ?? new User { Username = username };
    }

    // ───────────── PROFILE MANAGEMENT ─────────────

    public async Task UpdateUserDisplayNameAsync(int userId, string displayName)
    {
        var payload = new { DisplayName = displayName };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/users?id=eq.{userId}")
        {
            Content = content
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserPasswordAsync(int userId, string newPasswordHash)
    {
        var payload = new { PasswordHash = newPasswordHash };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/users?id=eq.{userId}")
        {
            Content = content
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    // ───────────── USER INGREDIENTS ─────────────

    public async Task<List<int>> GetUserIngredientIdsAsync(int userId)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/user_ingredients?user_id=eq.{userId}&select=ingredient_id");
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<UserIngredient>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.Select(ui => ui.IngredientId).ToList() ?? new();
    }

    public async Task AddUserIngredientAsync(int userId, int ingredientId)
    {
        var payload = new { UserId = userId, IngredientId = ingredientId };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/user_ingredients")
        {
            Content = content
        };
        req.Headers.Remove("Prefer");
        req.Headers.Add("Prefer", "resolution=ignore-duplicates,return=representation");
        await _http.SendAsync(req);
    }

    public async Task RemoveUserIngredientAsync(int userId, int ingredientId)
    {
        await _http.DeleteAsync($"{_baseUrl}/user_ingredients?user_id=eq.{userId}&ingredient_id=eq.{ingredientId}");
    }
}

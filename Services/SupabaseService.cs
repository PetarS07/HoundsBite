using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoundsBite.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase.Gotrue.Exceptions;

namespace HoundsBite.Services;

public sealed record RegisterResult(User? User, string? ErrorMessage, bool NeedsEmailConfirmation);
public sealed record LoginResult(User? User, string? ErrorMessage, bool NeedsEmailConfirmation);

public class SupabaseService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _supabaseUrl;
    private readonly string _apiKey;
    private readonly SupabaseSessionPersistence _sessionPersistence;
    private Supabase.Client? _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    /// <summary>Stored in public.users when passwords are managed by Supabase Auth (not verified locally).</summary>
    private const string AuthManagedPasswordPlaceholder = "__managed_by_supabase_auth__";

    public SupabaseService(string supabaseUrl, string supabaseKey)
    {
        _supabaseUrl = supabaseUrl.TrimEnd('/');
        _baseUrl = _supabaseUrl + "/rest/v1";
        _apiKey = supabaseKey;
        _sessionPersistence = new SupabaseSessionPersistence();
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

    private async Task EnsureClientReadyAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false,
                AutoRefreshToken = true,
                SessionHandler = _sessionPersistence
            };
            _client = new Supabase.Client(_supabaseUrl, _apiKey, options);
            await _client.InitializeAsync();
            ApplyAuthHeadersFromSession();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void ApplyAuthHeadersFromSession()
    {
        var token = _client?.Auth.CurrentSession?.AccessToken ?? _apiKey;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string? EmailFromAuthSession(Supabase.Gotrue.Session? session)
    {
        var u = session?.User;
        if (u == null) return null;
        return !string.IsNullOrEmpty(u.Email) ? u.Email : u.Phone;
    }

    /// <summary>
    /// Call on startup so a restored Supabase session repopulates Preferences (LoggedUserId, etc.).
    /// </summary>
    public async Task RestoreSessionFromStorageAsync()
    {
        await EnsureClientReadyAsync();
        if (_client?.Auth.CurrentSession == null)
        {
            if (Preferences.Get("LoggedUserId", 0) != 0)
            {
                Preferences.Remove("LoggedUserId");
                Preferences.Remove("LoggedUsername");
                Preferences.Remove("LoggedEmail");
                Preferences.Remove("LoggedDisplayName");
                Preferences.Remove("IsAdminUser");
            }
            return;
        }

        ApplyAuthHeadersFromSession();
        if (Preferences.Get("LoggedUserId", 0) != 0)
            return;

        var email = EmailFromAuthSession(_client.Auth.CurrentSession);
        if (string.IsNullOrEmpty(email))
            return;

        var profile = await GetUserByUsernameAsync(email) ?? await InsertAppUserProfileAsync(email);
        ApplyPreferencesFromUser(profile);
        MessagingCenter.Send<object>(this, "LoginChanged");
    }

    private static void ApplyPreferencesFromUser(User user)
    {
        var displayLabel = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName!;
        Preferences.Set("LoggedUserId", user.Id);
        Preferences.Set("LoggedUsername", user.Username);
        Preferences.Set("LoggedEmail", user.Username);
        Preferences.Set("LoggedDisplayName", displayLabel);
        Preferences.Set("IsAdminUser", user.IsAdmin);
    }

    public async Task SignOutAsync()
    {
        await EnsureClientReadyAsync();
        if (_client != null)
            await _client.Auth.SignOut();
        ApplyAuthHeadersFromSession();
    }

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

    public async Task<Recipe?> GetRecipeByExternalIdAsync(string externalId)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipes?external_id=eq.{Uri.EscapeDataString(externalId)}&select=*");
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
            recipe.ExternalId,
            recipe.SourceUrl
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

    public async Task<List<Recipe>> GetPendingRecipesAsync()
    {
        var resp = await _http.GetAsync($"{_baseUrl}/recipes?status=eq.Pending&select=*&order=id");
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<List<Recipe>>(await resp.Content.ReadAsStringAsync(), JsonOpts) ?? new();
    }

    public async Task UpdateRecipeStatusAsync(int recipeId, string status)
    {
        // Note: The 'status' column is currently missing from the recipes table.
        // This method is a placeholder until the schema is updated.
        await Task.CompletedTask;
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
    /// Signs in with Supabase Auth (email + password), then loads the app profile row from public.users.
    /// </summary>
    public async Task<User?> LoginAsync(string email, string password)
    {
        var result = await LoginWithResultAsync(email, password);
        return result.User;
    }

    public async Task<LoginResult> LoginWithResultAsync(string email, string password)
    {
        await EnsureClientReadyAsync();
        if (_client == null)
            return new LoginResult(null, "Could not initialize auth.", false);

        try
        {
            await _client.Auth.SignInWithPassword(email, password);
        }
        catch (GotrueException ex)
        {
            var msg = ex.Message ?? "Login failed.";

            var needsConfirm =
                msg.Contains("confirm", StringComparison.OrdinalIgnoreCase) &&
                msg.Contains("email", StringComparison.OrdinalIgnoreCase);

            if (needsConfirm)
                return new LoginResult(null, "Please confirm your email address, then try again.", true);

            if (ex.StatusCode == 429 || msg.Contains("rate", StringComparison.OrdinalIgnoreCase))
                return new LoginResult(null, "Too many attempts. Please wait a bit and try again.", false);

            return new LoginResult(null, "Invalid email or password.", false);
        }

        ApplyAuthHeadersFromSession();
        var profile = await GetUserByUsernameAsync(email) ?? await InsertAppUserProfileAsync(email);
        ApplyPreferencesFromUser(profile);
        MessagingCenter.Send<object>(this, "LoginChanged");
        return new LoginResult(profile, null, false);
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
    /// Registers with Supabase Auth, then ensures a matching row exists in public.users for app data (favorites, etc.).
    /// </summary>
    public async Task<RegisterResult> RegisterUserAsync(string email, string password)
    {
        await EnsureClientReadyAsync();
        if (_client == null)
            return new RegisterResult(null, "Could not initialize auth.", false);

        try
        {
            var session = await _client.Auth.SignUp(email, password);
            if (session == null)
            {
                // When email confirmation is enabled, Supabase returns no session until confirmed.
                return new RegisterResult(null, null, true);
            }

            ApplyAuthHeadersFromSession();
            var profile = await GetUserByUsernameAsync(email) ?? await InsertAppUserProfileAsync(email);
            ApplyPreferencesFromUser(profile);
            MessagingCenter.Send<object>(this, "LoginChanged");
            return new RegisterResult(profile, null, false);
        }
        catch (GotrueException ex)
        {
            var msg = ex.Message ?? "Registration failed.";

            if (msg.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase) ||
                ex.StatusCode == 429)
            {
                return new RegisterResult(null,
                    "Too many registration attempts. Please wait a few minutes and try again.", false);
            }

            if (msg.Contains("already registered", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("User already registered", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("email_exists", StringComparison.OrdinalIgnoreCase))
            {
                return new RegisterResult(null, "This email is already registered.", false);
            }

            // Fallback: show a generic message rather than raw JSON
            return new RegisterResult(null, "Registration failed. Please try again later.", false);
        }

    }

    private async Task<User> InsertAppUserProfileAsync(string email)
    {
        var payload = new
        {
            Username = email,
            PasswordHash = AuthManagedPasswordPlaceholder,
            Role = "User"
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_baseUrl}/users", content);
        resp.EnsureSuccessStatusCode();
        var list = JsonSerializer.Deserialize<List<User>>(await resp.Content.ReadAsStringAsync(), JsonOpts);
        return list?.FirstOrDefault() ?? new User { Username = email };
    }

    /// <summary>
    /// Verifies the current password by re-authenticating, then sets the new password via Supabase Auth.
    /// </summary>
    public async Task<bool> ChangePasswordWithAuthAsync(string email, string currentPassword, string newPassword)
    {
        await EnsureClientReadyAsync();
        if (_client == null) return false;

        try
        {
            await _client.Auth.SignInWithPassword(email, currentPassword);
        }
        catch (GotrueException)
        {
            return false;
        }

        ApplyAuthHeadersFromSession();
        try
        {
            await _client.Auth.Update(new Supabase.Gotrue.UserAttributes { Password = newPassword });
        }
        catch (GotrueException)
        {
            return false;
        }

        return true;
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

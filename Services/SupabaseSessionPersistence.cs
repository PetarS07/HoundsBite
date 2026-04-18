using System.Text.Json;
using Microsoft.Maui.Storage;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace HoundsBite.Services;

/// <summary>
/// Persists Supabase Auth sessions so the user stays signed in across app restarts.
/// </summary>
public sealed class SupabaseSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string StorageKey = "houndsbite.supabase.session";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonSerializer.Serialize(session, JsonOpts);
            SecureStorage.Default.SetAsync(StorageKey, json).GetAwaiter().GetResult();
        }
        catch
        {
            // Persistence is best-effort; auth still works for the current process.
        }
    }

    public void DestroySession()
    {
        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch
        {
            // ignore
        }
    }

    public Session? LoadSession()
    {
        try
        {
            var json = SecureStorage.Default.GetAsync(StorageKey).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(json))
                return null;
            return JsonSerializer.Deserialize<Session>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}

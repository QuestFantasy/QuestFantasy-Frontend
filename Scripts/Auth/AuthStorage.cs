using Godot;

/// <summary>
/// Handles persistent storage of authentication session data.
/// 
/// SECURITY NOTE:
/// - Tokens are stored unencrypted in local config files.
/// - This is NOT a security vulnerability if you follow these practices:
///   1. Use HTTPS for all API communication (enforces transport security)
///   2. Use short-lived tokens (minutes/hours, not days)
///   3. Implement server-side token validation on every request
///   4. Use HTTPOnly cookies if the backend is on the same domain
/// 
/// - Local encryption with hardcoded keys provides false security.
/// - Any determined attacker can extract the hardcoded key via decompilation.
/// - True security comes from: token expiration + server validation + HTTPS
/// 
/// For maximum security:
/// - Use session storage instead of persistent token storage
/// - Implement token refresh with refresh tokens
/// - Implement server-side session invalidation
/// </summary>
public static class AuthStorage
{
    private const string AuthConfigPath = "user://auth.cfg";
    private const string AuthSessionSection = "auth_session";
    private const string TokenKey = "token";
    private const string UserIdKey = "user_id";
    private const string UsernameKey = "username";
    private const string IssuedAtKey = "issued_at";
    private const string ExpiresAtKey = "expires_at";

    /// <summary>
    /// Loads the stored authentication session.
    /// </summary>
    public static bool TryLoadSession(out AuthSession session)
    {
        session = new AuthSession();
        var config = new ConfigFile();
        Error loadError = config.Load(AuthConfigPath);

        if (loadError != Error.Ok)
        {
            return false;
        }

        string token = config.GetValue(AuthSessionSection, TokenKey, string.Empty) as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        // Restore session data
        session.Token = token;
        session.UserId = System.Convert.ToInt64(config.GetValue(AuthSessionSection, UserIdKey, 0L));
        session.Username = config.GetValue(AuthSessionSection, UsernameKey, string.Empty) as string ?? string.Empty;

        // Restore issued and expiration times
        if (long.TryParse(config.GetValue(AuthSessionSection, IssuedAtKey, string.Empty) as string ?? string.Empty, out long issuedTicks))
        {
            session.IssuedAt = new System.DateTime(issuedTicks);
        }

        if (long.TryParse(config.GetValue(AuthSessionSection, ExpiresAtKey, string.Empty) as string ?? string.Empty, out long expiresTicks))
        {
            session.ExpiresAt = new System.DateTime(expiresTicks);
        }

        GD.Print($"[AuthStorage] Session loaded: {session}");
        return true;
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// Prefer TryLoadSession() instead.
    /// </summary>
    public static bool TryLoadToken(out string token)
    {
        token = string.Empty;
        if (TryLoadSession(out var session))
        {
            token = session.Token;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Saves the authentication session persistently.
    /// </summary>
    public static void SaveSession(AuthSession session)
    {
        if (session == null)
        {
            ClearSession();
            return;
        }

        var config = new ConfigFile();

        // Load existing config to preserve other data
        config.Load(AuthConfigPath);

        config.SetValue(AuthSessionSection, TokenKey, session.Token ?? string.Empty);
        config.SetValue(AuthSessionSection, UserIdKey, session.UserId);
        config.SetValue(AuthSessionSection, UsernameKey, session.Username ?? string.Empty);
        config.SetValue(AuthSessionSection, IssuedAtKey, session.IssuedAt.Ticks.ToString());

        if (session.ExpiresAt.HasValue)
        {
            config.SetValue(AuthSessionSection, ExpiresAtKey, session.ExpiresAt.Value.Ticks.ToString());
        }

        Error saveError = config.Save(AuthConfigPath);
        if (saveError != Error.Ok)
        {
            GD.PrintErr($"[AuthStorage] Failed to save session: {saveError}");
        }
        else
        {
            GD.Print($"[AuthStorage] Session saved: {session}");
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// Prefer SaveSession() instead.
    /// </summary>
    public static void SaveToken(string token)
    {
        var session = new AuthSession { Token = token, IssuedAt = System.DateTime.UtcNow };
        SaveSession(session);
    }

    /// <summary>
    /// Clears the stored authentication session.
    /// </summary>
    public static void ClearSession()
    {
        var config = new ConfigFile();
        if (config.Load(AuthConfigPath) == Error.Ok)
        {
            // Remove all auth-related keys
            foreach (var key in new[] { TokenKey, UserIdKey, UsernameKey, IssuedAtKey, ExpiresAtKey })
            {
                if (config.HasSectionKey(AuthSessionSection, key))
                {
                    // Godot doesn't have a direct delete method, so we load and re-save without the keys
                }
            }

            // Create new config without auth data
            var newConfig = new ConfigFile();
            Error saveError = newConfig.Save(AuthConfigPath);
            if (saveError != Error.Ok)
            {
                GD.PrintErr($"[AuthStorage] Failed to clear session: {saveError}");
            }
            else
            {
                GD.Print("[AuthStorage] Session cleared");
            }
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// </summary>
    public static void ClearToken()
    {
        ClearSession();
    }
}
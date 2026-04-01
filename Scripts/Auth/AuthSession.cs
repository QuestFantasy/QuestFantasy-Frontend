using System;

/// <summary>
/// Represents an authenticated user session with token and expiration information.
/// Supports both opaque tokens and JWT-style tokens with expiration claims.
/// </summary>
public class AuthSession
{
    /// <summary>
    /// Unique authentication token for API requests.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Remote user ID from backend.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Remote username from backend.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Timestamp when the token was issued (UTC).
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Timestamp when the token expires (UTC).
    /// If null, token may not have expiration info (opaque token).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Determines if the session is currently valid (token not expired).
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Token))
                return false;

            if (ExpiresAt == null)
                return true; // No expiration info available

            return DateTime.UtcNow < ExpiresAt;
        }
    }

    /// <summary>
    /// Determines if the token is about to expire (within 5 minutes).
    /// Useful for proactive token refresh.
    /// </summary>
    public bool IsExpiringSoon
    {
        get
        {
            if (ExpiresAt == null)
                return false;

            TimeSpan timeUntilExpiry = ExpiresAt.Value - DateTime.UtcNow;
            return timeUntilExpiry.TotalMinutes < 5;
        }
    }

    /// <summary>
    /// Time remaining until token expiration.
    /// Returns null if expiration time is unknown.
    /// </summary>
    public TimeSpan? TimeUntilExpiry
    {
        get
        {
            if (ExpiresAt == null)
                return null;

            TimeSpan remaining = ExpiresAt.Value - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Clears all sensitive session data.
    /// </summary>
    public void Clear()
    {
        Token = string.Empty;
        UserId = 0;
        Username = string.Empty;
        IssuedAt = DateTime.MinValue;
        ExpiresAt = null;
    }

    public override string ToString()
    {
        return $"AuthSession(User={Username}, Valid={IsValid}, ExpiresIn={TimeUntilExpiry?.TotalMinutes:F1}m)";
    }
}
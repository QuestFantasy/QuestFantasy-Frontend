using System;
using System.Collections.Generic;

using Godot;

/// <summary>
/// Validates and extracts information from authentication tokens.
/// Supports JWT-style tokens and provides expiration information.
/// </summary>
public class TokenValidator
{
    /// <summary>
    /// Attempts to extract JWT claims from a token.
    /// JWT format: header.payload.signature
    /// </summary>
    public static bool TryExtractClaims(string token, out Dictionary<string, object> claims)
    {
        claims = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            // JWT tokens have 3 parts separated by dots
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                // Not a JWT token (might be opaque token), which is valid
                return false;
            }

            // Decode the payload (second part)
            // Note: Base64 decode might need padding
            string payload = parts[1];

            // Add padding if necessary
            int paddingNeeded = (4 - (payload.Length % 4)) % 4;
            payload += new string('=', paddingNeeded);

            // Base64 decode
            byte[] decodedBytes = System.Convert.FromBase64String(payload);
            string decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);

            // Parse JSON using Godot's static JSON class
            JSONParseResult parseResult = JSON.Parse(decodedJson);
            if (parseResult.Error != Error.Ok)
            {
                GD.PrintErr("Failed to parse JWT payload: " + parseResult.Error);
                return false;
            }

            var dict = parseResult.Result as Godot.Collections.Dictionary;
            if (dict == null)
            {
                return false;
            }

            // Convert Godot Dictionary to C# Dictionary
            claims = new Dictionary<string, object>();
            foreach (object keyObj in dict.Keys)
            {
                string key = keyObj as string;
                if (!string.IsNullOrEmpty(key))
                {
                    claims[key] = dict[keyObj];
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error extracting JWT claims: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Attempts to extract expiration time from a JWT token.
    /// </summary>
    public static bool TryGetExpirationTime(string token, out DateTime expiresAt)
    {
        expiresAt = DateTime.MinValue;

        if (!TryExtractClaims(token, out var claims))
        {
            return false;
        }

        // Look for standard JWT expiration claim
        if (claims.TryGetValue("exp", out object expObj))
        {
            if (expObj is long expUnix || (expObj is int expInt && (expUnix = expInt) > 0))
            {
                // Convert Unix timestamp to DateTime
                expiresAt = UnixTimeStampToDateTime(expUnix);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract user ID from a JWT token.
    /// Common claim names: "sub", "user_id", "uid"
    /// </summary>
    public static bool TryGetUserId(string token, out long userId)
    {
        userId = 0;

        if (!TryExtractClaims(token, out var claims))
        {
            return false;
        }

        // Try common user ID claim names
        foreach (var claimName in new[] { "sub", "user_id", "uid", "id" })
        {
            if (claims.TryGetValue(claimName, out object value))
            {
                if (value is long id || (value is int intId && (id = intId) > 0))
                {
                    userId = id;
                    return true;
                }

                // Try parsing string value
                if (value is string strValue && long.TryParse(strValue, out long parsedId))
                {
                    userId = parsedId;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract username from a JWT token.
    /// Common claim names: "username", "name", "preferred_username"
    /// </summary>
    public static bool TryGetUsername(string token, out string username)
    {
        username = string.Empty;

        if (!TryExtractClaims(token, out var claims))
        {
            return false;
        }

        // Try common username claim names
        foreach (var claimName in new[] { "username", "preferred_username", "name" })
        {
            if (claims.TryGetValue(claimName, out object value))
            {
                if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
                {
                    username = strValue;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a token is currently valid (not expired).
    /// </summary>
    public static bool IsTokenValid(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        // Try to get expiration time
        if (TryGetExpirationTime(token, out var expiresAt))
        {
            return DateTime.UtcNow < expiresAt;
        }

        // If no expiration info, assume token is valid (opaque token)
        return true;
    }

    /// <summary>
    /// Determines if a token is expiring soon (within threshold).
    /// </summary>
    public static bool IsTokenExpiringSoon(string token, float thresholdMinutes = 5f)
    {
        if (!TryGetExpirationTime(token, out var expiresAt))
        {
            return false;
        }

        TimeSpan timeUntilExpiry = expiresAt - DateTime.UtcNow;
        return timeUntilExpiry.TotalMinutes < thresholdMinutes && timeUntilExpiry.TotalSeconds > 0;
    }

    /// <summary>
    /// Gets time remaining until token expiration.
    /// </summary>
    public static TimeSpan? GetTimeUntilExpiry(string token)
    {
        if (!TryGetExpirationTime(token, out var expiresAt))
        {
            return null;
        }

        TimeSpan remaining = expiresAt - DateTime.UtcNow;
        return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        try
        {
            // Unix timestamp is milliseconds or seconds, try both
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            // If value is very large, it's likely milliseconds
            if (unixTimeStamp > 100000000000)
            {
                dateTime = dateTime.AddMilliseconds(unixTimeStamp);
            }
            else
            {
                dateTime = dateTime.AddSeconds(unixTimeStamp);
            }

            return dateTime;
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error converting Unix timestamp: " + ex.Message);
            return DateTime.MinValue;
        }
    }
}
using System;

using Godot;

/// <summary>
/// Rate limiter to prevent login brute force attacks.
/// Implements exponential backoff after multiple failed attempts.
/// </summary>
public class LoginRateLimiter
{
    private int _failedAttempts = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _lockoutUntil = DateTime.MinValue;

    /// <summary>
    /// Maximum failed attempts before temporary lockout.
    /// </summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>
    /// Initial lockout duration in seconds (after 5 failures).
    /// </summary>
    private const float InitialLockoutSeconds = 30f;

    /// <summary>
    /// Maximum lockout duration in seconds.
    /// </summary>
    private const float MaxLockoutSeconds = 300f; // 5 minutes

    /// <summary>
    /// Multiplier for exponential backoff.
    /// </summary>
    private const float BackoffMultiplier = 1.5f;

    /// <summary>
    /// Reset failed attempts if no failure for this many seconds.
    /// </summary>
    private const float ResetTimeoutSeconds = 60f;

    /// <summary>
    /// Whether the rate limiter is currently in lockout.
    /// </summary>
    public bool IsLocked => DateTime.UtcNow < _lockoutUntil;

    /// <summary>
    /// Seconds remaining until lockout expires.
    /// Returns 0 if not locked.
    /// </summary>
    public float SecondsUntilUnlock
    {
        get
        {
            if (!IsLocked)
                return 0f;
            return (float)(_lockoutUntil - DateTime.UtcNow).TotalSeconds;
        }
    }

    /// <summary>
    /// Current number of failed attempts.
    /// </summary>
    public int FailedAttempts => _failedAttempts;

    /// <summary>
    /// Records a failed login attempt.
    /// Returns true if attempt should be allowed, false if rate limited.
    /// </summary>
    public bool RecordFailedAttempt()
    {
        DateTime now = DateTime.UtcNow;

        // Check if lockout has expired
        if (IsLocked)
        {
            return false;
        }

        // Reset counter if enough time has passed since last failure
        if ((now - _lastFailureTime).TotalSeconds > ResetTimeoutSeconds)
        {
            _failedAttempts = 0;
        }

        _failedAttempts++;
        _lastFailureTime = now;

        // Trigger lockout after max attempts
        if (_failedAttempts >= MaxFailedAttempts)
        {
            float lockoutDuration = CalculateLockoutDuration();
            _lockoutUntil = now.AddSeconds(lockoutDuration);
            GD.PrintErr($"[LoginRateLimiter] Lockout triggered: {lockoutDuration}s");
            return false;
        }

        GD.Print($"[LoginRateLimiter] Failed attempt {_failedAttempts}/{MaxFailedAttempts}");
        return true;
    }

    /// <summary>
    /// Records a successful login attempt and resets the counter.
    /// </summary>
    public void RecordSuccessfulAttempt()
    {
        _failedAttempts = 0;
        _lastFailureTime = DateTime.MinValue;
        _lockoutUntil = DateTime.MinValue;
        GD.Print("[LoginRateLimiter] Success recorded, rate limiter reset");
    }

    /// <summary>
    /// Manually resets the rate limiter (e.g., after user enters correct CAPTCHA).
    /// </summary>
    public void Reset()
    {
        _failedAttempts = 0;
        _lastFailureTime = DateTime.MinValue;
        _lockoutUntil = DateTime.MinValue;
        GD.Print("[LoginRateLimiter] Manually reset");
    }

    /// <summary>
    /// Calculates lockout duration with exponential backoff.
    /// </summary>
    private float CalculateLockoutDuration()
    {
        // Calculate how many full cycles of max attempts we've completed
        int cycles = (_failedAttempts - MaxFailedAttempts) / MaxFailedAttempts;
        float duration = InitialLockoutSeconds * Mathf.Pow(BackoffMultiplier, cycles);
        return Mathf.Min(duration, MaxLockoutSeconds);
    }

    public override string ToString()
    {
        if (IsLocked)
        {
            return $"LoginRateLimiter(LOCKED: {SecondsUntilUnlock:F1}s remaining)";
        }
        return $"LoginRateLimiter({_failedAttempts}/{MaxFailedAttempts} attempts)";
    }
}
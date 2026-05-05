using System;

using Godot;

/// <summary>
/// Orchestrates the authentication flow including login, registration, token validation, and logout.
/// Implements security features like rate limiting and token expiration checking.
/// </summary>
public class AuthFlowController : Node
{
    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    public event Action Authenticated;
    public event Action LoggedOut;
    public event Action AuthViewShown;

    private AuthApiClient _authApiClient;
    private AuthView _authView;
    private AuthSession _currentSession = new AuthSession();
    private readonly LoginRateLimiter _rateLimiter = new LoginRateLimiter();

    /// <summary>
    /// Current authenticated session (read-only for external observers).
    /// </summary>
    public AuthSession CurrentSession => _currentSession;

    public override void _Ready()
    {
        SetupModules();
        StartAuthenticationFlow();
    }

    /// <summary>
    /// Periodic check for token expiration (called from _PhysicsProcess or a timer).
    /// </summary>
    public override void _PhysicsProcess(float delta)
    {
        if (!_currentSession.IsValid)
        {
            return;
        }

        // Check if token is expiring soon and proactively refresh
        if (_currentSession.IsExpiringSoon)
        {
            GD.PrintErr("[Auth] Token expiring soon, attempting re-validation");
            ValidateSavedToken();
        }
    }

    private void SetupModules()
    {
        _authApiClient = new AuthApiClient
        {
            BackendBaseUrl = BackendBaseUrl
        };
        AddChild(_authApiClient);

        _authView = new AuthView();
        AddChild(_authView);

        _authView.LoginSubmitted += OnLoginSubmitted;
        _authView.RegisterSubmitted += OnRegisterSubmitted;
    }

    private void StartAuthenticationFlow()
    {
        if (AuthStorage.TryLoadSession(out var loadedSession) && loadedSession.IsValid)
        {
            _currentSession = loadedSession;
            _authView.SetStatus("Validating saved login session...");
            ValidateSavedToken();
            return;
        }

        // Loaded session is expired or doesn't exist
        if (loadedSession != null && loadedSession.Token != null && !loadedSession.IsValid)
        {
            GD.Print("[Auth] Loaded session is expired, clearing...");
            AuthStorage.ClearSession();
        }

        _authView.SetStatus("Please log in or register to continue.");
        ShowAuthView();
    }

    private void OnLoginSubmitted(string credential, string password)
    {
        if (_authApiClient.IsBusy)
        {
            _authView.SetStatus("Request already in progress. Please wait.");
            return;
        }

        // Check rate limiting
        if (_rateLimiter.IsLocked)
        {
            float secondsRemaining = _rateLimiter.SecondsUntilUnlock;
            _authView.SetStatus($"Too many failed attempts. Try again in {secondsRemaining:F0} seconds.");
            return;
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(credential) || string.IsNullOrWhiteSpace(password))
        {
            _authView.SetStatus("Please enter both username/email and password.");
            return;
        }

        SetAuthBusy(true);
        _authView.SetStatus("Logging in...");
        if (!_authApiClient.Login(credential, password, OnLoginCompleted))
        {
            SetAuthBusy(false);
            _authView.SetStatus("Failed to send login request. Please try again.");
        }
    }

    private void OnRegisterSubmitted(string username, string email, string password, string confirmPassword)
    {
        if (_authApiClient.IsBusy)
        {
            _authView.SetStatus("Request already in progress. Please wait.");
            return;
        }

        // Check rate limiting
        if (_rateLimiter.IsLocked)
        {
            float secondsRemaining = _rateLimiter.SecondsUntilUnlock;
            _authView.SetStatus($"Too many failed attempts. Try again in {secondsRemaining:F0} seconds.");
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            _authView.SetStatus("Password and confirmation must match.");
            return;
        }

        SetAuthBusy(true);
        _authView.SetStatus("Registering...");
        if (!_authApiClient.Register(username, email, password, confirmPassword, OnRegisterCompleted))
        {
            SetAuthBusy(false);
            _authView.SetStatus("Failed to send registration request. Please try again.");
        }
    }

    private void ValidateSavedToken()
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentSession.Token))
        {
            _authView.SetStatus("No valid session found.");
            ShowAuthView();
            return;
        }

        SetAuthBusy(true);
        if (!_authApiClient.ValidateToken(_currentSession.Token, OnValidateTokenCompleted))
        {
            SetAuthBusy(false);
            _authView.SetStatus("Failed to validate session. Please try again.");
        }
    }

    private void OnValidateTokenCompleted(AuthApiResult result)
    {
        SetAuthBusy(false);

        if (!result.NetworkOk)
        {
            _authView.SetStatus(result.ErrorMessage);
            ShowAuthView();
            return;
        }

        if (result.ResponseCode == 200)
        {
            // Token is still valid
            _authView.HideView();
            Authenticated?.Invoke();
            return;
        }

        // Token is invalid or expired (e.g., 401 Unauthorized)
        if (result.ResponseCode == 401 || result.ResponseCode == 403)
        {
            AuthStorage.ClearSession();
            _currentSession.Clear();
            _authView.ClearInputs();
            _authView.SetStatus("Your session has expired. Please log in again.");
            ShowAuthView();
            LoggedOut?.Invoke();
            return;
        }

        // Other errors
        _authView.SetStatus(result.GetApiErrorMessage("Session validation failed. Please log in again."));
        ShowAuthView();
    }

    private void OnLoginCompleted(AuthApiResult result)
    {
        HandleAuthTokenResponse(result, 200, "Login failed. Please check your credentials.");
    }

    private void OnRegisterCompleted(AuthApiResult result)
    {
        HandleAuthTokenResponse(result, 201, "Registration failed. Please try again.");
    }

    private void HandleAuthTokenResponse(AuthApiResult result, int expectedCode, string fallbackErrorMessage)
    {
        SetAuthBusy(false);

        if (!result.NetworkOk)
        {
            _rateLimiter.RecordFailedAttempt();
            _authView.SetStatus(result.ErrorMessage);
            ShowAuthView();
            return;
        }

        if (!result.IsSuccessStatus(expectedCode))
        {
            _rateLimiter.RecordFailedAttempt();
            _authView.SetStatus(result.GetApiErrorMessage(fallbackErrorMessage));
            ShowAuthView();
            return;
        }

        string token = result.GetString("token");
        if (string.IsNullOrWhiteSpace(token))
        {
            _rateLimiter.RecordFailedAttempt();
            _authView.SetStatus("The backend did not return a token. Please try again later.");
            ShowAuthView();
            return;
        }

        // Build session from token and response data
        _currentSession = new AuthSession
        {
            Token = token,
            IssuedAt = DateTime.UtcNow
        };

        // Try to extract user info from token (if JWT)
        if (TokenValidator.TryGetUserId(token, out long userId))
        {
            _currentSession.UserId = userId;
        }

        if (TokenValidator.TryGetUsername(token, out string username))
        {
            _currentSession.Username = username;
        }

        // Try to extract expiration time
        if (TokenValidator.TryGetExpirationTime(token, out var expiresAt))
        {
            _currentSession.ExpiresAt = expiresAt;
        }

        // Save session
        AuthStorage.SaveSession(_currentSession);

        // Mark rate limiter success
        _rateLimiter.RecordSuccessfulAttempt();

        GD.Print($"[Auth] Authentication successful: {_currentSession}");

        _authView.SetStatus("Verifying session...");
        ValidateSavedToken();
    }

    public void RequestLogout()
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentSession.Token))
        {
            FinalizeLogout("You have been logged out.");
            return;
        }

        _authView.SetStatus("Logging out...");
        if (!_authApiClient.Logout(_currentSession.Token, OnLogoutCompleted))
        {
            _authView.SetStatus("Could not send logout request. Please try again.");
        }
    }

    private void OnLogoutCompleted(AuthApiResult result)
    {
        if (!result.NetworkOk)
        {
            GD.PrintErr("Logout request failed: " + result.ErrorMessage);
            _authView.SetStatus("Network issue during logout. Logging out locally anyway.");
            FinalizeLogout("Logged out (offline).");
            return;
        }

        // 200 = success, 204 = no content (also success)
        // 401 = already logged out or invalid token (treat as success)
        if (result.ResponseCode == 200 || result.ResponseCode == 204 || result.ResponseCode == 401)
        {
            FinalizeLogout("Logout successful. Please log in to continue.");
            return;
        }

        // Other errors
        _authView.SetStatus(result.GetApiErrorMessage("Logout failed. Please try again."));
    }

    private void FinalizeLogout(string message)
    {
        // Clear all sensitive data
        AuthStorage.ClearSession();
        _currentSession.Clear();
        _authView.ClearInputs();
        _authView.SetStatus(message);
        ShowAuthView();

        // Reset rate limiter
        _rateLimiter.Reset();

        // Emit event
        LoggedOut?.Invoke();

        GD.Print("[Auth] Logout completed");
    }

    private void ShowAuthView()
    {
        _authView.ShowView();
        AuthViewShown?.Invoke();
    }

    private void SetAuthBusy(bool isBusy)
    {
        _authView?.SetInteractable(!isBusy);
    }
}
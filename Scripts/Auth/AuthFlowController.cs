using Godot;
using System;

public class AuthFlowController : Node
{
    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    public event Action Authenticated;
    public event Action LoggedOut;

    private AuthApiClient _authApiClient;
    private AuthView _authView;
    private string _authToken = string.Empty;

    public override void _Ready()
    {
        SetupModules();
        StartAuthenticationFlow();
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
        if (AuthStorage.TryLoadToken(out string token) && !string.IsNullOrWhiteSpace(token))
        {
            _authToken = token;
            _authView.SetStatus("Validating saved login session...");
            ValidateSavedToken();
            return;
        }

        _authView.SetStatus("Please log in or register to continue.");
        _authView.ShowView();
    }

    private void OnLoginSubmitted(string credential, string password)
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        SetAuthBusy(true);
        _authView.SetStatus("Logging in...");
        if (!_authApiClient.Login(credential, password, OnLoginCompleted))
        {
            SetAuthBusy(false);
            _authView.SetStatus("Another request is already in progress.");
        }
    }

    private void OnRegisterSubmitted(string username, string email, string password, string confirmPassword)
    {
        if (_authApiClient.IsBusy)
        {
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
            _authView.SetStatus("Another request is already in progress.");
        }
    }

    private void ValidateSavedToken()
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        SetAuthBusy(true);
        if (!_authApiClient.ValidateToken(_authToken, OnValidateTokenCompleted))
        {
            SetAuthBusy(false);
            _authView.SetStatus("Another request is already in progress.");
        }
    }

    private void OnValidateTokenCompleted(AuthApiResult result)
    {
        SetAuthBusy(false);

        if (!result.NetworkOk)
        {
            _authView.SetStatus(result.ErrorMessage);
            _authView.ShowView();
            return;
        }

        if (result.ResponseCode == 200)
        {
            _authView.HideView();
            Authenticated?.Invoke();
            return;
        }

        AuthStorage.ClearToken();
        _authToken = string.Empty;
        _authView.ClearInputs();
        _authView.SetStatus("Your session is no longer valid. Please log in again.");
        _authView.ShowView();
    }

    private void OnLoginCompleted(AuthApiResult result)
    {
        HandleAuthTokenResponse(result, 200, "Login failed. Please check your input.");
    }

    private void OnRegisterCompleted(AuthApiResult result)
    {
        HandleAuthTokenResponse(result, 201, "Registration failed. Please check your input.");
    }

    private void HandleAuthTokenResponse(AuthApiResult result, int expectedCode, string fallbackErrorMessage)
    {
        SetAuthBusy(false);

        if (!result.NetworkOk)
        {
            _authView.SetStatus(result.ErrorMessage);
            _authView.ShowView();
            return;
        }

        if (!result.IsSuccessStatus(expectedCode))
        {
            _authView.SetStatus(result.GetApiErrorMessage(fallbackErrorMessage));
            _authView.ShowView();
            return;
        }

        string token = result.GetString("token");
        if (string.IsNullOrWhiteSpace(token))
        {
            _authView.SetStatus("The backend did not return a token. Please try again later.");
            _authView.ShowView();
            return;
        }

        _authToken = token;
        AuthStorage.SaveToken(_authToken);
        _authView.SetStatus("Verifying session...");
        ValidateSavedToken();
    }

    public void RequestLogout()
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_authToken))
        {
            FinalizeLogout("You have been logged out.");
            return;
        }

        if (!_authApiClient.Logout(_authToken, OnLogoutCompleted))
        {
            _authView.SetStatus("Could not send logout request. Please try again.");
        }
    }

    private void OnLogoutCompleted(AuthApiResult result)
    {
        if (!result.NetworkOk)
        {
            GD.PrintErr("Logout request failed: " + result.ErrorMessage);
            _authView.SetStatus("Network issue during logout. Please try again.");
            return;
        }

        if (!result.IsSuccessStatus(200, 401))
        {
            _authView.SetStatus(result.GetApiErrorMessage("Logout failed. Please try again."));
            return;
        }

        FinalizeLogout("Logout successful. Please log in to continue.");
    }

    private void FinalizeLogout(string message)
    {
        AuthStorage.ClearToken();
        _authToken = string.Empty;
        _authView.ClearInputs();
        _authView.SetStatus(message);
        _authView.ShowView();
        LoggedOut?.Invoke();
    }

    private void SetAuthBusy(bool isBusy)
    {
        _authView?.SetInteractable(!isBusy);
    }
}

using Godot;
using System;

public class AuthFlowController : Node
{
    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    public event Action Authenticated;

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

    private void OnRegisterSubmitted(string username, string email, string password)
    {
        if (_authApiClient.IsBusy)
        {
            return;
        }

        SetAuthBusy(true);
        _authView.SetStatus("Registering...");
        if (!_authApiClient.Register(username, email, password, OnRegisterCompleted))
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

    private void SetAuthBusy(bool isBusy)
    {
        _authView?.SetInteractable(!isBusy);
    }
}

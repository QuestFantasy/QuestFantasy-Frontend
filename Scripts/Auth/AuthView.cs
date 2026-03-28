using Godot;
using System;

public class AuthView : CanvasLayer
{
    public event Action<string, string> LoginSubmitted;
    public event Action<string, string, string, string> RegisterSubmitted;

    private Label _statusLabel;

    private LineEdit _loginCredentialInput;
    private LineEdit _loginPasswordInput;
    private Button _loginSubmitButton;

    private LineEdit _registerUsernameInput;
    private LineEdit _registerEmailInput;
    private LineEdit _registerPasswordInput;
    private LineEdit _registerConfirmPasswordInput;
    private Button _registerSubmitButton;

    public override void _Ready()
    {
        BuildUi();
    }

    public void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
        }
    }

    public void SetInteractable(bool isEnabled)
    {
        if (_loginSubmitButton != null)
        {
            _loginSubmitButton.Disabled = !isEnabled;
        }

        if (_registerSubmitButton != null)
        {
            _registerSubmitButton.Disabled = !isEnabled;
        }
    }

    public void ShowView()
    {
        Visible = true;
    }

    public void HideView()
    {
        Visible = false;
        SetStatus(string.Empty);
    }

    public void ClearInputs()
    {
        if (_loginCredentialInput != null)
        {
            _loginCredentialInput.Text = string.Empty;
        }

        if (_loginPasswordInput != null)
        {
            _loginPasswordInput.Text = string.Empty;
        }

        if (_registerUsernameInput != null)
        {
            _registerUsernameInput.Text = string.Empty;
        }

        if (_registerEmailInput != null)
        {
            _registerEmailInput.Text = string.Empty;
        }

        if (_registerPasswordInput != null)
        {
            _registerPasswordInput.Text = string.Empty;
        }

        if (_registerConfirmPasswordInput != null)
        {
            _registerConfirmPasswordInput.Text = string.Empty;
        }
    }

    private void BuildUi()
    {
        var root = new Control();
        root.AnchorRight = 1f;
        root.AnchorBottom = 1f;
        AddChild(root);

        var center = new CenterContainer();
        center.AnchorRight = 1f;
        center.AnchorBottom = 1f;
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.RectMinSize = new Vector2(500f, 0f);
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddConstantOverride("margin_left", 22);
        margin.AddConstantOverride("margin_right", 22);
        margin.AddConstantOverride("margin_top", 18);
        margin.AddConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 10);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = "QuestFantasy";
        title.Align = Label.AlignEnum.Center;
        vbox.AddChild(title);

        /*
        var subtitle = new Label();
        subtitle.Text = "Log in to start exploring the map";
        subtitle.Align = Label.AlignEnum.Center;
        vbox.AddChild(subtitle);
        */

        var authTabs = new TabContainer();
        authTabs.RectMinSize = new Vector2(0f, 260f);
        vbox.AddChild(authTabs);

        BuildLoginTab(authTabs);
        BuildRegisterTab(authTabs);

        _statusLabel = new Label();
        _statusLabel.Autowrap = true;
        _statusLabel.Text = string.Empty;
        vbox.AddChild(_statusLabel);
    }

    private void BuildLoginTab(TabContainer authTabs)
    {
        var loginTab = new VBoxContainer();
        loginTab.Name = "Login";
        loginTab.AddConstantOverride("separation", 8);
        authTabs.AddChild(loginTab);

        _loginCredentialInput = new LineEdit();
        _loginCredentialInput.PlaceholderText = "Username or Email";
        loginTab.AddChild(_loginCredentialInput);

        _loginPasswordInput = new LineEdit();
        _loginPasswordInput.PlaceholderText = "Password";
        _loginPasswordInput.Secret = true;
        loginTab.AddChild(_loginPasswordInput);

        _loginSubmitButton = new Button();
        _loginSubmitButton.Text = "Log In";
        _loginSubmitButton.Connect("pressed", this, nameof(OnLoginPressed));
        loginTab.AddChild(_loginSubmitButton);
    }

    private void BuildRegisterTab(TabContainer authTabs)
    {
        var registerTab = new VBoxContainer();
        registerTab.Name = "Register";
        registerTab.AddConstantOverride("separation", 8);
        authTabs.AddChild(registerTab);

        _registerUsernameInput = new LineEdit();
        _registerUsernameInput.PlaceholderText = "Username";
        registerTab.AddChild(_registerUsernameInput);

        _registerEmailInput = new LineEdit();
        _registerEmailInput.PlaceholderText = "Email";
        registerTab.AddChild(_registerEmailInput);

        _registerPasswordInput = new LineEdit();
        _registerPasswordInput.PlaceholderText = "Password (at least 8 characters)";
        _registerPasswordInput.Secret = true;
        registerTab.AddChild(_registerPasswordInput);

        _registerConfirmPasswordInput = new LineEdit();
        _registerConfirmPasswordInput.PlaceholderText = "Confirm password";
        _registerConfirmPasswordInput.Secret = true;
        registerTab.AddChild(_registerConfirmPasswordInput);

        _registerSubmitButton = new Button();
        _registerSubmitButton.Text = "Register and Log In";
        _registerSubmitButton.Connect("pressed", this, nameof(OnRegisterPressed));
        registerTab.AddChild(_registerSubmitButton);
    }

    private void OnLoginPressed()
    {
        string credential = _loginCredentialInput?.Text?.Trim() ?? string.Empty;
        string password = _loginPasswordInput?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(credential) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Please enter your username (or email) and password.");
            return;
        }

        LoginSubmitted?.Invoke(credential, password);
    }

    private void OnRegisterPressed()
    {
        string username = _registerUsernameInput?.Text?.Trim() ?? string.Empty;
        string email = _registerEmailInput?.Text?.Trim() ?? string.Empty;
        string password = _registerPasswordInput?.Text ?? string.Empty;
        string confirmPassword = _registerConfirmPasswordInput?.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(confirmPassword))
        {
            SetStatus("Please fill in username, email, password, and password confirmation.");
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            SetStatus("Password and confirmation must match.");
            return;
        }

        RegisterSubmitted?.Invoke(username, email, password, confirmPassword);
    }
}

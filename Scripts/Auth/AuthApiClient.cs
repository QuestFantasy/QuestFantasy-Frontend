using Godot;
using System;
using System.Collections.Generic;

public class AuthApiResult
{
    public bool NetworkOk { get; set; }
    public int ResponseCode { get; set; }
    public string ErrorMessage { get; set; }
    public Godot.Collections.Dictionary Data { get; set; }

    public bool IsSuccessStatus(params int[] expectedCodes)
    {
        for (int i = 0; i < expectedCodes.Length; i++)
        {
            if (ResponseCode == expectedCodes[i])
            {
                return true;
            }
        }

        return false;
    }

    public string GetString(string key)
    {
        if (Data == null || !Data.Contains(key) || Data[key] == null)
        {
            return string.Empty;
        }

        return Data[key].ToString();
    }

    public string GetApiErrorMessage(string fallback)
    {
        if (Data == null)
        {
            return fallback;
        }

        string detail = GetString("detail");
        if (!string.IsNullOrWhiteSpace(detail))
        {
            return detail;
        }

        string message = GetString("message");
        if (!string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        if (Data.Contains("non_field_errors")
            && Data["non_field_errors"] is Godot.Collections.Array nonFieldErrors
            && nonFieldErrors.Count > 0)
        {
            return nonFieldErrors[0].ToString();
        }

        return fallback;
    }
}

public class AuthApiClient : Node
{
    private enum AuthRequestKind
    {
        None,
        ValidateToken,
        Login,
        Register
    }

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private HTTPRequest _request;
    private AuthRequestKind _pendingKind = AuthRequestKind.None;
    private Action<AuthApiResult> _pendingCallback;

    public bool IsBusy => _pendingKind != AuthRequestKind.None;

    public override void _Ready()
    {
        _request = new HTTPRequest();
        AddChild(_request);
        _request.Connect("request_completed", this, nameof(OnRequestCompleted));
    }

    public bool ValidateToken(string token, Action<AuthApiResult> callback)
    {
        return SendRequest(
            AuthRequestKind.ValidateToken,
            "/api/auth/me/",
            HTTPClient.Method.Get,
            null,
            token,
            callback);
    }

    public bool Login(string credential, string password, Action<AuthApiResult> callback)
    {
        var payload = new Godot.Collections.Dictionary();
        if (credential.Contains("@"))
        {
            payload["email"] = credential;
        }
        else
        {
            payload["username"] = credential;
        }

        payload["password"] = password;

        return SendRequest(
            AuthRequestKind.Login,
            "/api/auth/login/",
            HTTPClient.Method.Post,
            payload,
            null,
            callback);
    }

    public bool Register(string username, string email, string password, Action<AuthApiResult> callback)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["username"] = username,
            ["email"] = email,
            ["password"] = password
        };

        return SendRequest(
            AuthRequestKind.Register,
            "/api/auth/register/",
            HTTPClient.Method.Post,
            payload,
            null,
            callback);
    }

    private bool SendRequest(
        AuthRequestKind kind,
        string endpointPath,
        HTTPClient.Method method,
        Godot.Collections.Dictionary payload,
        string authorizationToken,
        Action<AuthApiResult> callback)
    {
        if (IsBusy)
        {
            return false;
        }

        string url = BuildApiUrl(endpointPath);
        var headers = new List<string> { "Accept: application/json" };

        if (!string.IsNullOrWhiteSpace(authorizationToken))
        {
            headers.Add("Authorization: Token " + authorizationToken);
        }

        string requestBody = string.Empty;
        if (payload != null)
        {
            headers.Add("Content-Type: application/json");
            requestBody = JSON.Print(payload);
        }

        _pendingKind = kind;
        _pendingCallback = callback;

        Error err = _request.Request(url, headers.ToArray(), true, method, requestBody);
        if (err != Error.Ok)
        {
            CompleteRequest(new AuthApiResult
            {
                NetworkOk = false,
                ResponseCode = 0,
                ErrorMessage = "Cannot connect to backend server: " + err,
                Data = null
            });
            return false;
        }

        return true;
    }

    private void OnRequestCompleted(int result, int responseCode, string[] headers, byte[] body)
    {
        string bodyText = body != null ? System.Text.Encoding.UTF8.GetString(body) : string.Empty;

        CompleteRequest(new AuthApiResult
        {
            NetworkOk = result == (int)HTTPRequest.Result.Success,
            ResponseCode = responseCode,
            ErrorMessage = result == (int)HTTPRequest.Result.Success
                ? string.Empty
                : "Network request failed. Please make sure the backend server is running.",
            Data = ParseJsonDictionary(bodyText)
        });
    }

    private void CompleteRequest(AuthApiResult result)
    {
        Action<AuthApiResult> callback = _pendingCallback;
        _pendingCallback = null;
        _pendingKind = AuthRequestKind.None;

        callback?.Invoke(result);
    }

    private string BuildApiUrl(string endpointPath)
    {
        return BackendBaseUrl.TrimEnd('/') + endpointPath;
    }

    private static Godot.Collections.Dictionary ParseJsonDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JSONParseResult parsed = JSON.Parse(json);
        if (parsed.Error != Error.Ok)
        {
            return null;
        }

        return parsed.Result as Godot.Collections.Dictionary;
    }
}

using System;
using System.Collections.Generic;

using Godot;

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

        var messages = new List<string>();
        CollectErrorMessages(Data, null, messages, 0);

        if (messages.Count <= 0)
        {
            return fallback;
        }

        var uniqueMessages = new List<string>();
        for (int i = 0; i < messages.Count; i++)
        {
            string msg = messages[i];
            if (string.IsNullOrWhiteSpace(msg))
            {
                continue;
            }

            bool exists = false;
            for (int j = 0; j < uniqueMessages.Count; j++)
            {
                if (string.Equals(uniqueMessages[j], msg, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                uniqueMessages.Add(msg);
            }
        }

        if (uniqueMessages.Count <= 0)
        {
            return fallback;
        }

        if (uniqueMessages.Count == 1)
        {
            return uniqueMessages[0];
        }

        return string.Join("\n", uniqueMessages.ToArray());
    }

    private static void CollectErrorMessages(object value, string fieldName, List<string> output, int depth)
    {
        if (value == null || output == null || depth > 4)
        {
            return;
        }

        if (value is Godot.Collections.Dictionary dict)
        {
            foreach (object keyObj in dict.Keys)
            {
                string key = keyObj?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                // Never expose backend status code in UI.
                if (string.Equals(key, "status_code", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                object next = dict[key];

                // Unwrap generic error container from backend exception handler.
                if (string.Equals(key, "error", StringComparison.OrdinalIgnoreCase)
                    && (next is Godot.Collections.Dictionary || next is Godot.Collections.Array))
                {
                    CollectErrorMessages(next, fieldName, output, depth + 1);
                    continue;
                }

                if (string.Equals(key, "error", StringComparison.OrdinalIgnoreCase))
                {
                    CollectErrorMessages(next, null, output, depth + 1);
                    continue;
                }

                // Top-level generic message keys are displayed without field prefix.
                if (string.Equals(key, "detail", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "message", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "non_field_errors", StringComparison.OrdinalIgnoreCase))
                {
                    CollectErrorMessages(next, null, output, depth + 1);
                    continue;
                }

                CollectErrorMessages(next, key, output, depth + 1);
            }

            return;
        }

        if (value is Godot.Collections.Array arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                CollectErrorMessages(arr[i], fieldName, output, depth + 1);
            }

            return;
        }

        string raw = value.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string cleaned = CleanMessage(raw);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return;
        }

        string label = ToDisplayFieldName(fieldName);
        output.Add(string.IsNullOrWhiteSpace(label) ? cleaned : (label + ": " + cleaned));
    }

    private static string ToDisplayFieldName(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return string.Empty;
        }

        string normalized = fieldName.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "username":
                return "Username";
            case "email":
                return "Email";
            case "password":
                return "Password";
            case "confirm_password":
                return "Confirm password";
            default:
                return HumanizeFieldName(fieldName);
        }
    }

    private static string HumanizeFieldName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Replace("_", " ").Trim();
        if (normalized.Length <= 0)
        {
            return string.Empty;
        }

        return char.ToUpper(normalized[0]) + normalized.Substring(1);
    }

    private static string CleanMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        string text = raw.Trim();

        // Common DRF serializer-to-string format: ['message']
        if (text.Length >= 4 && text.StartsWith("['") && text.EndsWith("']"))
        {
            text = text.Substring(2, text.Length - 4).Trim();
        }
        else if (text.Length >= 4 && text.StartsWith("[\"") && text.EndsWith("\"]"))
        {
            text = text.Substring(2, text.Length - 4).Trim();
        }

        return text;
    }
}

public class AuthApiClient : Node
{
    private enum AuthRequestKind
    {
        None,
        ValidateToken,
        Login,
        Register,
        Logout,
        FetchPlayerProfile,
        UpdatePlayerProfile
    }

    [Export] public string BackendBaseUrl = "http://127.0.0.1:8000";

    private HTTPRequest _request;
    private AuthRequestKind _pendingKind = AuthRequestKind.None;
    private Action<AuthApiResult> _pendingCallback;

    public bool IsBusy => _pendingKind != AuthRequestKind.None;

    public override void _Ready()
    {
        _request = new HTTPRequest();
        _request.PauseMode = PauseModeEnum.Process;
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

    public bool Register(string username, string email, string password, string confirmPassword, Action<AuthApiResult> callback)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["username"] = username,
            ["email"] = email,
            ["password"] = password,
            ["confirm_password"] = confirmPassword
        };

        return SendRequest(
            AuthRequestKind.Register,
            "/api/auth/register/",
            HTTPClient.Method.Post,
            payload,
            null,
            callback);
    }

    public bool Logout(string token, Action<AuthApiResult> callback)
    {
        return SendRequest(
            AuthRequestKind.Logout,
            "/api/auth/logout/",
            HTTPClient.Method.Post,
            null,
            token,
            callback);
    }

    public bool FetchPlayerProfile(string token, Action<AuthApiResult> callback)
    {
        return SendRequest(
            AuthRequestKind.FetchPlayerProfile,
            "/api/player/profile/",
            HTTPClient.Method.Get,
            null,
            token,
            callback);
    }

    public bool UpdatePlayerProfile(string token, Godot.Collections.Dictionary payload, Action<AuthApiResult> callback)
    {
        return SendRequest(
            AuthRequestKind.UpdatePlayerProfile,
            "/api/player/profile/",
            HTTPClient.Method.Patch,
            payload,
            token,
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
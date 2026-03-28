using Godot;

public static class AuthStorage
{
    private const string AuthConfigPath = "user://auth.cfg";
    private const string AuthConfigPassword = "QuestFantasy.Auth.Token.v1";

    public static bool TryLoadToken(out string token)
    {
        token = string.Empty;
        var config = new ConfigFile();
        Error loadError = config.LoadEncryptedPass(AuthConfigPath, AuthConfigPassword);
        if (loadError != Error.Ok)
        {
            return false;
        }

        object value = config.GetValue("auth", "token", string.Empty);
        token = value != null ? value.ToString() : string.Empty;
        return !string.IsNullOrWhiteSpace(token);
    }

    public static void SaveToken(string token)
    {
        var config = new ConfigFile();
        config.SetValue("auth", "token", token ?? string.Empty);
        Error saveError = config.SaveEncryptedPass(AuthConfigPath, AuthConfigPassword);
        if (saveError != Error.Ok)
        {
            GD.PrintErr("Failed to save auth token: " + saveError);
        }
    }

    public static void ClearToken()
    {
        SaveToken(string.Empty);
    }
}

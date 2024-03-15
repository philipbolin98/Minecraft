using UnityEngine;

public static class Settings {
    public static string appPath = Application.persistentDataPath;
    public const string settingsJson = "settings.json";
    public static string settingsPath = $"{appPath}/{settingsJson}";
    public const int maxThreadPoolThreads = 8;
    public static GameSettings gameSettings;
}

public static class DefaultGameSettings {
    public const int renderDistance = 16;
    public const float mouseSensitivity = 3;
}
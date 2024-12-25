using UnityEngine;

public static class Settings {
    public static string appPath = Application.persistentDataPath;
    public static string settingsPath = $"{appPath}/settings.json";
    public static string savesPath = $"{appPath}/saves";
    public const int maxThreadPoolThreads = 16;
    public static GameSettings gameSettings;
}

public static class DefaultGameSettings {
    public const int renderDistance = 32;
    public const float mouseSensitivity = 3;
}
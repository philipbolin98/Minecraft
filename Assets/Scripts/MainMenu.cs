using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {

    public GameObject mainMenu;
    public GameObject newGameMenu;
    public GameObject loadGameMenu;
    public GameObject settingsMenu;
    public TextMeshProUGUI seedInput;
    public Slider renderDistanceSlider;
    public Slider mouseSensitivitySlider;

    public void Start() {
        try {

            LoadSettingsJson();

        } catch (Exception ex) {
            Debug.LogError("Error in Menu Start: " + ex.Message);
        }
    }

    public void LoadSettingsJson() {
        try {

            if (!File.Exists(Settings.settingsPath)) {

                Settings.gameSettings = new GameSettings() {
                    renderDistance = DefaultGameSettings.renderDistance,
                    mouseSensitivity = DefaultGameSettings.mouseSensitivity
                };

                SaveSettingsJson();

            } else {

                string json = File.ReadAllText(Settings.settingsPath);
                Settings.gameSettings = JsonUtility.FromJson<GameSettings>(json);
            }

            renderDistanceSlider.value = Settings.gameSettings.renderDistance;
            mouseSensitivitySlider.value = Settings.gameSettings.mouseSensitivity;

        } catch (Exception ex) {
            Debug.LogError("Error in Menu LoadSettingsJson: " + ex.Message);
        }
    }

    public void SaveSettingsJson() {
        try {

            string json = JsonUtility.ToJson(Settings.gameSettings);
            File.WriteAllText(Settings.settingsPath, json);

        } catch (Exception ex) {
            Debug.LogError("Error in Menu SaveSettingsJson: " + ex.Message);
        }
    }

    public void QuitApplication() {
        try {

            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif

        } catch (Exception ex) {
            Debug.LogError("Error in Menu QuitApplication: " + ex.Message);
        }
    }

    public void ShowMenu(string menuName) {
        try {

            switch (menuName.ToLower()) {
                case "main":

                    mainMenu.SetActive(true);
                    settingsMenu.SetActive(false);
                    newGameMenu.SetActive(false);
                    loadGameMenu.SetActive(false);
                    break;

                case "newgame":

                    mainMenu.SetActive(false);
                    newGameMenu.SetActive(true);
                    break;

                case "loadgame":

                    mainMenu.SetActive(false);
                    loadGameMenu.SetActive(true);
                    break;

                case "settings":

                    mainMenu.SetActive(false);
                    settingsMenu.SetActive(true);
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Menu ShowMenu: " + ex.Message);
        }
    }

    public void CreateNewWorld() {
        try {

            string seed = seedInput.text.Trim();

            if (seed == "") {
                Planet.seed = UnityEngine.Random.Range(0, int.MaxValue);
            } else {
                Planet.seed = seed.GetHashCode();
            }

            SceneManager.LoadScene("Planet");

        } catch (Exception ex) {
            Debug.LogError("Error in Menu CreateNewWorld: " + ex.Message);
        }
    }

    public void UpdateSetting(string settingName) {
        try {

            switch (settingName.ToLower()) {
                case "renderdistance":
                    Settings.gameSettings.renderDistance = (int)renderDistanceSlider.value;
                    break;
                case "mousesensitivity":
                    Settings.gameSettings.mouseSensitivity = (int)mouseSensitivitySlider.value;
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Menu UpdateSetting: " + ex.Message);
        }
    }
}

public class GameSettings {
    public int renderDistance;
    public float mouseSensitivity;
}
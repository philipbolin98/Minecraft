using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour {

    public GameObject pauseMenu;
    public GameObject settingsMenu;
    public Slider renderDistanceSlider;
    public Slider mouseSensitivitySlider;

    private Planet planet;

    public void Start() {
        try {

            planet = GameObject.Find("Planet").GetComponent<Planet>();

        } catch (Exception ex) {
            Debug.LogError("Error in PauseMenu Start: " + ex.Message);
        }
    }

    public void ShowMenu(string menuName) {
        try {

            switch (menuName.ToLower()) {
                case "main":

                    pauseMenu.SetActive(true);
                    settingsMenu.SetActive(false);
                    break;

                case "settings":

                    pauseMenu.SetActive(false);
                    settingsMenu.SetActive(true);
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in PauseMenu ShowMenu: " + ex.Message);
        }
    }

    public void UpdateSetting(string settingName) {
        try {

            switch (settingName.ToLower()) {
                case "renderdistance":
                    Settings.gameSettings.renderDistance = (int)renderDistanceSlider.value;
                    this.planet.CheckRenderDistance(this.planet.player.transform.position);
                    break;
                case "mousesensitivity":
                    Settings.gameSettings.mouseSensitivity = (int)mouseSensitivitySlider.value;
                    break;
            }

        } catch (Exception ex) {
            Debug.LogError("Error in PauseMenu UpdateSetting: " + ex.Message);
        }
    }

    public void SaveAndQuit() {
        try {

            SaveSystem.SavePlanet(this.planet);
            SceneManager.LoadScene("MainMenu");

        } catch (Exception ex) {
            Debug.LogError("Error in Planet SaveAndQuit: " + ex.Message);
        }
    }
}
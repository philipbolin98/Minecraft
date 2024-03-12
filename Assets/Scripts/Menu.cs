using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour {

    public GameObject mainMenu;
    public GameObject newGameMenu;
    public GameObject loadGameMenu;
    public GameObject settingsMenu;
    public TextMeshProUGUI seedInput;

    public void Start() {
        try {

            //mainMenu = GameObject.Find("Main Menu");
            //settings = GameObject.Find("Settings");

        } catch (Exception ex) {
            Debug.LogError("Error in Menu Start: " + ex.Message);
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
                    newGameMenu.SetActive(false);
                    loadGameMenu.SetActive(false);
                    settingsMenu.SetActive(false);
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
}
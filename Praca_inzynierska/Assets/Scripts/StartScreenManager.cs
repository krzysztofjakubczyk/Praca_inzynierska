using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class StartScreenManager : MonoBehaviour
{
    public GameObject startScreen;
    public Button startButton;
    public Canvas CanvasToReturnButton;
    public Button returnButton;
    public TMP_Dropdown simulationModeDropdown;
    public static int selectedSimulationMode = 0;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(CanvasToReturnButton);
        PopulateDropdown();

        if (startScreen != null && startButton != null && simulationModeDropdown != null)
        {
            startScreen.SetActive(true);
            startButton.onClick.AddListener(StartSimulation);
            returnButton.onClick.AddListener(ReturnToMainMenu);
        }
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Main");
    }
    void PopulateDropdown()
    {
        simulationModeDropdown.value = 0;
    }

    void StartSimulation()
    {
        selectedSimulationMode = simulationModeDropdown.value;
        startScreen.SetActive(false);
        ApplySimulationMode();
    }

    void ApplySimulationMode()
{
    string sceneToLoad = "";
    
    switch (selectedSimulationMode)
    {
        case 0:
            sceneToLoad = "Arkonska-Niemierzynska";
            break;
        case 1:
            sceneToLoad = "Oszacowane rzeczywisty";
            break;
        case 2:
            sceneToLoad = "Dokument Sczanieckiej rzeczywisty";
            break;
        case 3:
            sceneToLoad = "Oszacowane Logika rozmyta";
            break;
        case 4:
            sceneToLoad = "Dokument Sczanieckiej Logika rozmyta";
            break;
        default:
            Debug.LogWarning("Nieznany tryb symulacji");
            return;
    }

    SceneManager.LoadScene(sceneToLoad);
}

}

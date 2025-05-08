using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenManager : MonoBehaviour
{
    public static StartScreenManager Instance { get; private set; }

    //odpowiada za obs³ugê przycisku do wyboru wariantu symulacji
    public Button startButton;
    public TMP_Dropdown simulationModeDropdown;
    public static int selectedSimulationMode = 0;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        PopulateDropdown();
        startButton.onClick.AddListener(StartSimulation);
    }
    
    void PopulateDropdown()
    {
        simulationModeDropdown.value = 0;
    }

    void StartSimulation()
    {
        selectedSimulationMode = simulationModeDropdown.value;
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
            case 5:
                sceneToLoad = "Wiêksze Oszacowane Logika rozmyta";
                break;
            case 6:
                sceneToLoad = "Wiêksze Oszacowane rzeczywisty";
                break;
            default:
                Debug.LogWarning("Nieznany tryb symulacji");
                return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}

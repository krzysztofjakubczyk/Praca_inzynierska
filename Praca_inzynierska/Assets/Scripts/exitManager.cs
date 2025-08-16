using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class exitManager : MonoBehaviour
{
    public static exitManager Instance { get; private set; }
    public Canvas PauseCanvas;
    public bool isGamePaused;
    public Button ExitButton;
    public Button returnButton;
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            Destroy(PauseCanvas.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(PauseCanvas.gameObject);
            DontDestroyOnLoad(this.gameObject);
            PauseCanvas.gameObject.SetActive(false);
        }

        returnButton.onClick.AddListener(ReturnToMainMenu);
        ExitButton.onClick.AddListener(ExitSimulation);
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Main");
        PauseCanvas.gameObject.SetActive(false);
    }
    private void ExitSimulation()
    {
        Application.Quit();
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!isGamePaused)
            {
                PauseCanvas.gameObject.SetActive(true);
                isGamePaused = true;
            }
            else
            {
                isGamePaused = false;
                PauseCanvas.gameObject.SetActive(false);
            }
            var timeManager = GameObject.FindFirstObjectByType<TimeManager>();
            if (timeManager != null) timeManager.TogglePause();
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class exitManager : MonoBehaviour
{
    public static exitManager Instance { get; private set; }
    public Canvas CanvasToReturnButton;
    public Button ExitButton;
    public Button returnButton;
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            Destroy(CanvasToReturnButton.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(CanvasToReturnButton);
            DontDestroyOnLoad(this);
        }

        returnButton.onClick.AddListener(ReturnToMainMenu);
        ExitButton.onClick.AddListener(ExitSimulation);
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Main");
    }
    private void ExitSimulation()
    {
        Application.Quit();
    }

}

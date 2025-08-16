using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class scenarioManager : MonoBehaviour
{
    public TMP_Text scenarioDynamicText;

    private void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        scenarioDynamicText.text = currentSceneName;
    }
}

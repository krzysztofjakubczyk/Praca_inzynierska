using Cinemachine;
using TMPro;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown; // Dropdown do wyboru kamery
    public CinemachineVirtualCamera[] cameras;        // Tablica kamer, kt�re b�d� prze��czane
    public CinemachineFreeLook freeLookCamera; // FreeLook Camera dla dynamicznej kontroli

    void Start()
    {
        // Wy��czenie wszystkich kamer na starcie, poza pierwsz�
        SetActiveCamera(0);

        // Dodajemy funkcj� zmieniaj�c� kamer� na podstawie wybranej opcji w Dropdown
        cameraDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(cameraDropdown); });
    }

    // Funkcja wywo�ywana przy zmianie opcji w Dropdown
    public void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        int selectedIndex = dropdown.value; // Pobranie wybranego indeksu
        SetActiveCamera(selectedIndex);     // Ustawienie aktywnej kamery na podstawie indeksu
    }

    // Funkcja aktywuj�ca wybran� kamer� i dezaktywuj�ca pozosta�e
    private void SetActiveCamera(int index)
    {
        // Wy��cz wszystkie zwyk�e kamery
        foreach (CinemachineVirtualCamera cam in cameras)
        {
            cam.gameObject.SetActive(false);
        }

        // Wy��cz FreeLook kamer�
        if (freeLookCamera != null)
        {
            freeLookCamera.gameObject.SetActive(false);
        }

        // Je�li index to ostatnia pozycja, aktywuj FreeLook Camera
        if (index == cameras.Length)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.gameObject.SetActive(true);
            }
        }
        else
        {
            // Aktywuj jedn� z normalnych kamer
            if (index >= 0 && index < cameras.Length)
            {
                cameras[index].gameObject.SetActive(true);
            }
        }
    }
}


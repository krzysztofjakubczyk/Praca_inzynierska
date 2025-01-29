using Cinemachine;
using TMPro;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown; // Dropdown do wyboru kamery
    public CinemachineVirtualCamera[] cameras;        // Tablica kamer, które bêd¹ prze³¹czane
    public CinemachineFreeLook freeLookCamera; // FreeLook Camera dla dynamicznej kontroli

    void Start()
    {
        // Wy³¹czenie wszystkich kamer na starcie, poza pierwsz¹
        SetActiveCamera(0);

        // Dodajemy funkcjê zmieniaj¹c¹ kamerê na podstawie wybranej opcji w Dropdown
        cameraDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(cameraDropdown); });
    }

    // Funkcja wywo³ywana przy zmianie opcji w Dropdown
    public void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        int selectedIndex = dropdown.value; // Pobranie wybranego indeksu
        SetActiveCamera(selectedIndex);     // Ustawienie aktywnej kamery na podstawie indeksu
    }

    // Funkcja aktywuj¹ca wybran¹ kamerê i dezaktywuj¹ca pozosta³e
    private void SetActiveCamera(int index)
    {
        // Wy³¹cz wszystkie zwyk³e kamery
        foreach (CinemachineVirtualCamera cam in cameras)
        {
            cam.gameObject.SetActive(false);
        }

        // Wy³¹cz FreeLook kamerê
        if (freeLookCamera != null)
        {
            freeLookCamera.gameObject.SetActive(false);
        }

        // Jeœli index to ostatnia pozycja, aktywuj FreeLook Camera
        if (index == cameras.Length)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.gameObject.SetActive(true);
            }
        }
        else
        {
            // Aktywuj jedn¹ z normalnych kamer
            if (index >= 0 && index < cameras.Length)
            {
                cameras[index].gameObject.SetActive(true);
            }
        }
    }
}


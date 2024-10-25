using Cinemachine;
using TMPro;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown; // Dropdown do wyboru kamery
    public CinemachineVirtualCamera[] cameras;        // Tablica kamer, kt�re b�d� prze��czane

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
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == index);
        }
    }
}

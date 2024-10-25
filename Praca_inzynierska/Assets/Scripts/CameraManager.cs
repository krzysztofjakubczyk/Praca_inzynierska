using Cinemachine;
using TMPro;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown; // Dropdown do wyboru kamery
    public CinemachineVirtualCamera[] cameras;        // Tablica kamer, które bêd¹ prze³¹czane

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
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == index);
        }
    }
}

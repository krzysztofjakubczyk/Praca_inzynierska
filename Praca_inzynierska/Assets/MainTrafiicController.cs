using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficController : MonoBehaviour
{
    [SerializeField] private List<LocalTrafficController> ControllerList;
    [SerializeField] private Dictionary<LocalTrafficController, int> CarCountOnInlet = new Dictionary<LocalTrafficController, int>();
    [SerializeField] private Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>> ColorCycle = new Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>>();
    public bool wantWarning;

    private int currentActiveControllerIndex = 0; // Indeks aktywnego kontrolera

    private void Start()
    {
        // Pocz�tkowe ustawienie dla ka�dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            ColorCycle.Add(controller, controller.GetTrafficLightCycle());
        }

        StartCoroutine(GetCarCountOnEntrance());
        StartCoroutine(SetLightCycle());
    }

    // Korutyna do zbierania liczby samochod�w na wlocie
    private IEnumerator GetCarCountOnEntrance()
    {
        while (true)
        {
            Dictionary<LocalTrafficController, int> updatedCarCounts = new Dictionary<LocalTrafficController, int>();

            foreach (var kvp in CarCountOnInlet)
            {
                LocalTrafficController trafficController = kvp.Key;
                int carCount = trafficController.carCountOnEntrance;
                updatedCarCounts[trafficController] = carCount;
            }

            foreach (var kvp in updatedCarCounts)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f);  // Od�wie�enie liczby samochod�w co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli �wiate� w kontrolerach lokalnych
    private IEnumerator SetLightCycle()
    {
        while (true)
        {
            // Resetujemy wszystkie kontrolery na czerwone (wszystkie �wiat�a czerwone)
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                var cycle = new Dictionary<TrafficLightColor, int>
                {
                    { TrafficLightColor.Red, 10 },    // Czerwone �wiat�o
                    { TrafficLightColor.Yellow, 0 },  // ��te �wiat�o wy��czone
                    { TrafficLightColor.Green, 0 }    // Zielone �wiat�o wy��czone
                };
                controller.SetTrafficLightCycle(cycle); // Wymuszenie czerwonego �wiat�a
            }

            // Wybieramy jeden kontroler do ustawienia na zielone
            var activeController = ControllerList[currentActiveControllerIndex];

            // Je�li to pierwszy kontroler, ustawiamy go na zielone, pozosta�e kontrolery maj� czerwone
            var greenCycle = new Dictionary<TrafficLightColor, int>
            {
                { TrafficLightColor.Red, 0 },     // Czerwone �wiat�o wy��czone
                { TrafficLightColor.Yellow, 3 },  // Kr�tkie ��te �wiat�o
                { TrafficLightColor.Green, 7 }    // D�u�sze zielone �wiat�o
            };

            activeController.SetTrafficLightCycle(greenCycle); // Zielone �wiat�o tylko dla aktywnego kontrolera

            if (wantWarning)
            {
                Debug.Log("Active controller with green light: " + activeController.name);
            }

            // Ustawiamy pozosta�e kontrolery na czerwone
            for (int i = 0; i < ControllerList.Count; i++)
            {
                if (i != currentActiveControllerIndex) // Dla wszystkich poza aktywnym kontrolerem
                {
                    var controller = ControllerList[i];
                    var cycleForOtherControllers = new Dictionary<TrafficLightColor, int>
                    {
                        { TrafficLightColor.Red, 10 },    // Czerwone �wiat�o
                        { TrafficLightColor.Yellow, 0 },  // ��te wy��czone
                        { TrafficLightColor.Green, 0 }    // Zielone wy��czone
                    };
                    controller.SetTrafficLightCycle(cycleForOtherControllers); // Ustawienie dla innych kontroler�w
                }
            }

            // Przejd� do nast�pnego kontrolera w kolejce
            currentActiveControllerIndex++;
            if (currentActiveControllerIndex >= ControllerList.Count)
            {
                currentActiveControllerIndex = 0; // Wracamy do pierwszego w kolejce
            }

            // Odczekaj pe�en cykl �wiate�, zanim zmienisz aktywny kontroler
            yield return new WaitForSeconds(10f);  // Mo�esz dostosowa� ten czas, aby odpowiada� Twoim potrzebom
        }
    }
}

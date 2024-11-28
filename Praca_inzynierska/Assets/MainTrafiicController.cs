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
        // Pocz¹tkowe ustawienie dla ka¿dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            ColorCycle.Add(controller, controller.GetTrafficLightCycle());
        }

        StartCoroutine(GetCarCountOnEntrance());
        StartCoroutine(SetLightCycle());
    }

    // Korutyna do zbierania liczby samochodów na wlocie
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

            yield return new WaitForSeconds(5f);  // Odœwie¿enie liczby samochodów co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli œwiate³ w kontrolerach lokalnych
    private IEnumerator SetLightCycle()
    {
        while (true)
        {
            // Resetujemy wszystkie kontrolery na czerwone (wszystkie œwiat³a czerwone)
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                var cycle = new Dictionary<TrafficLightColor, int>
                {
                    { TrafficLightColor.Red, 10 },    // Czerwone œwiat³o
                    { TrafficLightColor.Yellow, 0 },  // ¯ó³te œwiat³o wy³¹czone
                    { TrafficLightColor.Green, 0 }    // Zielone œwiat³o wy³¹czone
                };
                controller.SetTrafficLightCycle(cycle); // Wymuszenie czerwonego œwiat³a
            }

            // Wybieramy jeden kontroler do ustawienia na zielone
            var activeController = ControllerList[currentActiveControllerIndex];

            // Jeœli to pierwszy kontroler, ustawiamy go na zielone, pozosta³e kontrolery maj¹ czerwone
            var greenCycle = new Dictionary<TrafficLightColor, int>
            {
                { TrafficLightColor.Red, 0 },     // Czerwone œwiat³o wy³¹czone
                { TrafficLightColor.Yellow, 3 },  // Krótkie ¿ó³te œwiat³o
                { TrafficLightColor.Green, 7 }    // D³u¿sze zielone œwiat³o
            };

            activeController.SetTrafficLightCycle(greenCycle); // Zielone œwiat³o tylko dla aktywnego kontrolera

            if (wantWarning)
            {
                Debug.Log("Active controller with green light: " + activeController.name);
            }

            // Ustawiamy pozosta³e kontrolery na czerwone
            for (int i = 0; i < ControllerList.Count; i++)
            {
                if (i != currentActiveControllerIndex) // Dla wszystkich poza aktywnym kontrolerem
                {
                    var controller = ControllerList[i];
                    var cycleForOtherControllers = new Dictionary<TrafficLightColor, int>
                    {
                        { TrafficLightColor.Red, 10 },    // Czerwone œwiat³o
                        { TrafficLightColor.Yellow, 0 },  // ¯ó³te wy³¹czone
                        { TrafficLightColor.Green, 0 }    // Zielone wy³¹czone
                    };
                    controller.SetTrafficLightCycle(cycleForOtherControllers); // Ustawienie dla innych kontrolerów
                }
            }

            // PrzejdŸ do nastêpnego kontrolera w kolejce
            currentActiveControllerIndex++;
            if (currentActiveControllerIndex >= ControllerList.Count)
            {
                currentActiveControllerIndex = 0; // Wracamy do pierwszego w kolejce
            }

            // Odczekaj pe³en cykl œwiate³, zanim zmienisz aktywny kontroler
            yield return new WaitForSeconds(10f);  // Mo¿esz dostosowaæ ten czas, aby odpowiada³ Twoim potrzebom
        }
    }
}

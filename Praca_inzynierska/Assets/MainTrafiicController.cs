using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficController : MonoBehaviour
{
    [SerializeField] private List<LocalTrafficController> ControllerList;
    [SerializeField] private Dictionary<LocalTrafficController, int> CarCountOnInlet = new Dictionary<LocalTrafficController, int>();
    [SerializeField] private Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>> ColorCycle = new Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>>();

    private void Start()
    {
        // Pocz¹tkowe ustawienie dla ka¿dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            ColorCycle.Add(controller, controller.GetTrafficLightCycle()); // Zmieniamy cykle œwiate³ w kontrolerach
        }

        // Uruchamiamy korutyny
        StartCoroutine(GetCarCountOnEntrance());
        StartCoroutine(GetLightCycle());
        StartCoroutine(SetLightCycle());
    }

    // Korutyna do zbierania liczby samochodów na wlocie
    private IEnumerator GetCarCountOnEntrance()
    {
        while (true)
        {
            // Tymczasowy s³ownik do przechowywania zmian
            Dictionary<LocalTrafficController, int> updatedCarCounts = new Dictionary<LocalTrafficController, int>();

            // Iterujemy przez istniej¹ce elementy
            foreach (var kvp in CarCountOnInlet)
            {
                LocalTrafficController trafficController = kvp.Key;
                int carCount = trafficController.carCountOnEntrance;
                updatedCarCounts[trafficController] = carCount;  // Dodajemy do tymczasowego s³ownika
            }

            // Po zakoñczeniu iteracji, aktualizujemy oryginalny s³ownik
            foreach (var kvp in updatedCarCounts)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f);
        }
    }


    // Korutyna do zbierania aktualnego cyklu œwiate³ z kontrolerów lokalnych
    private IEnumerator GetLightCycle()
    {
        while (true)
        {
            // Zbieramy zaktualizowane cykle w tymczasowym s³owniku
            Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>> updatedColorCycles = new Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>>();

            // Iteracja po ka¿dym kontrolerze i pobranie zaktualizowanego cyklu œwiate³
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                updatedColorCycles[controller] = controller.GetTrafficLightCycle(); // Pobieramy cykl œwiate³
            }

            // Teraz, po zakoñczeniu iteracji, przypisujemy zaktualizowane wartoœci
            foreach (var kvp in updatedColorCycles)
            {
                ColorCycle[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f); // Czekaj 5 sekund przed kolejn¹ iteracj¹
        }
    }


    // Korutyna do ustawiania cykli œwiate³ w kontrolerach lokalnych
    private IEnumerator SetLightCycle()
    {
        while (true)
        {
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                var currentCycle = GetTrafficLightCycleForController(controller); // Pobieramy nowy cykl dla kontrolera
                controller.SetTrafficLightCycle(currentCycle); // Ustawiamy cykl œwiate³ w kontrolerze lokalnym
            }
            yield return new WaitForSeconds(30f); // Ustawiaj cykl co 3 sekundy
        }
    }

    // Metoda do obliczania cyklu œwiate³ na podstawie liczby samochodów na wlocie
    private Dictionary<TrafficLightColor, int> GetTrafficLightCycleForController(LocalTrafficController controller)
    {
        Dictionary<TrafficLightColor, int> cycle = new Dictionary<TrafficLightColor, int>();

        int redTime = 10;
        int yellowTime = 5;
        int greenTime = 5;

        // Jeœli liczba samochodów na wlocie jest wiêksza, zmieniamy czas na zielonym
        if (CarCountOnInlet[controller] > 10)
        {
            greenTime = 10; // D³u¿sze zielone œwiat³o
            redTime = 15;   // D³u¿sze czerwone œwiat³o
        }

        // Ustawiamy czasy cyklu dla ka¿dego koloru œwiat³a
        cycle[TrafficLightColor.Red] = redTime;
        cycle[TrafficLightColor.Yellow] = yellowTime;
        cycle[TrafficLightColor.Green] = greenTime;

        return cycle; // Zwracamy cykl
    }
}

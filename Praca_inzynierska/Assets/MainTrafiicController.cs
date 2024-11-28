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
        // Pocz�tkowe ustawienie dla ka�dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            ColorCycle.Add(controller, controller.GetTrafficLightCycle()); // Zmieniamy cykle �wiate� w kontrolerach
        }

        // Uruchamiamy korutyny
        StartCoroutine(GetCarCountOnEntrance());
        StartCoroutine(GetLightCycle());
        StartCoroutine(SetLightCycle());
    }

    // Korutyna do zbierania liczby samochod�w na wlocie
    private IEnumerator GetCarCountOnEntrance()
    {
        while (true)
        {
            // Tymczasowy s�ownik do przechowywania zmian
            Dictionary<LocalTrafficController, int> updatedCarCounts = new Dictionary<LocalTrafficController, int>();

            // Iterujemy przez istniej�ce elementy
            foreach (var kvp in CarCountOnInlet)
            {
                LocalTrafficController trafficController = kvp.Key;
                int carCount = trafficController.carCountOnEntrance;
                updatedCarCounts[trafficController] = carCount;  // Dodajemy do tymczasowego s�ownika
            }

            // Po zako�czeniu iteracji, aktualizujemy oryginalny s�ownik
            foreach (var kvp in updatedCarCounts)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f);
        }
    }


    // Korutyna do zbierania aktualnego cyklu �wiate� z kontroler�w lokalnych
    private IEnumerator GetLightCycle()
    {
        while (true)
        {
            // Zbieramy zaktualizowane cykle w tymczasowym s�owniku
            Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>> updatedColorCycles = new Dictionary<LocalTrafficController, Dictionary<TrafficLightColor, int>>();

            // Iteracja po ka�dym kontrolerze i pobranie zaktualizowanego cyklu �wiate�
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                updatedColorCycles[controller] = controller.GetTrafficLightCycle(); // Pobieramy cykl �wiate�
            }

            // Teraz, po zako�czeniu iteracji, przypisujemy zaktualizowane warto�ci
            foreach (var kvp in updatedColorCycles)
            {
                ColorCycle[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f); // Czekaj 5 sekund przed kolejn� iteracj�
        }
    }


    // Korutyna do ustawiania cykli �wiate� w kontrolerach lokalnych
    private IEnumerator SetLightCycle()
    {
        while (true)
        {
            foreach (var kvp in ColorCycle)
            {
                LocalTrafficController controller = kvp.Key;
                var currentCycle = GetTrafficLightCycleForController(controller); // Pobieramy nowy cykl dla kontrolera
                controller.SetTrafficLightCycle(currentCycle); // Ustawiamy cykl �wiate� w kontrolerze lokalnym
            }
            yield return new WaitForSeconds(30f); // Ustawiaj cykl co 3 sekundy
        }
    }

    // Metoda do obliczania cyklu �wiate� na podstawie liczby samochod�w na wlocie
    private Dictionary<TrafficLightColor, int> GetTrafficLightCycleForController(LocalTrafficController controller)
    {
        Dictionary<TrafficLightColor, int> cycle = new Dictionary<TrafficLightColor, int>();

        int redTime = 10;
        int yellowTime = 5;
        int greenTime = 5;

        // Je�li liczba samochod�w na wlocie jest wi�ksza, zmieniamy czas na zielonym
        if (CarCountOnInlet[controller] > 10)
        {
            greenTime = 10; // D�u�sze zielone �wiat�o
            redTime = 15;   // D�u�sze czerwone �wiat�o
        }

        // Ustawiamy czasy cyklu dla ka�dego koloru �wiat�a
        cycle[TrafficLightColor.Red] = redTime;
        cycle[TrafficLightColor.Yellow] = yellowTime;
        cycle[TrafficLightColor.Green] = greenTime;

        return cycle; // Zwracamy cykl
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] CarPrefab;
    public float spawnInterval = 2.0f; // Czas spawnowania pojazdów (w sekundach)
    public int maxVehicles = 10; // Maksymalna liczba pojazdów na drodze

    private int currentVehicleCount = 0; // Liczba aktualnie spawnowanych pojazdów

    private void Start()
    {
        StartCoroutine(SpawnVehicles()); // Rozpocznij spawnowanie pojazdów
    }

    private IEnumerator SpawnVehicles()
    {
        while (true)
        {
            if (currentVehicleCount < maxVehicles)
            {
                SpawnVehicle(); // Spawnowanie pojazdu
            }
            yield return new WaitForSeconds(spawnInterval); // Czekaj na kolejny cykl spawnowania
        }
    }

    private void SpawnVehicle()
    {
        // Wybierz losowy prefab z tablicy
        GameObject vehiclePrefab = CarPrefab[Random.Range(0, CarPrefab.Length)];

        // Tworzenie pojazdu w punkcie spawnowania
        GameObject newVehicle = Instantiate(vehiclePrefab, transform.position, transform.rotation);

        // Zwiêksz licznik aktualnych pojazdów
        currentVehicleCount++;

        //// Przyk³adowa logika do zarz¹dzania ¿yciem pojazdu (np. po osi¹gniêciu celu, zmniejsz licznik)
        //newVehicle.GetComponent<Auto>().OnVehicleDestroyed += DecreaseVehicleCount; // Subskrybuj zdarzenie z klasy Auto
    }

    private void DecreaseVehicleCount()
    {
        currentVehicleCount--; // Zmniejsz licznik, gdy pojazd zostanie zniszczony lub osi¹gnie cel
    }
}

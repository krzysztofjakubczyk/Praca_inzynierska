using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] CarPrefab;
    [SerializeField] public float spawnInterval = 0.5f; // Domyœlny czas spawnowania pojazdów
    [SerializeField] public int maxVehicles; // Maksymalna liczba pojazdów na drodze
    [SerializeField] private int currentVehicleCount = 0; // Liczba aktualnie spawnowanych pojazdów

    private const float realToSimRatio = 1f; // 15 sekund symulacji = 1 sekunda rzeczywista

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

            // Czas oczekiwania zale¿ny od skali symulacji
            yield return new WaitForSeconds(spawnInterval / realToSimRatio);
        }
    }

    public void SetMaxVehicles(int max)
    {
        maxVehicles = max;
    }

    private void SpawnVehicle()
    {
        // Wybierz losowy prefab z tablicy
        GameObject vehiclePrefab = CarPrefab[Random.Range(0, CarPrefab.Length)];

        // Tworzenie pojazdu w punkcie spawnowania
        Instantiate(vehiclePrefab, transform.position, transform.rotation);

        // Zwiêksz licznik aktualnych pojazdów
        currentVehicleCount++;
    }

    public void ResetSpawner()
    {
        currentVehicleCount = 0;
    }
}

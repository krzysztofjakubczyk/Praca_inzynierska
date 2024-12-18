using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] CarPrefab;
    [SerializeField] float spawnInterval = 2.0f; // Czas spawnowania pojazd�w (w sekundach)
    [SerializeField] public int maxVehicles; // Maksymalna liczba pojazd�w na drodze
    [SerializeField] int currentVehicleCount = 0; // Liczba aktualnie spawnowanych pojazd�w
    private void Start()
    {
        StartCoroutine(SpawnVehicles()); // Rozpocznij spawnowanie pojazd�w
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
        Instantiate(vehiclePrefab, transform.position, transform.rotation);

        // Zwi�ksz licznik aktualnych pojazd�w
        currentVehicleCount++;  
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            maxVehicles++;
            SpawnVehicle();
        }
    }
    public void ResetSpawner()
    {
        currentVehicleCount = 0;
    }
}

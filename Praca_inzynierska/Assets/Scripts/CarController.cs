using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    public float detectionDistance = 10f;
    public float stopDistance = 8f;
    private float detectionInterval = 0.5f;

    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do kt�rego pojazd ma si� uda�
        Transform nearestParkingSpot = FindNearestStopLine(agent.transform);
        agent.SetDestination(nearestParkingSpot.position);

        StartCoroutine(DetectCarsCoroutine());
    }
    Transform FindNearestStopLine(Transform carTransform)
    {
        GameObject[] parkingSpots = GameObject.FindGameObjectsWithTag("StopPoint"); 

        Transform nearestSpot = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject spot in parkingSpots)
        {
            float distance = Vector3.Distance(carTransform.position, spot.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSpot = spot.transform;
            }
        }

        return nearestSpot;
    }

    private IEnumerator DetectCarsCoroutine()
    {
        while (true)
        {
            DetectCars();
            yield return new WaitForSeconds(detectionInterval); // Czekaj przez okre�lony czas
        }
    }

    private void DetectCars()
    {
        RaycastHit hit;
        // Ustawienie wektora kierunku do przodu
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;
        Debug.DrawRay(transform.position, forwardDirection, Color.red); // Rysuje czerwony promie�

        // Sprawdzenie, czy raycast trafia w inne auto
        if (Physics.Raycast(transform.position, forwardDirection, out hit, detectionDistance))
        {
            if (hit.collider.CompareTag("Car")) // Zak�adamy, �e inne samochody maj� tag "Car"
            {
                // Je�li wykryto inne auto, zatrzymaj si�, je�li jest za blisko
                if (hit.distance < stopDistance)
                {
                    print("Zatrzymano autko");
                    agent.isStopped = true; // Zatrzymaj pojazd
                }
                else
                {
                    agent.isStopped = false; // Wzn�w jazd�
                }
            }
        }
        else
        {
            // Nie wykryto przeszk�d, kontynuuj jazd�
            agent.isStopped = false;
        }
    }
}
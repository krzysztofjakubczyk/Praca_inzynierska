using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] float detectionDistance = 10f;
    [SerializeField] float stopDistance = 8f;
    [SerializeField] float detectionInterval = 0.5f;
    public Waypoint currentWaypoint;  // Aktualny waypoint, na kt�ry pojazd zmierza

    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do kt�rego pojazd ma si� uda�
        Transform nearestParkingSpot = FindLineChooser(agent.transform);
        SetAgentDestination(nearestParkingSpot.position);

        StartCoroutine(DetectCarsCoroutine());
    }
    private void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Przeskakuj do kolejnego waypointa, gdy dotrzesz do bie��cego
            currentWaypoint = currentWaypoint.NextWaypoint;
            if (currentWaypoint != null)
            {
                agent.SetDestination(currentWaypoint.transform.position);
            }
        }
    }
    private Transform FindLineChooser(Transform carTransform)
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
    public void SetAgentDestination(Vector3 destinationTransform)
    {
         agent.SetDestination(destinationTransform);
    }

}
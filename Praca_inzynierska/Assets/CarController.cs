using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    public Transform target;  // Transform celu, do którego pojazd ma siê udaæ
    private NavMeshAgent agent;

    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do którego pojazd ma siê udaæ
        agent.SetDestination(target.position);
    }

    void Update()
    {
        // Jeœli pojazd ma nowy cel lub potrzebujesz aktualizowaæ go dynamicznie
        if (Vector3.Distance(agent.destination, target.position) > 0.1f)
        {
            agent.SetDestination(target.position);
        }
    }
}

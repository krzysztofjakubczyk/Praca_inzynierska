using System.Collections;
using UnityEngine;

public enum TrafficLightColor
{
    Red,
    Yellow,
    Green
}

public class LocalTrafficController : MonoBehaviour
{

    [SerializeField] private Sensor sensor;
    [SerializeField] private int carCountOnEntrance;
    private string carCountFusedLogic;
    public TrafficLightColor currentLight;

    private void Start()
    {
        StartCoroutine(getCarCount());
        currentLight = TrafficLightColor.Red;
    }

    private IEnumerator getCarCount()
    {
        while (true)
        {
            carCountOnEntrance = sensor.CarCount;
            yield return new WaitForSeconds(5.0f);
        }
    }



}

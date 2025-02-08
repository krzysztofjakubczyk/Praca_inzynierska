    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Timers : MonoBehaviour
    {
        [SerializeField]private float entryTime;
        [SerializeField]private float waitningTime;
        public float timeSpent;
        private bool isTracking = false;
        public string cameFrom;
        public void StartTimer()
        {
       
            entryTime = Time.time; // Zapisz czas wej�cia
            isTracking = true;
        }

        public void StopTimer(GameObject vehicle)
        {
            if (!isTracking) return;

            float exitTime = Time.time;
            timeSpent = exitTime - entryTime;

    //Debug.Log($"Pojazd {vehicle.name} sp�dzi� {timeSpent:F2} sekund na skrzy�owaniu.");

            isTracking = false; // Zatrzymaj �ledzenie
        }
        public void SetCameFrom(string toSet)
        {
            cameFrom = toSet;
        }
    }

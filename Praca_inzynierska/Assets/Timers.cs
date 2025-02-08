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
       
            entryTime = Time.time; // Zapisz czas wejœcia
            isTracking = true;
        }

        public void StopTimer(GameObject vehicle)
        {
            if (!isTracking) return;

            float exitTime = Time.time;
            timeSpent = exitTime - entryTime;

    //Debug.Log($"Pojazd {vehicle.name} spêdzi³ {timeSpent:F2} sekund na skrzy¿owaniu.");

            isTracking = false; // Zatrzymaj œledzenie
        }
        public void SetCameFrom(string toSet)
        {
            cameFrom = toSet;
        }
    }

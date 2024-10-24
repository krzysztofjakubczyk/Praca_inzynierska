using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera cameraOnTheRoof;   // Kamera z ty³u
    public CinemachineVirtualCamera cameraBus;    // Kamera z góry
    public CinemachineVirtualCamera cameraCar; // Kamera wewnêtrzna
    public CinemachineVirtualCamera cameraPolice; // Kamera wewnêtrzna
    public CinemachineVirtualCamera cameraTruck; // Kamera wewnêtrzna


    private CinemachineVirtualCamera activeCamera;

    void Start()
    {
        // Ustaw domyœln¹ kamerê (np. widok z ty³u)
        SwitchToCamera(cameraOnTheRoof);
    }

    public void SwitchToCamera(CinemachineVirtualCamera newCamera)
    {
        // Wy³¹cz aktualnie aktywn¹ kamerê
        if (activeCamera != null)
        {
            activeCamera.Priority = 0;  // Ustaw nisk¹ priorytet, aby j¹ wy³¹czyæ
        }

        // Ustaw now¹ kamerê jako aktywn¹
        newCamera.Priority = 10;  // Wy¿szy priorytet w³¹cza tê kamerê
        activeCamera = newCamera;
    }

    // Funkcje wywo³ywane przez przyciski
    public void SwitchToRoofCamera()
    {
        SwitchToCamera(cameraOnTheRoof);
    }

    public void SwitchToBusCamera()
    {
        SwitchToCamera(cameraBus);
    }

    public void SwitchToCarCamera()
    {
        SwitchToCamera(cameraCar);
    }
    public void SwitchToPoliceCamera()
    {
        SwitchToCamera(cameraPolice);
    }
    public void SwitchToTruckCamera()
    {
        SwitchToCamera(cameraTruck);
    }
}
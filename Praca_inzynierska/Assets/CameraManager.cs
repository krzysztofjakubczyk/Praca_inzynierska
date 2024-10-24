using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera cameraOnTheRoof;   // Kamera z ty�u
    public CinemachineVirtualCamera cameraBus;    // Kamera z g�ry
    public CinemachineVirtualCamera cameraCar; // Kamera wewn�trzna
    public CinemachineVirtualCamera cameraPolice; // Kamera wewn�trzna
    public CinemachineVirtualCamera cameraTruck; // Kamera wewn�trzna


    private CinemachineVirtualCamera activeCamera;

    void Start()
    {
        // Ustaw domy�ln� kamer� (np. widok z ty�u)
        SwitchToCamera(cameraOnTheRoof);
    }

    public void SwitchToCamera(CinemachineVirtualCamera newCamera)
    {
        // Wy��cz aktualnie aktywn� kamer�
        if (activeCamera != null)
        {
            activeCamera.Priority = 0;  // Ustaw nisk� priorytet, aby j� wy��czy�
        }

        // Ustaw now� kamer� jako aktywn�
        newCamera.Priority = 10;  // Wy�szy priorytet w��cza t� kamer�
        activeCamera = newCamera;
    }

    // Funkcje wywo�ywane przez przyciski
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
using UnityEngine;
using Cinemachine;

public class FreeLookCameraController : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public float moveSpeed = 10f;
    public float sprintMultiplier = 2f;
    public float lookSensitivity = 2f;

    private Vector3 moveDirection;
    private bool isRightMousePressed = false;

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        float moveSpeedAdjusted = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        float horizontal = Input.GetAxis("Horizontal"); // A, D lub strza�ki lewo/prawo
        float vertical = Input.GetAxis("Vertical");     // W, S lub strza�ki g�ra/d�
        float upDown = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0); // E = w g�r�, Q = w d�

        moveDirection = new Vector3(horizontal, upDown, vertical).normalized;
        transform.position += transform.TransformDirection(moveDirection) * moveSpeedAdjusted * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButtonDown(1)) // Prawy przycisk myszy wci�ni�ty
        {
            isRightMousePressed = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(1)) // Prawy przycisk myszy zwolniony
        {
            isRightMousePressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isRightMousePressed)
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            Debug.Log($"Mouse X: {mouseX}, Mouse Y: {mouseY}"); // Sprawdzenie, czy myszka dzia�a

            // Je�li masz Cinemachine FreeLook, obracamy kamer�
            if (freeLookCamera != null)
            {
                freeLookCamera.m_XAxis.Value += mouseX;
                freeLookCamera.m_YAxis.Value -= mouseY;
            }
            else
            {
                // Je�li Cinemachine nie dzia�a, obracamy obiekt (np. kamer�)
                transform.Rotate(Vector3.up * mouseX, Space.World);
                transform.Rotate(Vector3.left * mouseY, Space.Self);
            }
        }
    }
}

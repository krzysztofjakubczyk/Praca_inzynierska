using UnityEngine;
using Cinemachine;

public class FlyCamera : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float moveSpeed = 10f;
    public float sprintMultiplier = 2f;
    public float lookSensitivity = 2f;

    private bool isRightMousePressed = false;
    private Vector3 moveDirection;
    private Transform cameraTransform;

    void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("Brak przypisanej Cinemachine Virtual Camera! Przypisz j¹ w Inspectorze.");
            return;
        }

        cameraTransform = virtualCamera.transform;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        float moveX = Input.GetAxis("Horizontal"); // A, D
        float moveZ = Input.GetAxis("Vertical"); // W, S
        float moveY = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0); // Q (w dó³), E (w górê)

        moveDirection = new Vector3(moveX, moveY, moveZ).normalized;
        cameraTransform.position += cameraTransform.TransformDirection(moveDirection) * speed * Time.deltaTime;
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButtonDown(1)) // Prawy przycisk myszy wciœniêty
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

            cameraTransform.Rotate(Vector3.up * mouseX, Space.World);
            cameraTransform.Rotate(Vector3.left * mouseY, Space.Self);
        }
    }
}

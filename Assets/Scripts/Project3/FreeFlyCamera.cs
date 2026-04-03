using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float verticalSpeed = 5f;

    [Header("視角旋轉")]
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("縮放(FOV Zoom)")]
    public float normalFov = 60f;
    public float zoomFov = 30f;
    public float zoomSmooth = 10f;

    [Header("游標")]
    public bool lockCursorOnStart = true;

    private float yaw;
    private float pitch;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        cam.fieldOfView = normalFov;

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleCursor();
        HandleLook();
        HandleMove();
        HandleZoom();
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 改成左鍵重新鎖定，避免和右鍵縮放衝突
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleLook()
    {
        // 若游標沒鎖定就不轉鏡頭
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMove()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= sprintMultiplier;

        Vector3 move = Vector3.zero;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        move += transform.forward * v;
        move += transform.right * h;

        if (Input.GetKey(KeyCode.E))
            move += Vector3.up * (verticalSpeed / moveSpeed);

        if (Input.GetKey(KeyCode.Q))
            move += Vector3.down * (verticalSpeed / moveSpeed);

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * speed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        float targetFov = Input.GetMouseButton(1) ? zoomFov : normalFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, zoomSmooth * Time.deltaTime);
    }
}
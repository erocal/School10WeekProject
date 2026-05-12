using UnityEngine;

public class UAVCameraRelativeController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float verticalSpeed = 5f;

    private void Start()
    {
        // 若沒有手動指定，就自動抓 Main Camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("尚未指定 Camera Transform，請把 Main Camera 拖到 cameraTransform。");
            return;
        }

        MoveUAV();
    }

    private void MoveUAV()
    {
        // W/S：前後移動
        float forwardInput = Input.GetAxisRaw("Vertical");

        // A/D：改成上下移動
        float upDown = 0f;

        if (Input.GetKey(KeyCode.D))
        {
            upDown = 1f;   // D 上升
        }
        else if (Input.GetKey(KeyCode.A))
        {
            upDown = -1f;  // A 下降
        }

        // 取得攝影機前方
        Vector3 camForward = cameraTransform.forward;

        // 忽略攝影機上下俯仰，只保留水平面方向
        camForward.y = 0f;
        camForward.Normalize();

        // 計算移動方向
        Vector3 moveDirection =
            camForward * forwardInput * moveSpeed +
            Vector3.up * upDown * verticalSpeed;

        // 實際移動
        transform.position += moveDirection * Time.deltaTime;
    }
}
using TMPro;
using UnityEngine;
public class AimToMouse : MonoBehaviour
{
    [SerializeField] private Camera cam;           // drag Main Camera (or leave null)
    [SerializeField] private LayerMask groundMask; // set "Ground" layer in Inspector
    [SerializeField] private float stepDistance = 1.5f;
    [SerializeField] private float moveSpeed = 3f;        // 每秒移動速度
    [SerializeField] private float rotationSpeed = 8f;   // 旋轉平滑速度
    [SerializeField] private float stopDistance = 0.1f;   // 停止距離
    [SerializeField] private GameObject AimVFX;

    private float rayDistance = 100f;   // 射線長度
    private Vector3 targetPosition;
    private bool hasTarget = false;

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.yellow);

        // 點擊設定目標
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask))
            {
                targetPosition = hit.point;
                hasTarget = true;

                Vector3 toTarget = targetPosition - transform.position;
                toTarget.y = 0f;

                float dot = Vector3.Dot(transform.forward.normalized, toTarget.normalized);
                Debug.Log($"Hit point: {hit.point}");
                Debug.Log(dot > 0 ? "In Front" : "Behind");

                toTarget.Normalize();
                targetPosition = transform.position + (toTarget * stepDistance);

                Quaternion quaternion = Quaternion.identity;
                GameObject.Instantiate(AimVFX, hit.point, quaternion);

            }
        }

        // 持續移動 + 旋轉
        if (hasTarget)
        {
            MoveTowardsTarget();
            RotateTowardsTarget(targetPosition);
        }

    }

    /// <summary>
    /// 持續往目標走
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        // 到達目標就停止
        if (toTarget.magnitude < stopDistance)
        {
            hasTarget = false;
            return;
        }

        Vector3 direction = toTarget.normalized;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 使用 Slerp 平滑旋轉到目標方向（限制旋轉速度）
    /// </summary>
    private void RotateTowardsTarget(Vector3 target)
    {
        Vector3 lookDir = target - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);

        transform.rotation = SmoothRotate(
            transform.rotation,
            targetRotation,
            rotationSpeed
        );
    }

    /// <summary>
    /// 核心：用 Slerp 限制旋轉速度
    /// </summary>
    private Quaternion SmoothRotate(Quaternion current, Quaternion target, float speed)
    {
        return Quaternion.Slerp(
            current,
            target,
            speed * Time.deltaTime
        );
    }

}

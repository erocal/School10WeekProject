using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CCMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -18f;

    // 新增：旋轉速度與衝刺倍率
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    private CharacterController cc;
    private float verticalVel;

    private void Awake() => cc = GetComponent<CharacterController>();

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 修改：A / D 不再左右平移，而是左右旋轉
        transform.Rotate(0f, h * rotateSpeed * Time.deltaTime, 0f);

        // 修改：只使用 W / S 控制前進與後退
        Vector3 move = new Vector3(0f, 0f, v);

        move = Vector3.ClampMagnitude(move, 1f);
        move = transform.TransformDirection(move);

        // 新增：Left Shift 衝刺
        float currentMoveSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveSpeed *= sprintMultiplier;
        }

        if (cc.isGrounded && verticalVel < 0f)
            verticalVel = -2f;

        if (cc.isGrounded && Input.GetButtonDown("Jump"))
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVel += gravity * Time.deltaTime;

        // 修改：moveSpeed 改成 currentMoveSpeed
        Vector3 velocity = move * currentMoveSpeed + Vector3.up * verticalVel;

        cc.Move(velocity * Time.deltaTime);
    }
}
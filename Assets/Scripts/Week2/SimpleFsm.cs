using UnityEngine;
public class SimpleFsm : MonoBehaviour
{
    private enum PlayerState { Idle, Run, Jump }

    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpDuration = 0.2f;

    private PlayerState state = PlayerState.Idle;
    private float jumpTimer = 0f;

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (state != PlayerState.Jump)
        {
            // switch expression: compact state rules
            state = (state, input.sqrMagnitude) switch
            {
                (_, > 0.01f) => PlayerState.Run,
                (PlayerState.Run, <= 0.01f) => PlayerState.Idle,
                _ => state
            };
        }

        if (Input.GetButtonDown("Jump"))
            state = PlayerState.Jump;

        Debug.Log(state);
        UpdateMovement(input);
    }
    private void UpdateMovement(Vector2 input)
    {
        // Run
        if (state is PlayerState.Run)
        {
            Vector3 move = new Vector3(input.x, 0f, input.y).normalized;
            transform.Translate(move * runSpeed * Time.deltaTime, Space.World);
        }

        // Jump
        if (state is PlayerState.Jump)
        {
            jumpTimer += Time.deltaTime;

            if (jumpTimer <= jumpDuration)
            {
                transform.Translate(Vector3.up * jumpForce * Time.deltaTime, Space.World);
            }
            else
            {
                jumpTimer = 0f;

                state = input.sqrMagnitude > 0.01f
                    ? PlayerState.Run
                    : PlayerState.Idle;
            }
        }
    }
}

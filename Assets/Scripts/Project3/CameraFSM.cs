using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFSM : MonoBehaviour
{
    private enum PlayerState { Idle, Run, Jump }

    [Header("Move")]
    [SerializeField] private float runSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpDuration = 0.2f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private PlayerState state = PlayerState.Idle;
    private float jumpTimer = 0f;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (state != PlayerState.Jump)
        {
            state = (state, input.sqrMagnitude) switch
            {
                (_, > 0.01f) => PlayerState.Run,
                (PlayerState.Run, <= 0.01f) => PlayerState.Idle,
                _ => state
            };
        }

        if (Input.GetButtonDown("Jump") && state != PlayerState.Jump)
        {
            state = PlayerState.Jump;
            jumpTimer = 0f;
        }

        UpdateMovement(input);
        SyncCameraDirectionWithPlayer();
    }

    private void UpdateMovement(Vector2 input)
    {
        Vector3 move = GetCameraRelativeMove(input);

        // Run
        if (state is PlayerState.Run)
        {
            this.transform.eulerAngles = new Vector3(transform.eulerAngles.x, cameraTransform.eulerAngles.y, transform.eulerAngles.z);

            if (move.sqrMagnitude > 0.001f)
            {
                transform.Translate(move * runSpeed * Time.deltaTime, Space.World);
            }
        }

        // Jump
        if (state is PlayerState.Jump)
        {
            this.transform.eulerAngles = new Vector3(transform.eulerAngles.x, cameraTransform.eulerAngles.y, transform.eulerAngles.z);

            jumpTimer += Time.deltaTime;

            if (move.sqrMagnitude > 0.001f)
            {
                transform.Translate(move * runSpeed * Time.deltaTime, Space.World);
            }

            if (jumpTimer <= jumpDuration)
            {
                transform.Translate(Vector3.up * jumpForce * Time.deltaTime, Space.World);
            }
            else
            {
                jumpTimer = 0f;
                state = input.sqrMagnitude > 0.01f ? PlayerState.Run : PlayerState.Idle;
            }
        }
    }

    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y).normalized;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // 只保留水平面方向，避免相機往上/下看時角色也往上飄
        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * input.y + camRight * input.x;
        return move.normalized;
    }

    private void SyncCameraDirectionWithPlayer()
    {
        if (cameraTransform == null) return;

        Vector3 camEuler = cameraTransform.eulerAngles;
        camEuler.y = transform.eulerAngles.y;
        cameraTransform.eulerAngles = camEuler;
    }
}
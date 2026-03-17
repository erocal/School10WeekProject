using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MyTransform : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;   // units per second
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    [SerializeField] private float rotationSpeed = 60f; // degrees per second

    private float logTimer = 0f;
    private void Update()
    {
        Vector3 delta = moveDirection.normalized * moveSpeed * Time.deltaTime;
        transform.position += delta;

        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

        logTimer += Time.deltaTime;
        if (logTimer >= 1f)
        {
            Vector3 p = transform.position;
            Debug.Log($"Position: x={p.x:F2}, y={p.y:F2}, z={p.z:F2}");
            Debug.Log($"Rotation: {transform.eulerAngles}");
            logTimer = 0f;
        }
    }
}

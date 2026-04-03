using UnityEngine;
public class EnemySensor : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRadius = 3f;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.5f;

    private bool wasInside = false;
    private void Update()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        bool inside = dist <= detectionRadius;
        if (inside && !wasInside) Debug.Log($"{this.name}: Player entered");
        wasInside = inside;
        // TODO (Lab - optional): chase player when inside (MoveTowards)
        if (inside)
        {
            if (dist > stopDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    player.position,
                    moveSpeed * Time.deltaTime
                );
            }
        }
    }
    private void OnDrawGizmos() // only for scene view
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

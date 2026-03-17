using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int score = 0;
    [SerializeField] private float moveSpeed = 5.5f;

    private void Awake()
    {
        // Awake: initialize references & invariants
        Debug.Log($"[Awake] {playerName} speed={moveSpeed}");
    }

    private void Start()
    {
        score += 10;
        Debug.Log($"[Start] score={score}");
    }
}

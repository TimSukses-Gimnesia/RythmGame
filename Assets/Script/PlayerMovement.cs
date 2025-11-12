using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Lane Positions (0=Right, 1=Left, 2=Up, 3=Down)")]
    public List<Transform> movePosition;

    [Header("Dash Movement Settings")]
    [Tooltip("Kecepatan dash ke lane baru (higher = snappier/lebih cepat).")]
    public float dashSpeed = 25f;

    [Tooltip("How close before considered 'snapped' to target lane.")]
    public float snapThreshold = 0.01f;

    [Tooltip("If true, player stays exactly at lane center after dash.")]
    public bool hardSnapToLane = true;

    [Header("Game State & Health")]
    public float maxHealth = 100f;
    public float healthDecreaseRate = 2f;
    public string gameOverSceneName = "GameOverScene";
    private bool isGameOver = false;

    private Vector2 moveInput;
    private Vector3 targetPosition;
    private int currentLane = -1;
    private Vector3 centerPosition = Vector3.zero;
    private Vector3 dashVelocity;

    void Start()
    {
        // Reset gameplay variables
        HitJudgement.health = maxHealth;
        HitJudgement.score = 0;
        HitJudgement.combo = 0;
        isGameOver = false;
        Time.timeScale = 1f;

        targetPosition = centerPosition;
        transform.position = targetPosition;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        if (moveInput.x > 0)
        {
            MoveToLane(0); // Right
        }
        else if (moveInput.x < 0)
        {
            MoveToLane(1); // Left
        }
        else if (moveInput.y > 0)
        {
            MoveToLane(2); // Up
        }
        else if (moveInput.y < 0)
        {
            MoveToLane(3); // Down
        }
    }

    void MoveToLane(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= movePosition.Count) return;
        if (currentLane == laneIndex) return;

        currentLane = laneIndex;
        targetPosition = movePosition[laneIndex].position;

        // Compute dash velocity instantly toward target
        dashVelocity = (targetPosition - transform.position).normalized * dashSpeed;
    }

    void Update()
    {
        if (isGameOver) return;

        // Dash movement (quick glide)
        if (Vector3.Distance(transform.position, targetPosition) > snapThreshold)
        {
            transform.position += dashVelocity * Time.deltaTime;

            // Overshoot protection
            Vector3 toTarget = targetPosition - transform.position;
            if (Vector3.Dot(toTarget, dashVelocity) <= 0)
            {
                transform.position = targetPosition;
                dashVelocity = Vector3.zero;
            }
        }
        else if (hardSnapToLane && dashVelocity != Vector3.zero)
        {
            // Stop cleanly on lane
            transform.position = targetPosition;
            dashVelocity = Vector3.zero;
        }

        HitJudgement.health = Mathf.Clamp(HitJudgement.health, 0f, maxHealth);
        if (HitJudgement.health <= 0 && !isGameOver)
        {
            HitJudgement.health = 0f;
            HandleGameOver();
        }
    }

    void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("GAME OVER!");

        // Find and activate GameOverUI
        var ui = FindFirstObjectByType<GameOverUI>();
        if (ui != null)
        {
            ui.ShowGameOver(HitJudgement.score);
        }
        else
        {
            Debug.LogWarning("GameOverUI not found in scene!");
            Time.timeScale = 0f;
        }
    }
}
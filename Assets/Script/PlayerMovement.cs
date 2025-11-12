using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Lane Positions (0=Right, 1=Left, 2=Up, 3=Down)")]
    public List<Transform> movePosition;

    [Header("Dash Movement Settings")]
    public float dashSpeed = 25f;
    public float snapThreshold = 0.01f;
    public bool hardSnapToLane = true;

    [Header("Game State & Health")]
    public float maxHealth = 100f;
    public float healthDecreaseRate = 2f;
    private bool isGameOver = false;

    [Header("Player Sprites (Change per direction)")]
    public Sprite starRight;  // Lane 0
    public Sprite starLeft;   // Lane 1
    public Sprite starUp;     // Lane 2
    public Sprite starDown;   // Lane 3

    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private Vector3 targetPosition;
    private int currentLane = -1;
    private Vector3 centerPosition = Vector3.zero;
    private Vector3 dashVelocity;

    void Start()
    {
        // Cache komponen
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogWarning("⚠️ SpriteRenderer not found on Player object.");

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

        if (moveInput.x > 0) MoveToLane(0); // right
        else if (moveInput.x < 0) MoveToLane(1); // left
        else if (moveInput.y > 0) MoveToLane(2); // up
        else if (moveInput.y < 0) MoveToLane(3); // down
    }

    void MoveToLane(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= movePosition.Count) return;
        if (currentLane == laneIndex) return;

        currentLane = laneIndex;
        targetPosition = movePosition[laneIndex].position;
        dashVelocity = (targetPosition - transform.position).normalized * dashSpeed;

        // Ganti sprite sesuai arah
        UpdateSpriteForLane(laneIndex);
    }

    void UpdateSpriteForLane(int laneIndex)
    {
        if (spriteRenderer == null) return;

        switch (laneIndex)
        {
            case 0:
                spriteRenderer.sprite = starRight;
                break;
            case 1:
                spriteRenderer.sprite = starLeft;
                break;
            case 2:
                spriteRenderer.sprite = starUp;
                break;
            case 3:
                spriteRenderer.sprite = starDown;
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (isGameOver) return;

        if (Vector3.Distance(transform.position, targetPosition) > snapThreshold)
        {
            transform.position += dashVelocity * Time.deltaTime;
            Vector3 toTarget = targetPosition - transform.position;
            if (Vector3.Dot(toTarget, dashVelocity) <= 0)
            {
                transform.position = targetPosition;
                dashVelocity = Vector3.zero;
            }
        }
        else if (hardSnapToLane && dashVelocity != Vector3.zero)
        {
            transform.position = targetPosition;
            dashVelocity = Vector3.zero;
        }

        HitJudgement.health = Mathf.Clamp(HitJudgement.health, 0f, maxHealth);
        if (HitJudgement.health <= 0f && !isGameOver)
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

        SpawnNote.FreezeGameplay();

        var ui = FindFirstObjectByType<GameOverUI>();
        if (ui != null)
            ui.ShowGameOver(HitJudgement.score);
        else
        {
            Debug.LogWarning("GameOverUI not found in scene!");
            Time.timeScale = 0f;
        }
    }
}

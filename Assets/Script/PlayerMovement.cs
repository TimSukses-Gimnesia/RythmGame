using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; 

public class PlayerMovement : MonoBehaviour
{
    public List<Transform> movePosition;

    public float speed;
    private float timeToMove;

    public Vector2 moveInput;

    [Header("Game State & Health")]
    public float maxHealth = 100f;
    public float healthDecreaseRate = 2f; 
    public string gameOverSceneName = "GameOverScene"; 
    private bool isGameOver = false;
    // -----------------------------------------

    void Start()
    {
        // Reset semua status game saat mulai
        HitJudgement.health = maxHealth;
        HitJudgement.score = 0;
        HitJudgement.combo = 0;
        isGameOver = false;
        Time.timeScale = 1f; 
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {

        if (isGameOver) return;
        if (moveInput.x > 0)
        {
            transform.position = movePosition[0].position;
            timeToMove = 0;
        }
        else if (moveInput.x < 0)
        {
            transform.position = movePosition[1].position;
            timeToMove = 0;
        }
        else if (moveInput.y > 0)
        {
            transform.position = movePosition[2].position;
            timeToMove = 0;
        }
        else if (moveInput.y < 0)
        {
            transform.position = movePosition[3].position;
            timeToMove = 0;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, Vector3.zero, timeToMove);
            timeToMove += Time.deltaTime * speed;
        }


        //HitJudgement.health -= healthDecreaseRate * Time.deltaTime;

    
        //if (HitJudgement.health > maxHealth)
        //{
        //    HitJudgement.health = maxHealth;
        //}

        //if (HitJudgement.health <= 0)
        //{
        //    HitJudgement.health = 0;
        //    HandleGameOver();
        //}
    }

    void HandleGameOver()
    {
        //isGameOver = true;
        Debug.Log("GAME OVER!");

       //di command dulu yg bawah ntar aja game overnya

        //Time.timeScale = 0f;


    }
}
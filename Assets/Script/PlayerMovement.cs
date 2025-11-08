using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public List<Transform> movePosition;

    public float speed;
    private float timeToMove;

    public Vector2 moveInput;
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    void Update()
    {
        // Key Down
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
        }else
        {
            transform.position = Vector3.Lerp(transform.position, Vector3.zero, timeToMove);
            timeToMove += Time.deltaTime * speed;
        }
    }
}

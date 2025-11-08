using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public List<Transform> movePosition;

    public float speed;
    private float timeToMove;

    void Update()
    {
        // Key Down
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            transform.position = movePosition[0].position;
            timeToMove = 0;
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            transform.position = movePosition[1].position;
            timeToMove = 0;
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            transform.position = movePosition[2].position;
            timeToMove = 0;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            transform.position = movePosition[3].position;
            timeToMove = 0;
        }

        // Key Up check override
        if (Keyboard.current.dKey.wasReleasedThisFrame || 
            Keyboard.current.aKey.wasReleasedThisFrame ||
            Keyboard.current.wKey.wasReleasedThisFrame ||
            Keyboard.current.sKey.wasReleasedThisFrame)
        {
            // cek siapa yg masih hold
            if (Keyboard.current.dKey.isPressed)
                transform.position = movePosition[0].position;
            else if (Keyboard.current.aKey.isPressed)
                transform.position = movePosition[1].position;
            else if (Keyboard.current.wKey.isPressed)
                transform.position = movePosition[2].position;
            else if (Keyboard.current.sKey.isPressed)
                transform.position = movePosition[3].position;
            else
                timeToMove = 0; // biar langsung start lerp ke tengah
        }

        // idle → center
        if (!Keyboard.current.dKey.isPressed &&
            !Keyboard.current.aKey.isPressed &&
            !Keyboard.current.wKey.isPressed &&
            !Keyboard.current.sKey.isPressed)
        {
            transform.position = Vector3.Lerp(transform.position, Vector3.zero, timeToMove);
            timeToMove += Time.deltaTime * speed;
        }
    }
}

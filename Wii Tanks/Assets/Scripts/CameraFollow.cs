using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private static Transform player;

    public static Transform Player
    {
        get
        {
            return player;
        }
        set
        {
            player = value;
        }
    }


    private const float DAMPING = 12.0f;
    private const float HEIGHT = 25.0f;
    private const float OFFSET = 0.0f;
    private const float VIEW_DISTANCE = 4.0f;

    private void Start()
    {
        switch (Settings.Camera)
        {
            case "Camera1":
                transform.position = new Vector3(0, 26, -36);
                gameObject.transform.eulerAngles = new Vector3(40.345f, 0f, 0f);
                break;
            case "Camera2":
                transform.position = new Vector3(0, 0, HEIGHT);
                gameObject.transform.eulerAngles = new Vector3(90f, 0f, 0f);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void Update()
    {
        if (!player || Settings.Camera != "Camera2")
            return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = VIEW_DISTANCE;
        Vector3 CursorPosition = Camera.main.ScreenToWorldPoint(mousePos);

        Vector3 PlayerPosition = player.position;

        Vector3 center = new((PlayerPosition.x + CursorPosition.x) / 2, PlayerPosition.y, (PlayerPosition.z + CursorPosition.z) / 2);

        transform.position = Vector3.Lerp(transform.position, center + new Vector3(0, HEIGHT, OFFSET), Time.deltaTime * DAMPING);
    }
}

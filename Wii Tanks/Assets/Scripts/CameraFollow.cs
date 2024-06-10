using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private const float DAMPING = 12.0f;
    private const float HEIGHT = 25.0f;
    private const float OFFSET = 0.0f;
    private const float VIEW_DISTANCE = 4.0f;

    public static Transform Player { get; set; }

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
        if (!Player || Settings.Camera != "Camera2")
            return;

        var mousePos = Input.mousePosition;
        mousePos.z = VIEW_DISTANCE;
        var CursorPosition = Camera.main.ScreenToWorldPoint(mousePos);

        var PlayerPosition = Player.position;

        Vector3 center = new((PlayerPosition.x + CursorPosition.x) / 2, PlayerPosition.y,
            (PlayerPosition.z + CursorPosition.z) / 2);

        transform.position = Vector3.Lerp(transform.position, center + new Vector3(0, HEIGHT, OFFSET),
            Time.deltaTime * DAMPING);
    }
}
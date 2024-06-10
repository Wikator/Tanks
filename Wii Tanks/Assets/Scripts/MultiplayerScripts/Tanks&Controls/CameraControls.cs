using UnityEngine;

public class CameraControls : MonoBehaviour
{
    private Transform middlePoint;

    private void Start()
    {
        middlePoint = GameObject.Find("MiddlePoint").transform;
    }


    //Any player can rotate the camera whenever they want
    //The camera will only rotate for them, not on the server

    private void Update()
    {
        if (Input.GetKey(KeyCode.Q)) transform.RotateAround(middlePoint.position, Vector3.up, 50 * Time.deltaTime);

        if (Input.GetKey(KeyCode.E)) transform.RotateAround(middlePoint.position, Vector3.up, -50 * Time.deltaTime);
    }
}
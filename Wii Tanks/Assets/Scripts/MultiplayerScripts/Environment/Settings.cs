using UnityEngine;

public sealed class Settings : MonoBehaviour
{
    private GameObject background;



    void Start()
    {
        background = GameObject.Find("Background");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayerNetworking.Instance.DisconnectFromGame();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerNetworking.Instance.showPlayerNames = !PlayerNetworking.Instance.showPlayerNames;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            background.SetActive(!background.activeSelf);
        }
    }
}

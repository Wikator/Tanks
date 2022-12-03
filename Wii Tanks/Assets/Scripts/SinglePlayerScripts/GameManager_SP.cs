using System.Linq;
using UnityEngine;

public sealed class GameManager_SP : MonoBehaviour
{
    public static GameManager_SP Instance { get; private set; }
    public bool GameInProgress { get; private set; }


    private void Awake()
    {
        Instance = this;
        GameInProgress = false;
    }

    public void EndGame()
    {
        GameInProgress = false;
    }


    public void StartGame()
    {
        GameInProgress = true;

    }


    public void KillAllPlayers()
    {

    }
}

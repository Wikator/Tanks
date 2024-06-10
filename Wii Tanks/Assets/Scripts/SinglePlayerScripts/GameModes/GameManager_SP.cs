using ObjectPoolManager;
using UnityEngine;

public sealed class GameManager_SP : MonoBehaviour
{
    public static GameManager_SP Instance { get; private set; }
    public bool GameInProgress { get; private set; }


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameInProgress = true;
        UIManager_SP.Instance.Init();
        UIManager_SP.Instance.Show<MainView_SP>();
        ObjectPoolManager_SP.ResetObjectPool();
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
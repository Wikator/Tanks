using System.Linq;
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
        GameInProgress = false;
        UIManager_SP.Instance.Init();
        UIManager_SP.Instance.Show<MainView_SP>();
        ObjectPoolManager_SP.ResetObjectPool();

        CampaignModeManager_SP.Instance.StartGame();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            CampaignModeManager_SP.Instance.NextMap();
        }
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

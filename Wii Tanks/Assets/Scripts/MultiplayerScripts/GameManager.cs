using FishNet.Object;
using FishNet.Object.Synchronizing;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public string gameMode;

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> players = new();


    [field: SyncVar]
    public bool GameInProgress { get; private set; }


	private void Awake()
    {
        Instance = this;
        GameInProgress = false;
	}

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameMode = "None";

		UIManager.Instance.SetUpAllUI(false, gameMode);
	}


    private void OnDestroy()
    {
        Instance = null;
    }


    [Client]
    public int NumberOfReadyPlayers()
    {
        if (players.Count == 0)
            return 0;

        int playersReady = 0;

        foreach (PlayerNetworking player in players)
        {
            if (player.IsReady)
            {
                playersReady++;
            }
        }

        return playersReady;
    }

    [Server]
    public void EndGame()
    {
        GameInProgress = false;
        gameMode = "GameFinished";
        UIManager.Instance.SetUpAllUI(GameInProgress, gameMode);
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        GameInProgress = true;

        UIManager.Instance.SetUpAllUI(GameInProgress, gameMode);

        foreach (PlayerNetworking player in players)
        {
            player.SpawnTank();
        }

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }
    }


    [Server]
    public void KillAllPlayers()
    {
        foreach (PlayerNetworking player in players)
        {
            player.StartDespawningTank();
        }
    }
}

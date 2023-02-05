using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> players = new();


    [field: SyncVar]
    public bool GameInProgress { get; private set; }

    private string gameMode;

    public string GameMode
    {
        get
        {
            return gameMode;
        }
        set
        {
            gameMode = value;

            Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>(gameMode + "Manager").WaitForCompletion(), transform.position, Quaternion.identity));
        }
    }


	private void Awake()
    {
        Instance = this;
        GameInProgress = false;
	}

    public override void OnStartServer()
    {
        base.OnStartServer();

		UIManager.Instance.SetUpAllUI(false, "None");
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
        GameMode = "GameFinished";
        UIManager.Instance.SetUpAllUI(GameInProgress, GameMode);
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        GameInProgress = true;

        UIManager.Instance.SetUpAllUI(GameInProgress, GameMode);

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

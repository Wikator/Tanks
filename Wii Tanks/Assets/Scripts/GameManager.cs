using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using FishNet.Managing.Logging;
using System.Linq;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncVar]
    public string gameMode;


    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> players = new();


    [field: SyncVar]
    public bool GameInProgress { get; private set; }


    [field : SyncVar]
    public bool CanStart { get; private set; }


    private void Awake()
    {
        Instance = this;
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoaded;
        GameInProgress = false;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameMode = "None";
    }


    private void OnDestroy()
    {
        Instance = null;
    }


    [Server(Logging = LoggingType.Off)]
    public void Update()
    {
        CanStart = players.All(player => player.IsReady);
    }


    private void OnSceneLoaded(SceneLoadEndEventArgs args)
    {
        if (!args.QueueData.AsServer)
            return;

        if (args.LoadedScenes[0].name != "MapSelection" && args.LoadedScenes[0].name != "EndScreen")
        {
            UIManager.Instance.SetUpAllUI(GameInProgress, gameMode);
        }
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
            player.DespawnTank();
        }
    }
}

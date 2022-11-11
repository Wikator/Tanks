using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using FishNet.Managing.Logging;
using System.Linq;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncObject]
    public readonly SyncDictionary<string, int> scores = new();


    [SyncVar]
    public string gameMode;


    [field : SyncVar]
    public bool GameInProgress { get; private set; }


    [field : SyncVar]
    public bool CanStart { get; private set; }


    private void Awake()
    {
        Instance = this;
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoaded;
        scores.OnChange += OnScoreChange;
        GameInProgress = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        gameMode = "None";
    }


    private void OnDestroy()
    {
        Instance = null;
        scores.OnChange -= OnScoreChange;
    }


    [Server(Logging = LoggingType.Off)]
    public void Update()
    {
        CanStart = players.All(player => player.IsReady);
    }

    private void OnScoreChange(SyncDictionaryOperation op, string key, int value, bool asServer)
    {
        if (!MainView.Instance)
            return;

        if (op == SyncDictionaryOperation.Set)
        {
            MainView.Instance.UpdateScore(key, value);
        }
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

    public int NumberOfReadyPlayers()
    {
        int playersReady = 0;

        if (players.Count == 0)
            return 0;

        foreach (PlayerNetworking player in players)
        {
            if (player.IsReady)
            {
                playersReady++;
            }
        }

        return (playersReady);
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        GameInProgress = true;

        UIManager.Instance.SetUpAllUI(GameInProgress, gameMode);

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }

        foreach (PlayerNetworking player in players)
        {
            player.SpawnTank();
        }
    }
}

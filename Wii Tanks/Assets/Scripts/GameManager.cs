using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using FishNet.Managing.Logging;
using System.Linq;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    //Hashset is used, because there are no duplicates allowed, and indexing is not necessary

    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncObject]
    public readonly SyncDictionary<string, int> scores = new();

    [SyncVar, HideInInspector]
    public bool canStart;

    [SyncVar]
    public bool gameInProgress = false;


    [SyncVar]
    public string gameMode = "None";


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoaded;
        scores.OnChange += OnScoreChange;
    }

    private void OnDestroy()
    {
        Instance = null;
    }


    [Server(Logging = LoggingType.Off)]
    private void Update()
    {
        canStart = players.All(player => player.isReady);
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
            UIManager.Instance.SetUpAllUI(gameInProgress, Instance.gameMode);
        }
    }

    public int NumberOfReadyPlayers()
    {
        int playersReady = 0;

        if (players.Count == 0)
            return 0;

        foreach (PlayerNetworking player in players)
        {
            if (player.isReady)
            {
                playersReady++;
            }
        }

        return (playersReady);
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        gameInProgress = true;

        UIManager.Instance.SetUpAllUI(gameInProgress, gameMode);

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

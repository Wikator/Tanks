using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using FishNet.Managing.Logging;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncObject]
    public readonly SyncDictionary<string, int> scores = new();


    [SyncVar]
    private bool gameInProgress = false;

    [SyncVar]
    private bool canStart;


    public bool GameInProgress { get { return gameInProgress; } }

    public bool CanStart { get { return canStart; } }



    [field: SyncVar(OnChange = nameof(OnGameModeChange))]
    public string GameMode { get; [ServerRpc(RequireOwnership = false)] set; }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void OnGameModeChange(string oldVal, string newVal, bool asServer)
    {
        if (newVal == "None")
            return;

        if (oldVal == "None")
        {
            Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>(newVal + "Manager").WaitForCompletion(), transform.position, Quaternion.identity));
            UIManager.Instance.SetUpAllUI(false, newVal);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        GameMode = "None";
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
    public void Update()
    {
        canStart = players.All(player => player.IsReady);
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
            UIManager.Instance.SetUpAllUI(gameInProgress, Instance.GameMode);
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
        gameInProgress = true;

        UIManager.Instance.SetUpAllUI(gameInProgress, GameMode);

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

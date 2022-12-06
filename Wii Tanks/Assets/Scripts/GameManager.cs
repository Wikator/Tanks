using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Logging;
using System.Linq;
using UnityEngine;

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


    [SerializeField]
    private bool animateBackground;


	private void Awake()
    {
        Instance = this;
        GameInProgress = false;


		if (!animateBackground)
		{
			Destroy(GameObject.Find("Plane"));
		}
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


    [Server(Logging = LoggingType.Off)]
    public void Update()
    {
        CanStart = players.All(player => player.IsReady);
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
            StartCoroutine(player.SpawnTank(0f));
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

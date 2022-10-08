using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using System.Linq;
using UnityEngine;
using FishNet;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    //Hashset is used, because there are no duplicates allowed, and indexing is not necessary

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> players = new();

    [SyncVar, HideInInspector]
    public bool canStart;

    [SyncVar]
    public bool gameInProgress = false;

    [SyncVar]
    public int playersReady = 0;

    [SyncVar]
    public string gameMode = "None";


    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        canStart = players.All(player => player.isReady);
    }



    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        gameInProgress = true;

        foreach (PlayerNetworking player in players)
        {
            player.StartGame();
        }

        UIManager.Instance.SetUpAllUI(gameInProgress, gameMode);

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }
    }
}

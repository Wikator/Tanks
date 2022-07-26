using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    //[SyncObject]
    //public readonly SyncList<GameObject> spawns = new();

    [SyncObject]
    public readonly SyncList<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncList<PlayerNetworking> redTeam = new();

    [HideInInspector]
    [SyncVar]
    public bool canStart;

    [SyncVar]
    public int playersReady = 0;

    [SyncVar]
    public string gameMode = "None";

    [SyncVar]
    private bool gameModeChosen = false;


    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        canStart = players.All(player => player.isReady);

        if (gameMode != "None")
        {
            foreach (PlayerNetworking player in players)
            {
                if (!player.gameModeChosen)
                {
                    player.GameModeChosen(player.Owner, gameMode);
                    player.gameModeChosen = true;
                }
            }

            if (!gameModeChosen)
            {
                gameModeChosen = true;

                switch (gameMode)
                {
                    case "Deathmatch":
                        gameObject.AddComponent<DeathmatchGameMode>();
                        break;
                    case "Elimination":
                        gameObject.AddComponent<EliminationGameMode>();
                        break;
                }
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        if (!canStart)
            return;

        /*if (gameObject.TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.SetTeams();
        }*/

        for (int i = 0; i < players.Count; i++)
        {
            players[i].StartGame();
        }

        /*if (gameObject.TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }*/

        gameObject.GetComponent<EliminationGameMode>().waitingForNewRound = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopGame()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].StopGame();
        }
    }
}

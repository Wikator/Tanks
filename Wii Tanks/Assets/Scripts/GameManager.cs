using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncVar, HideInInspector]
    public bool canStart;

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

            if (!FindObjectOfType<GameMode>())
            {
                SpawnGameModeManager(gameMode);
            }
        }
    }

    //[ServerRpc(RequireOwnership = false)]
    private void SpawnGameModeManager(string gameMode)
    {
        switch (gameMode)
        {
            case "Deathmatch":
                Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>("DeathmatchManager").WaitForCompletion(), transform.position, Quaternion.identity));
                break;
            case "Elimination":
                Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>("EliminationManager").WaitForCompletion(), transform.position, Quaternion.identity));
                break;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        if (!canStart)
            return;


        for (int i = 0; i < players.Count; i++)
        {
            players[i].StartGame();
        }

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
            eliminationGameMode.waitingForNewRound = false;
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

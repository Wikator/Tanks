using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    }

    public void MapChosen(Button button)
    {
        button.onClick.AddListener(() => MapSelection.Instance.LoadScene(PlayerNetworking.Instance.gameObject.GetComponent<NetworkObject>(), button.name));

        /*foreach (PlayerNetworking player in players)
        {
            button.onClick.AddListener(() => player.OnSceneLoaded());
        }*/
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

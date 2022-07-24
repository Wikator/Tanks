using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncObject]
    public readonly SyncList<GameObject> spawns = new();

    [HideInInspector]
    [SyncVar]
    public bool canStart;

    [SyncVar]
    public int playersReady = 0;


    private void Awake()
    {
        Instance = this;

        for (int i = 1; i < 11; i++)
        {
            spawns.Add(GameObject.Find("Spawn" + i.ToString()));
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        canStart = players.All(player => player.isReady);
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

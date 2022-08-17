using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Linq;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncList<PlayerNetworking> players = new();

    [SyncVar, HideInInspector]
    public bool canStart, gameInProgress;

    [SyncVar]
    public int playersReady = 0;

    [SyncVar]
    public string gameMode = "None";


    private void Awake()
    {
        Instance = this;
        gameInProgress = false;
    }

    private void Start()
    {
        //InstanceFinder.SceneManager.OnClientPresenceChangeEnd += SceneLoaded;
    }

    private void Update()
    {
        canStart = players.All(player => player.isReady);
    }

    private void OnDestroy()
    {
        if (InstanceFinder.SceneManager)
        {
            //InstanceFinder.SceneManager.OnClientPresenceChangeEnd -= SceneLoaded;
        }
    }

    public void SceneLoaded(ClientPresenceChangeEventArgs obj)
    {
        if (obj.Scene.name == "MapSelection")
            return;
        try
        {
            PlayerNetworking player = obj.Connection.Objects.ToArray()[0].gameObject.GetComponent<PlayerNetworking>();
            player.SetUpUI(player.Owner, gameMode);
        }
        catch (IndexOutOfRangeException)
        {
            //obj.Connection.Disconnect(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGame()
    {
        if (!canStart)
            return;

        gameInProgress = true;

        foreach (PlayerNetworking player in players)
        {
            player.StartGame();
        }

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
            eliminationGameMode.waitingForNewRound = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopGame()
    {
        foreach(PlayerNetworking player in players)
        {
            player.StopGame();
        }
    }
}

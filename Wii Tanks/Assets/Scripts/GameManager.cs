using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
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
    public bool gameInProgress;

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
        //InstanceFinder.SceneManager.OnClientLoadedStartScenes += ClientConnected;
        InstanceFinder.SceneManager.OnClientPresenceChangeEnd += SceneLoaded;
    }

    private void Update()
    {
        canStart = players.All(player => player.isReady);
    }

    private void OnDestroy()
    {
        if (InstanceFinder.SceneManager)
        {
            //InstanceFinder.SceneManager.OnClientLoadedStartScenes -= ClientConnected;
            InstanceFinder.SceneManager.OnClientPresenceChangeEnd -= SceneLoaded;
        }
    }

    private void ClientConnected(NetworkConnection connection, bool asServer)
    {
        if (!asServer)
            return;

        GameObject player = Instantiate(Addressables.LoadAssetAsync<GameObject>("PlayerNetworking").WaitForCompletion());
        Spawn(player, connection);
    }

    private void SceneLoaded(ClientPresenceChangeEventArgs obj)
    {
        if (obj.Scene.name != "MapSelection" && obj.Added)
        {
            PlayerNetworking player = obj.Connection.Objects.ToArray()[0].gameObject.GetComponent<PlayerNetworking>();
            player.SetUpUI(player.Owner, gameMode, gameInProgress);
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
            player.SetUpUI(player.Owner, gameMode, gameInProgress);
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

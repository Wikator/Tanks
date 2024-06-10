using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Steamworks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public Tank ControlledPawn;


    [SyncVar] public string color;


    [SyncVar(ReadPermissions = ReadPermission.OwnerOnly)]
    public double superCharge;

    private GameObject background;
    public static PlayerNetworking Instance { get; private set; }

    [field: SyncVar] public ulong PlayerSteamID { get; private set; }

    [field: SyncVar] public string PlayerUsername { get; private set; }

    public string TankType { get; [ServerRpc(RunLocally = true)] set; }


    [field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public bool IsReady { get; [ServerRpc(RunLocally = true)] set; }


    //Each player adds themselves to the players list in the GameManager class, adn removes themselves when disconnecting

    private void Start()
    {
        background = GameObject.Find("Background");
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.P)) DisconnectFromGame();

        if (Input.GetKeyDown(KeyCode.T)) Settings.ShowPlayerNames = !Settings.ShowPlayerNames;

        if (Input.GetKeyDown(KeyCode.B)) background.SetActive(!background.activeSelf);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        superCharge = 0f;

        try
        {
            GameManager.Instance.players.Add(this);
        }
        catch (NullReferenceException)
        {
            Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>("GameManager").WaitForCompletion()));
            GameManager.Instance.players.Add(this);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;

        Settings.ShowPlayerNames = false;

        color = "None";
        TankType = "None";

        SetSteamID(SteamUser.GetSteamID().m_SteamID);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
            Instance = null;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();


        if (!GameManager.Instance)
            return;


        GameManager.Instance.players.Remove(this);

        if (GameManager.Instance.GameMode == "Deathmatch")
            return;


        var eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        if (eliminationGameMode)
        {
            if (eliminationGameMode.greenTeam.Contains(this)) eliminationGameMode.greenTeam.Remove(this);

            if (eliminationGameMode.redTeam.Contains(this)) eliminationGameMode.redTeam.Remove(this);

            eliminationGameMode.OnKilled(this);
        }

        if (FindObjectOfType<StockBattleGameMode>())
            if (StockBattleGameMode.defeatedPlayers.Contains(this))
                StockBattleGameMode.defeatedPlayers.Remove(this);
    }


    public void DisconnectFromGame()
    {
        if (!IsOwner)
            return;


        if (IsHost)
        {
            ClientManager.StopConnection();
            ServerManager.StopConnection(true);
        }
        else
        {
            ClientManager.StopConnection();
        }

        SteamMatchmaking.LeaveLobby(SteamLobby.LobbyID);
    }

    [Server]
    public void SpawnTank()
    {
        if (TankType == "None")
            return;

        var playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(TankType + "Pawn").WaitForCompletion(),
            GameMode.Instance.FindSpawnPosition(color), Quaternion.identity, transform);
        ControlledPawn = playerInstance.GetComponent<Tank>();
        ControlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
        playerInstance.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = false;
        playerInstance.transform.GetChild(0).GetChild(0).transform.GetChild(0).gameObject.GetComponent<MeshRenderer>()
            .enabled = false;
    }

    //Method for when a tank needs to be killed in order to start a new round

    [Server]
    public void StartDespawningTank()
    {
        if (ControlledPawn)
            if (ControlledPawn.IsSpawned)
                ControlledPawn.isDespawning = true;

        ControlledPawn = null;
    }

    //Methods that are called when in the lobby, when choosing colors, teams etc.

    [ServerRpc]
    public void SetTeam(string color)
    {
        var eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        switch (color)
        {
            case "Green":
                if (!eliminationGameMode.greenTeam.Contains(this) && eliminationGameMode.greenTeam.Count < 3)
                {
                    if (eliminationGameMode.redTeam.Contains(this)) eliminationGameMode.redTeam.Remove(this);
                    eliminationGameMode.greenTeam.Add(this);
                    this.color = color;
                }

                break;
            case "Red":
                if (!eliminationGameMode.redTeam.Contains(this) && eliminationGameMode.redTeam.Count < 3)
                {
                    if (eliminationGameMode.greenTeam.Contains(this)) eliminationGameMode.greenTeam.Remove(this);
                    eliminationGameMode.redTeam.Add(this);
                    this.color = color;
                }

                break;
        }
    }

    [ServerRpc]
    public void SetColor(string color)
    {
        this.color = color;
    }


    [ServerRpc]
    public void SetSteamID(ulong steamID)
    {
        PlayerSteamID = steamID;
        PlayerUsername = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
    }


    [ServerRpc]
    public void SetTankType(string tankType)
    {
        TankType = tankType;
    }

    [ServerRpc]
    public void SetReady(bool isReady)
    {
        IsReady = isReady;
    }

    [ServerRpc]
    public void SetSuperCharge(double superCharge)
    {
        this.superCharge = superCharge;
    }
}
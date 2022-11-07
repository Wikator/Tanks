using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }

    [SyncVar]
    public string color, tankType;

    [SyncVar]
    public Tank controlledPawn;

    [SyncVar, HideInInspector]
    public bool isReady;

    [SyncVar]
    public ulong playerSteamID;

    [SyncVar]
    public string playerUsername;


    //Each player will add themself to the players hashset in the GameManager class

    public override void OnStartServer()
    {
        base.OnStartServer();

        color = "None";
        tankType = "None";

        try
        {
            GameManager.Instance.players.Add(this);
            Debug.Log(GameManager.Instance.players.Count);
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

        SetSteamID(SteamUser.GetSteamID().m_SteamID);
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();

        //GameManager.Instance.players.Remove(this);
        //Debug.Log(GameManager.Instance.players.Count);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        //SteamMatchmaking.LeaveLobby(SteamLobby.LobbyID);

        if (IsOwner)
            Instance = null;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        GameManager.Instance.players.Remove(this);

        if (GameManager.Instance.gameMode == "Deathmatch" || !FindObjectOfType<EliminationGameMode>())
            return;

        EliminationGameMode eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        if (eliminationGameMode.greenTeam.Contains(this))
        {
            eliminationGameMode.greenTeam.Remove(this);
        }

        if (eliminationGameMode.redTeam.Contains(this))
        {
            eliminationGameMode.redTeam.Remove(this);
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && IsOwner)
        {
            DisconnectFromGame();
        }
    }

    private void DisconnectFromGame()
    {
        if (IsHost)
        {
            ServerManager.StopConnection(true);
            ClientManager.StopConnection();
        }
        else if
            (IsClient) ClientManager.StopConnection();

        if (IsOwner)
            SteamMatchmaking.LeaveLobby(SteamLobby.LobbyID);
    }    

    public void SpawnTank()
    {
        if (tankType == "None")
            return;

        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(tankType + "Pawn").WaitForCompletion(), FindObjectOfType<GameMode>().FindSpawnPosition(color), Quaternion.identity, transform);
        controlledPawn = playerInstance.GetComponent<Tank>();
        controlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
    }

    public void DespawnTank()
    {
        if (controlledPawn != null && controlledPawn.IsSpawned)
        {
            controlledPawn.GameOver();
        }
    }
    

    //Methods that are called when in the lobby, when choosing colors, teams etc.

    [ServerRpc]
    public void ChangeColor(string colorName) => color = colorName;

    [ServerRpc]
    public void ChangeTankType(string tankTypeName) => tankType = tankTypeName;

    [ServerRpc]
    public void SetTeams(string color)
    {
        EliminationGameMode eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        switch (color)
        {
            case "Green":
                if (eliminationGameMode.redTeam.Contains(this))
                    eliminationGameMode.redTeam.Remove(this);

                if (!eliminationGameMode.greenTeam.Contains(this))
                    eliminationGameMode.greenTeam.Add(this);
                break;
            case "Red":
                if (eliminationGameMode.greenTeam.Contains(this))
                    eliminationGameMode.greenTeam.Remove(this);

                if (!eliminationGameMode.redTeam.Contains(this))
                    eliminationGameMode.redTeam.Add(this);
                break;
        }
    }


    [ServerRpc]
    public void ServerSetIsReady(bool value)
    {
        switch (value)
        {
            case true:
                isReady = true;
                break;
            case false:
                isReady = false;
                break;
        }
    }


    [ServerRpc]
    public void SetSteamID(ulong steamID)
    {
        playerSteamID = steamID;
        playerUsername = SteamFriends.GetFriendPersonaName((CSteamID)steamID);

        Debug.Log($"Updating Server for User {playerUsername}");
    }
}

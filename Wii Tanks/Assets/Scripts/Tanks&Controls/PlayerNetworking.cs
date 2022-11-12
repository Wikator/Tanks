using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Steamworks;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }

    /*
    [field : SyncVar]
    public Tank ControlledPawn { get; private set; }
    */

    [SyncVar]
    public Tank ControlledPawn;

    [field : SyncVar]
    public ulong PlayerSteamID { get; private set; }

    [field : SyncVar]
    public string PlayerUsername { get; private set; }




    [field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public string Color { get; [ServerRpc(RunLocally = true)] set; }


    //[field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public string TankType { get; [ServerRpc(RunLocally = true)] set; }


    [field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public bool IsReady { get; [ServerRpc(RunLocally = true)] set; }






    //Each player will add themself to the players hashset in the GameManager class

    public override void OnStartServer()
    {
        base.OnStartServer();

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

        Color = "None";
        TankType = "None";

        Instance = this;

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

        if (eliminationGameMode)
        {
            eliminationGameMode.OnKilled(this);
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
        else
        {
            if (IsClient)
                ClientManager.StopConnection();
        }

        if (IsOwner)
            SteamMatchmaking.LeaveLobby(SteamLobby.LobbyID);
    }

    [Server]
    public void SpawnTank()
    {
        if (TankType == "None")
            return;

        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(TankType + "Pawn").WaitForCompletion(), FindObjectOfType<GameMode>().FindSpawnPosition(Color), Quaternion.identity, transform);
        ControlledPawn = playerInstance.GetComponent<Tank>();
        ControlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
    }

    public void DespawnTank()
    {
        if (ControlledPawn)
        {
            if (ControlledPawn.IsSpawned)
            {
                ControlledPawn.GameOver();
            }
        }
    }
    

    //Methods that are called when in the lobby, when choosing colors, teams etc.


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
    public void SetSteamID(ulong steamID)
    {
        PlayerSteamID = steamID;
        PlayerUsername = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
    }
}

using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Steamworks;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }


    public Tank ControlledPawn;

    [field : SyncVar]
    public ulong PlayerSteamID { get; private set; }

    [field : SyncVar]
    public string PlayerUsername { get; private set; }




    [SyncVar]
    public string color;

    public string TankType { get; [ServerRpc(RunLocally = true)] set; }


    [field: SyncVar(ReadPermissions = ReadPermission.ExcludeOwner)]
    public bool IsReady { get; [ServerRpc(RunLocally = true)] set; }


    [SyncVar(ReadPermissions = ReadPermission.OwnerOnly)]
    public double superCharge;


    public bool ShowPlayerNames { get; private set; }






    //Each player will add themself to the players list in the GameManager class

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

        ShowPlayerNames = true;

        color = "None";
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

        if (GameManager.Instance.gameMode == "Deathmatch")
            return;

        
        EliminationGameMode eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        if (eliminationGameMode)
        {
            if (eliminationGameMode.greenTeam.Contains(this))
            {
                eliminationGameMode.greenTeam.Remove(this);
            }

            if (eliminationGameMode.redTeam.Contains(this))
            {
                eliminationGameMode.redTeam.Remove(this);
            }

            eliminationGameMode.OnKilled(this);
        }

        if (FindObjectOfType<StockBattleGameMode>())
        {
            if (StockBattleGameMode.defeatedPlayers.Contains(this))
            {
                StockBattleGameMode.defeatedPlayers.Remove(this);
            }
        }
    }

    [Client]
    private void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                DisconnectFromGame();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                ShowPlayerNames = !ShowPlayerNames;
            }
        }
    }

    private void DisconnectFromGame()
    {
        if (IsHost)
        {
            ServerManager.StopConnection(true);
        }
        else
        {
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

        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(TankType + "Pawn").WaitForCompletion(), GameMode.Instance.FindSpawnPosition(color), Quaternion.identity, transform);
        ControlledPawn = playerInstance.GetComponent<Tank>();
        ControlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
    }


    //Method for when a tank needs to be killed in order to start a new round

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
    public void SetTeam(string color)
    {
        EliminationGameMode eliminationGameMode = FindObjectOfType<EliminationGameMode>();

        switch (color)
        {
            case "Green":
                if (!eliminationGameMode.greenTeam.Contains(this) && eliminationGameMode.greenTeam.Count < 3)
                {
                    if (eliminationGameMode.redTeam.Contains(this))
                    {
                        eliminationGameMode.redTeam.Remove(this);
                    }
                    eliminationGameMode.greenTeam.Add(this);
                    this.color = color;
                }
                break;
            case "Red":
                if (!eliminationGameMode.redTeam.Contains(this) && eliminationGameMode.redTeam.Count < 3)
                {
                    if (eliminationGameMode.greenTeam.Contains(this))
                    {
                        eliminationGameMode.greenTeam.Remove(this);
                    }
                    eliminationGameMode.redTeam.Add(this);
                    this.color = color;
                }
                break;
        }
    }

    [ServerRpc]
    public void SetColor(string color) => this.color = color;



    [ServerRpc]
    public void SetSteamID(ulong steamID)
    {
        PlayerSteamID = steamID;
        PlayerUsername = SteamFriends.GetFriendPersonaName((CSteamID)steamID);
    }
}

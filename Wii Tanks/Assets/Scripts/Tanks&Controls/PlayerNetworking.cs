using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }

    [SyncVar]
    public string color, tankType;

    [SyncVar]
    public Tank controlledPawn;

    [SyncVar, HideInInspector]
    public bool isReady;


    //Each player will add themself to the players hashset in the GameManager class

    public override void OnStartServer()
    {
        base.OnStartServer();

        color = "None";
        tankType = "None";

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

    public override void OnStopServer()
    {
        base.OnStopServer();

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

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;
    }

    [Server]
    public void SpawnTank()
    {
        if (tankType == "None")
            return;


        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(tankType + "Pawn").WaitForCompletion(), FindObjectOfType<GameMode>().FindSpawnPosition(color), Quaternion.identity, transform);
        controlledPawn = playerInstance.GetComponent<Tank>();
        controlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
    }


    [Server]
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
                GameManager.Instance.playersReady++;
                break;
            case false:
                isReady = false;
                GameManager.Instance.playersReady--;
                break;
        }
    }
}

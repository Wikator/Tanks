using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }

    [SyncVar]
    public string color = "None", tankType = "None";

    [SyncVar]
    public Tank controlledPawn;

    [SyncVar, HideInInspector]
    public bool isReady;

    [SyncVar, HideInInspector]
    public int score = 0;


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

    public override void OnStopServer()
    {
        base.OnStopServer();

        GameManager.Instance.players.Remove(this);

        if (GameManager.Instance.gameMode == "Deathmatch" || !FindObjectOfType<EliminationGameMode>())
            return;

        if (FindObjectOfType<EliminationGameMode>().greenTeam.Contains(this))
        {
            FindObjectOfType<EliminationGameMode>().greenTeam.Remove(this);
        }

        if (FindObjectOfType<EliminationGameMode>().redTeam.Contains(this))
        {
            FindObjectOfType<EliminationGameMode>().redTeam.Remove(this);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;
    }

    public void StartGame()
    {
        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(tankType + "Pawn").WaitForCompletion(), FindObjectOfType<GameMode>().FindSpawnPosition(color), Quaternion.identity, transform);
        controlledPawn = playerInstance.GetComponent<Tank>();
        controlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
    }

    public void StopGame()
    {
        if (controlledPawn != null && controlledPawn.IsSpawned)
        {
            controlledPawn.GameOver();
        }
    }


    [ServerRpc]
    public void ChangeColor(string colorName) => color = colorName;

    [ServerRpc]
    public void ChangeTankType(string tankTypeName) => tankType = tankTypeName;

    [ServerRpc]
    public void SetTeams(string color)
    {
        switch (color)
        {
            case "Green":
                if (FindObjectOfType<EliminationGameMode>().redTeam.Contains(this))
                    FindObjectOfType<EliminationGameMode>().redTeam.Remove(this);

                if (!FindObjectOfType<EliminationGameMode>().greenTeam.Contains(this))
                    FindObjectOfType<EliminationGameMode>().greenTeam.Add(this);
                break;
            case "Red":
                if (FindObjectOfType<EliminationGameMode>().greenTeam.Contains(this))
                    FindObjectOfType<EliminationGameMode>().greenTeam.Remove(this);

                if (!FindObjectOfType<EliminationGameMode>().redTeam.Contains(this))
                    FindObjectOfType<EliminationGameMode>().redTeam.Add(this);
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

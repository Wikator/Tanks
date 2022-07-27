using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class PlayerNetworking : NetworkBehaviour
{
    public static PlayerNetworking Instance { get; private set; }

    [SyncVar]
    public string username, color;

    [SyncVar]
    public Tank controlledPawn;

    [HideInInspector]
    [SyncVar]
    public bool isReady, gameModeChosen = false;

    [HideInInspector]
    [SyncVar]
    public int score = 0;


    public override void OnStartServer()
    {
        base.OnStartServer();
        color = "None";
        GameManager.Instance.players.Add(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        color = "None";
        GameManager.Instance.players.Remove(this);

        if (GameManager.Instance.greenTeam.Contains(this))
        {
            GameManager.Instance.greenTeam.Remove(this);
        }

        if (GameManager.Instance.redTeam.Contains(this))
        {
            GameManager.Instance.redTeam.Remove(this);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;

        UIManager.Instance.Init();

        UIManager.Instance.Show<GameModesView>();
    }

    public void StartGame()
    {
        GameObject playerInstance = GameManager.Instance.gameObject.GetComponent<GameMode>().FindSpawnPosition(color);
        controlledPawn = playerInstance.GetComponent<Tank>();
        controlledPawn.controllingPlayer = this;
        Spawn(playerInstance, Owner);
        TargetPlayerSpawned(Owner);
    }

    public void StopGame()
    {
        if (controlledPawn != null && controlledPawn.IsSpawned)
        {
            controlledPawn.Despawn();
        }
    }

    private IEnumerator Respawn(float time)
    {
        yield return new WaitForSeconds(time);
        StartGame();
    }

    public void StartRespawn(float time) => StartCoroutine(Respawn(time));


    [ServerRpc]
    public void ChangeColor(string colorName) => color = colorName;

    [ServerRpc]
    public void SetTeams(string color)
    {
        switch (color)
        {
            case "Green":
                if (GameManager.Instance.redTeam.Contains(this))
                    GameManager.Instance.redTeam.Remove(this);

                if (!GameManager.Instance.greenTeam.Contains(this))
                    GameManager.Instance.greenTeam.Add(this);

                break;
            case "Red":
                if (GameManager.Instance.greenTeam.Contains(this))
                    GameManager.Instance.greenTeam.Remove(this);

                if (!GameManager.Instance.redTeam.Contains(this))
                    GameManager.Instance.redTeam.Add(this);
                break;
        }
    }


    [ServerRpc]
    public void ServerSetIsReady(bool value)
    {
        isReady = value;

        switch (value)
        {
            case true:
                GameManager.Instance.playersReady++;
                break;
            case false:
                GameManager.Instance.playersReady--;
                break;
        }
    }

    [TargetRpc]
    private void TargetPlayerSpawned(NetworkConnection network) => UIManager.Instance.Show<MainView>();

    [TargetRpc]
    public void GameModeChosen(NetworkConnection network, string gameMode)
    {
        switch (gameMode)
        {
            case "Deathmatch":
                UIManager.Instance.Show<DeathmatchLobbyView>();
                break;
            case "Elimination":
                UIManager.Instance.Show<EliminationLobbyView>();
                break;
        }
    }
}

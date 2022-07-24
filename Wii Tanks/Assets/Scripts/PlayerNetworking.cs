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
    public string username;

    [SyncVar]
    public Tank controlledPawn;

    [field: SyncVar]
    public string color;

    [HideInInspector]
    [SyncVar]
    public bool isReady;

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
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;

        UIManager.Instance.Init();

        UIManager.Instance.Show<LobbyView>();
    }

    public void StartGame()
    {
        int randomNumber = Random.Range(0, 10);
        if (GameManager.Instance.spawns[randomNumber].GetComponent<Spawn>().isOccupied)
        {
            StartGame();
        }
        else
        {
            GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pawn").WaitForCompletion(), GameManager.Instance.spawns[randomNumber].transform.position, Quaternion.identity, transform);
            GameManager.Instance.spawns[randomNumber].GetComponent<Spawn>().isOccupied = true;
            controlledPawn = playerInstance.GetComponent<Tank>();
            controlledPawn.controllingPlayer = this;
            Spawn(playerInstance, Owner);
            TargetPlayerSpawned(Owner);
        }
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

    public void PointScored(int numberOfPoints) => score += numberOfPoints;

    public void StartRespawn(float time) => StartCoroutine(Respawn(time));


    [ServerRpc]
    public void ChangeColor(string colorName) => color = colorName;


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
}

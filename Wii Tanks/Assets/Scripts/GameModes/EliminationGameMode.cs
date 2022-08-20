using FishNet.Object.Synchronizing;
using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using FishNet.Object;

public sealed class EliminationGameMode : GameMode
{
    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> redTeam = new();

    [HideInInspector]
    public bool waitingForNewRound = true;

    private Transform bulletEmpty;
    private int greenSpawnCount, redSpawnCount;


    public override void OnStartServer()
    {
        base.OnStartServer();

        bulletEmpty = GameObject.Find("Bullets").transform;

        Transform greenSpawnsParent = GameObject.Find("GreenSpawns").transform;
        Transform redSpawnsParent = GameObject.Find("RedSpawns").transform;

        greenSpawnCount = greenSpawnsParent.childCount;
        redSpawnCount = redSpawnsParent.childCount;

        spawns["Green"] = new Transform[greenSpawnCount];
        spawns["Red"] = new Transform[redSpawnCount];

        for (int i = 0; i < greenSpawnCount; i++)
        {
            spawns["Green"][i] = greenSpawnsParent.GetChild(i).transform;
        }

        for (int i = 0; i < redSpawnCount; i++)
        {
            spawns["Red"][i] = redSpawnsParent.GetChild(i).transform;
        }
    }

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = color switch
        {
            "Green" => UnityEngine.Random.Range(0, greenSpawnCount),
            "Red" => UnityEngine.Random.Range(0, redSpawnCount),
            _ => 0
        };

        if (spawns[color][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            try
            {
                return FindSpawnPosition(color);
            }
            catch (StackOverflowException)
            {
                spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
                return spawns[color][randomNumber].position;
            }
        }
        else
        {
            spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return spawns[color][randomNumber].position;
        }
    }

    public override void OnKilled(PlayerNetworking controllingLayer)
    {
        return;
    }

    private void Update()
    {
        if (waitingForNewRound || !IsServer)
            return;

        if (greenTeam.All(player => !player.controlledPawn) && greenTeam.Count != 0)
        {
            foreach (PlayerNetworking player in redTeam)
            {
                PointScored(player, 1);
            }
            Debug.Log(0);
            StartCoroutine(NewRound());
            return;
        }

        if (redTeam.All(player => !player.controlledPawn) && redTeam.Count != 0)
        {
            foreach (PlayerNetworking player in greenTeam)
            {
                PointScored(player, 1);
            }

            Debug.Log(0);

            StartCoroutine(NewRound());
            return;
        }    
    }


    [Server]
    public void StartNewRound(GameManager gameManager)
    {
        foreach (PlayerNetworking player in gameManager.players)
        {
            player.StartGame();
        }

        UIManager.Instance.SetUpUI(gameManager.gameInProgress, gameManager.gameMode);

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }
    }

    [Server]
    public void KillAllPlayers()
    {
        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            player.StopGame();
        }
    }


    private IEnumerator NewRound()
    {
        waitingForNewRound = true;

        yield return new WaitForSeconds(3.5f);

        KillAllPlayers();

        foreach (Transform child in bulletEmpty)
        {
            child.GetComponent<BulletScript>().Despawn();
        }

        yield return new WaitForSeconds(2.0f);

        StartNewRound(GameManager.Instance);
    }
}

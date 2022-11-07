using FishNet.Object.Synchronizing;
using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using FishNet.Object;

public sealed class EliminationGameMode : GameMode
{
    //Each player will be added to the HashSet appropriote to their chosen team

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncHashSet<PlayerNetworking> redTeam = new();

    [HideInInspector]
    public bool waitingForNewRound = true;

    private Transform bulletEmpty;
    private int greenSpawnCount, redSpawnCount;

    [SyncVar, SerializeField]
    private int pointsToWin;


    //Before start of the game, this script finds and saves all possible spawn points
    //There are different spawns for different teams, so they are seperated inside a dictionary

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

        GameManager.Instance.scores["Green"] = 0;
        GameManager.Instance.scores["Red"] = 0;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        if (waitingForNewRound || !IsServer)
            return;

        switch (playerNetworking.color)
        {
            case "Green":
                if (greenTeam.All(player => !player.controlledPawn) && greenTeam.Count != 0)
                {
                    PointScored("Red", 1);

                    if (GameManager.Instance.scores["Red"] == pointsToWin)
                    {
                        LoadEndScene();
                    }
                    else
                    {
                        StartCoroutine(NewRound());
                    }
                }
                break;
            case "Red":
                if (redTeam.All(player => !player.controlledPawn) && redTeam.Count != 0)
                {
                    PointScored("Green", 1);

                    if (GameManager.Instance.scores["Green"] == pointsToWin)
                    {
                        LoadEndScene();
                    }
                    else
                    {
                        StartCoroutine(NewRound());
                    }
                }
                break;
        }
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = color switch
        {
            "Green" => UnityEngine.Random.Range(0, greenSpawnCount),
            "Red" => UnityEngine.Random.Range(0, redSpawnCount),
            _ => 0
        };

        Transform spawn = spawns[color][randomNumber];

        if (spawn.GetComponent<Spawn>().isOccupied)
        {
            try
            {
                return FindSpawnPosition(color);
            }
            catch (StackOverflowException)
            {
                spawn.GetComponent<Spawn>().isOccupied = true;
                return spawn.position;
            }
        }
        else
        {
            spawn.GetComponent<Spawn>().isOccupied = true;
            return spawn.position;
        }
    }


    [Server]
    public void StartNewRound(GameManager gameManager)
    {
        foreach (PlayerNetworking player in gameManager.players)
        {
            player.SpawnTank();
        }

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
            player.DespawnTank();
        }
    }


    //Each player and bullet needs to be destroyed before the new round

    [Server]
    private IEnumerator NewRound()
    {
        waitingForNewRound = true;

        yield return new WaitForSeconds(3.5f);

        KillAllPlayers();

        foreach (Transform child in bulletEmpty)
        {
            child.GetComponent<Bullet>().Despawn();
        }

        yield return new WaitForSeconds(2.0f);

        StartNewRound(GameManager.Instance);
    }
}

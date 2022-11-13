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
    public readonly SyncList<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncList<PlayerNetworking> redTeam = new();

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

        scores["Green"] = 0;
        scores["Red"] = 0;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {

        if (waitingForNewRound)
            return;

        switch (playerNetworking.Color)
        {
            case "Green":
                if (greenTeam.All(player => !player.ControlledPawn) && greenTeam.Count != 0)
                {
                    PointScored("Red", 1);

                    if (scores["Red"] == pointsToWin)
                    {
                        UIManager.Instance.Show<EndScreen>();
                    }
                    else
                    {
                        StartCoroutine(NewRound());
                    }
                }
                break;
            case "Red":
                if (redTeam.All(player => !player.ControlledPawn) && redTeam.Count != 0)
                {
                    PointScored("Green", 1);

                    if (scores["Green"] == pointsToWin)
                    {
                        UIManager.Instance.Show<EndScreen>();
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



    //Each player and bullet needs to be destroyed before the new round

    [Server]
    private IEnumerator NewRound()
    {
        waitingForNewRound = true;

        yield return new WaitForSeconds(3.5f);

        GameManager.Instance.KillAllPlayers();

        foreach (Transform child in bulletEmpty)
        {
            if (child.GetComponent<Bullet>().IsSpawned)
                child.GetComponent<Bullet>().Despawn();
        }

        yield return new WaitForSeconds(2.0f);

        StartNewRound(GameManager.Instance);
    }
}

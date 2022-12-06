using System;
using System.Collections;
using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public sealed class TakedownGameMode : EliminationGameMode
{
    private int respawnSpawnCount;

    private readonly Dictionary<string, float> respawnTime = new();


    [SerializeField]
    private float originalRespawnTime;
    
    [SerializeField]
    private float respawnTimeMultiplier;

    public override void OnStartServer()
    {
        base.OnStartServer();

        Transform respawnSpawnsParent = GameObject.Find("DeathmatchSpawns").transform;

        respawnSpawnCount = respawnSpawnsParent.childCount;

        spawns["Respawn"] = new Transform[respawnSpawnCount];

        for (int i = 0; i < respawnSpawnCount; i++)
        {
            spawns["Respawn"][i] = respawnSpawnsParent.GetChild(i).transform;
        }

        respawnTime["Green"] = originalRespawnTime;
        respawnTime["Red"] = originalRespawnTime;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {

        if (waitingForNewRound || !GameManager.Instance.GameInProgress)
            return;

        base.OnKilled(playerNetworking);

        StartCoroutine(playerNetworking.SpawnTank(respawnTime[playerNetworking.color]));
        respawnTime[playerNetworking.color] *= respawnTimeMultiplier;
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not

    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        if (waitingForNewRound)
        {
            return base.FindSpawnPosition(color);
        }
        else
        {
            color = "Respawn";

            Transform spawn = spawns[color][UnityEngine.Random.Range(0, respawnSpawnCount)];

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
    }


    [Server]
    protected override void StartNewRound(GameManager gameManager)
    {
        base.StartNewRound(gameManager);

        StopAllCoroutines();
        respawnTime["Green"] = originalRespawnTime;
        respawnTime["Red"] = originalRespawnTime;
    }
}

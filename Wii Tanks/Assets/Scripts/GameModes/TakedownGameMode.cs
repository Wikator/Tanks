using System;
using System.Collections;
using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public sealed class TakedownGameMode : EliminationGameMode
{
    private int respawnSpawnCount;

    private readonly Dictionary<string, float> respawnTime = new();

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

        respawnTime["Green"] = 3f;
        respawnTime["Red"] = 3f;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {

        if (waitingForNewRound || !GameManager.Instance.GameInProgress)
            return;

        base.OnKilled(playerNetworking);

        StartCoroutine(Respawn(playerNetworking, respawnTime[playerNetworking.color]));
        respawnTime[playerNetworking.color] *= 1.5f;
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
        respawnTime["Green"] = 3f;
        respawnTime["Red"] = 3f;
    }

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        if (!waitingForNewRound && GameManager.Instance.GameInProgress)
            controllingPLayer.SpawnTank();
    }
}

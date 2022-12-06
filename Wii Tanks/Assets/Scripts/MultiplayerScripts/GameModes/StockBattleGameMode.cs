using System;
using UnityEngine;
using System.Collections;
using FishNet.Object.Synchronizing;
using FishNet.Object;
using System.Collections.Generic;

public sealed class StockBattleGameMode : GameMode
{
    [SyncObject]
    public readonly SyncDictionary<string, int> lifeRemaining = new();

    [HideInInspector]
    public static List<PlayerNetworking> defeatedPlayers = new();


    private int spawnCount;

    [SerializeField]
    private float respawnTime;

    [SerializeField]
    private int playerLife;


    //Before start of the game, this script finds and saves all possible spawn points

    public override void OnStartServer()
    {
        base.OnStartServer();

        Transform spawnsParent = GameObject.Find("DeathmatchSpawns").transform;

        spawnCount = spawnsParent.childCount;

        spawns["NoTeams"] = new Transform[spawnCount];

        for (int i = 0; i < spawnCount; i++)
        {
            spawns["NoTeams"][i] = spawnsParent.GetChild(i).transform;
        }

        scores["Green"] = 0;
        scores["Red"] = 0;
        scores["Cyan"] = 0;
        scores["Purple"] = 0;
        scores["Yellow"] = 0;
        scores["Blue"] = 0;

        lifeRemaining["Green"] = playerLife;
        lifeRemaining["Red"] = playerLife;
        lifeRemaining["Cyan"] = playerLife;
        lifeRemaining["Purple"] = playerLife;
        lifeRemaining["Yellow"] = playerLife;
        lifeRemaining["Blue"] = playerLife;
    }



    //Each tank will loose a point and respawn after some time
    //Only DeathmachGameMode currently uses this method
    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        lifeRemaining[playerNetworking.color] -= 1;

        if (lifeRemaining[playerNetworking.color] == 0)
        {
            defeatedPlayers.Add(playerNetworking);

            if (defeatedPlayers.Count == GameManager.Instance.players.Count - 1)
            {
                GameManager.Instance.EndGame();
            }
        }
        else
        {
            StartCoroutine(playerNetworking.SpawnTank(respawnTime));
        }
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not
    //Color variable is unnecessary, but still needs to be here because it's used by the abstract method

    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        color = "NoTeams";

        Transform spawn = spawns[color][UnityEngine.Random.Range(0, spawnCount)];

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

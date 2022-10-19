using System;
using UnityEngine;
using System.Collections;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Logging;
using FishNet.Object;

public sealed class DeathmatchGameMode : GameMode
{
    private int spawnCount;

    [SyncVar]
    public float time;


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
        scores["Brown"] = 0;
    }

    [Server(Logging = LoggingType.Off)]
    private void Update()
    {
        if (IsServer && GameManager.Instance.gameInProgress)
        {
            if (time > 0)
            {
                time -= Time.deltaTime;
            }
            else
            {
                LoadEndScene();
            }
        }
    }


    //Each tank will loose a point and respawn after some time
    //Only DeathmachGameMode currently uses this method

    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        StartCoroutine(Respawn(playerNetworking, 1.5f));
        PointScored(playerNetworking.color, -1);
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not
    //Color variable is unnecessary here, but still needs to be here because it's used by the abstract method

    public override Vector3 FindSpawnPosition(string color)
    {
        color = "NoTeams";

        Transform spawn = spawns[color][UnityEngine.Random.Range(0, spawnCount)];

        if (!spawn)
            return Vector3.zero;

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

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        controllingPLayer.SpawnTank();
    }
}

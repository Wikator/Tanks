using System;
using UnityEngine;
using System.Collections;

public sealed class DeathmatchGameMode : GameMode
{
    private int spawnCount;

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
    }

    private void Update()
    {
        if (!GameManager.Instance.gameInProgress)
            return;

        if (time > 0)
        {
            time -= Time.deltaTime;
        }
        else
        {
            Debug.Log("Time has run out");
        }
    }


    //Each tank will loose a point and respawn after some time
    //Only DeathmachGameMode currently uses this method

    public override void OnKilled(PlayerNetworking controllingPlayer)
    {
        StartCoroutine(Respawn(controllingPlayer, 1.5f));
        PointScored(controllingPlayer, -1);
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not
    //Color variable is unnecessary here, but still needs to be here because it's used by the abstract method

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

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        controllingPLayer.SpawnTank();
    }
}

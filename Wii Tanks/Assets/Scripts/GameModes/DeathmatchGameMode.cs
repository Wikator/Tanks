using System;
using UnityEngine;
using System.Collections;

public sealed class DeathmatchGameMode : GameMode
{
    private int spawnCount;

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

    public override void OnKilled(PlayerNetworking controllingPlayer)
    {
        StartCoroutine(Respawn(controllingPlayer, 1.5f));
        PointScored(controllingPlayer, -1);
    }

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
        controllingPLayer.StartGame();
    }
}

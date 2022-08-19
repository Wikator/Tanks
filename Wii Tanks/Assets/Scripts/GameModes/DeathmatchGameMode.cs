using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public sealed class DeathmatchGameMode : GameMode
{
    private void Start()
    {
        spawns["NoTeams"] = new List<Transform>();

        foreach (Transform spawn in GameObject.Find("DeathmatchSpawns").transform)
        {
            spawns["NoTeams"].Add(spawn);
        }
    }

    public override void OnKilled(PlayerNetworking controllingPlayer)
    {
        StartCoroutine(Respawn(controllingPlayer, 1.5f));
        PointScored(controllingPlayer, -1);
    }

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = UnityEngine.Random.Range(0, 10);
        if (spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            try
            {
                return FindSpawnPosition(color);
            }
            catch (StackOverflowException)
            {
                spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied = true;
                return spawns["NoTeams"][randomNumber].position;
            }
        }
        else
        {
            spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return spawns["NoTeams"][randomNumber].position;
        }
    }


    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        controllingPLayer.StartGame();
    }
}

using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class DeathmatchGameMode : GameMode
{
    private void Start()
    {
        spawns["NoTeams"] = new List<GameObject>();

        for (int i = 1; i < 11; i++)
        {
            spawns["NoTeams"].Add(GameObject.Find("DeathmatchSpawn" + Convert.ToString(i)));
        }
    }

    public override void OnKilled(PlayerNetworking controllingPlayer)
    {
        controllingPlayer.StartRespawn(1.5f);
        PointScored(controllingPlayer, -1);
    }

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = UnityEngine.Random.Range(0, 10);
        //Cursor.visible = false;
        if (spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            return FindSpawnPosition(color);
        }
        else
        {
            spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return spawns["NoTeams"][randomNumber].transform.position;
            //playerInstance.GetComponent<Tank>().pointer = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pointer").WaitForCompletion(), playerInstance.transform.position, Quaternion.identity);
        }
    }
}

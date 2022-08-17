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
        StartCoroutine(Respawn(1.5f));
        PointScored(controllingPlayer, -1);
    }

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = UnityEngine.Random.Range(0, 10);
        //Cursor.visible = false;
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
            //playerInstance.GetComponent<Tank>().pointer = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pointer").WaitForCompletion(), playerInstance.transform.position, Quaternion.identity);
        }
    }


    private IEnumerator Respawn(float time)
    {
        yield return new WaitForSeconds(time);
        PlayerNetworking.Instance.StartGame();
    }
}

using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DeathmatchGameMode : GameMode
{
    private void Awake()
    {
        spawns["NoTeams"] = new List<GameObject>();

        for (int i = 1; i < 11; i++)
        {
            spawns["NoTeams"].Add(GameObject.Find("DeathmatchSpawn" + i.ToString()));
        }
    }

    public override void OnKilled(PlayerNetworking controllingPlayer)
    {
        base.OnKilled(controllingPlayer);
        controllingPlayer.StartRespawn(1.5f);
        PointScored(controllingPlayer, -1);
    }

    public override GameObject FindSpawnPosition(string color)
    {
        int randomNumber = Random.Range(0, 10);
        //Cursor.visible = false;
        if (FindObjectOfType<GameMode>().spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            return FindSpawnPosition(color);
        }
        else
        {
            GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pawn").WaitForCompletion(), FindObjectOfType<GameMode>().spawns["NoTeams"][randomNumber].transform.position, Quaternion.identity, transform);
            FindObjectOfType<GameMode>().spawns["NoTeams"][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return playerInstance;
            //playerInstance.GetComponent<Tank>().pointer = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pointer").WaitForCompletion(), playerInstance.transform.position, Quaternion.identity);
        }
    }
}

using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EliminationGameMode : GameMode
{
    [SyncObject]
    public readonly SyncList<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncList<PlayerNetworking> redTeam = new();

    private Transform bulletEmpty;

    public bool waitingForNewRound = true;


    private void Awake()
    {
        bulletEmpty = GameObject.Find("Bullets").transform;

        spawns["Green"] = new List<GameObject>();
        spawns["Red"] = new List<GameObject>();

        for (int i = 1; i < 4; i++)
        {
            spawns["Green"].Add(GameObject.Find("EliminationGreenSpawn" + Convert.ToString(i)));
            spawns["Red"].Add(GameObject.Find("EliminationRedSpawn" + Convert.ToString(i)));
        }
    }

    public override Vector3 FindSpawnPosition(string color)
    {
        int randomNumber = UnityEngine.Random.Range(0, 3);
        //Cursor.visible = false;
        if (spawns[color][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            try
            {
                return FindSpawnPosition(color);
            }
            catch (StackOverflowException)
            {
                spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
                return spawns[color][randomNumber].transform.position;
            }
        }
        else
        {
            spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return spawns[color][randomNumber].transform.position;
            //playerInstance.GetComponent<Tank>().pointer = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pointer").WaitForCompletion(), playerInstance.transform.position, Quaternion.identity);
        }
    }

    public override void OnKilled(PlayerNetworking controllingLayer)
    {
        return;
    }

    private void Update()
    {
        if (waitingForNewRound)
            return;

        bool playerLives = false;

        foreach (PlayerNetworking player in greenTeam)
        {
            if (player.controlledPawn != null)
            {
                playerLives = true;
                break;
            }
        }

        if (!playerLives && greenTeam.Count != 0)
        {
            foreach (PlayerNetworking player in redTeam)
            {
                PointScored(player, 1);
            }
            StartCoroutine(NewRound());
            return;
        }

        playerLives = false;

        foreach (PlayerNetworking player in redTeam)
        {
            if (player.controlledPawn != null)
            {
                playerLives = true;
                break;
            }
        }


        if (!playerLives && redTeam.Count != 0)
        {
            foreach (PlayerNetworking player in greenTeam)
            {
                PointScored(player, 1);
            }
            StartCoroutine(NewRound());
            return;
        }
      
    }

    private IEnumerator NewRound()
    {
        waitingForNewRound = true;
        yield return new WaitForSeconds(3.5f);

        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            if (player.controlledPawn != null)
            {
                player.controlledPawn.GameOver();
            }
        }

        foreach (Transform child in bulletEmpty)
        {
            child.GetComponent<BulletScript>().Despawn();
        }

        yield return new WaitForSeconds(2.0f);
        GameManager.Instance.StartGame();
    }
}

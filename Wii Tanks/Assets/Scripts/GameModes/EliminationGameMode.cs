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

    [HideInInspector]
    public bool waitingForNewRound = true;


    private void Awake()
    {
        bulletEmpty = GameObject.Find("Bullets").transform;

        spawns["Green"] = new List<Transform>();
        spawns["Red"] = new List<Transform>();

        foreach (Transform greenSpawn in GameObject.Find("GreenSpawns").transform)
        {
            spawns["Green"].Add(greenSpawn);
        }

        foreach (Transform redSpawn in GameObject.Find("RedSpawns").transform)
        {
            spawns["Red"].Add(redSpawn);
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
                return spawns[color][randomNumber].position;
            }
        }
        else
        {
            spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return spawns[color][randomNumber].position;
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
            if (player.controlledPawn)
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
            if (player.controlledPawn)
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

        GameManager.Instance.StopGame();

        foreach (Transform child in bulletEmpty)
        {
            child.GetComponent<BulletScript>().Despawn();
        }

        yield return new WaitForSeconds(2.0f);
        GameManager.Instance.StartGame();
    }
}

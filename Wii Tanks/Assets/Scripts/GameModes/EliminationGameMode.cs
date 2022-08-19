using FishNet.Object.Synchronizing;
using System;
using System.Linq;
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


    public override void OnStartServer()
    {
        base.OnStartServer();

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


        if (greenTeam.All(player => !player.controlledPawn) && greenTeam.Count != 0)
        {
            foreach (PlayerNetworking player in redTeam)
            {
                PointScored(player, 1);
            }

            StartCoroutine(NewRound());
            return;
        }


        if (redTeam.All(player => !player.controlledPawn) && redTeam.Count != 0)
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

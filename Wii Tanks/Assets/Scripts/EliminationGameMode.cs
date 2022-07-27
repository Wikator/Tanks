using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class EliminationGameMode : GameMode
{

    /*public List<PlayerNetworking> greenTeam = new();
    public List<PlayerNetworking> redTeam = new();*/


    //[HideInInspector]
    //[SyncVar]
    public bool waitingForNewRound = true;


    private void Awake()
    {
        spawns["Green"] = new List<GameObject>();
        spawns["Red"] = new List<GameObject>();

        for (int i = 1; i < 4; i++)
        {
            spawns["Green"].Add(GameObject.Find("EliminationGreenSpawn" + i.ToString()));
            spawns["Red"].Add(GameObject.Find("EliminationRedSpawn" + i.ToString()));
        }
    }

    public override GameObject FindSpawnPosition(string color)
    {
        int randomNumber = Random.Range(0, 3);
        //Cursor.visible = false;
        if (GameManager.Instance.gameObject.GetComponent<GameMode>().spawns[color][randomNumber].GetComponent<Spawn>().isOccupied)
        {
            return FindSpawnPosition(color);
        }
        else
        {
            GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pawn").WaitForCompletion(), GameManager.Instance.gameObject.GetComponent<GameMode>().spawns[color][randomNumber].transform.position, Quaternion.identity, transform);
            GameManager.Instance.gameObject.GetComponent<GameMode>().spawns[color][randomNumber].GetComponent<Spawn>().isOccupied = true;
            return playerInstance;
            //playerInstance.GetComponent<Tank>().pointer = Instantiate(Addressables.LoadAssetAsync<GameObject>("Pointer").WaitForCompletion(), playerInstance.transform.position, Quaternion.identity);
        }
    }

    public override void OnKilled(PlayerNetworking controllingLayer)
    {
        base.OnKilled(controllingLayer);
    }

    private void Update()
    {
        Debug.Log("Green " + GameManager.Instance.greenTeam.Count);
        Debug.Log("Red " + GameManager.Instance.redTeam.Count);

        if (waitingForNewRound)
            return;

        bool playerLives = false;

        foreach (PlayerNetworking player in GameManager.Instance.greenTeam)
        {
            if (player.controlledPawn != null)
            {
                playerLives = true;
                break;
            }
        }

        if (!playerLives && GameManager.Instance.greenTeam.Count != 0)
        {
            foreach (PlayerNetworking player in GameManager.Instance.redTeam)
            {
                PointScored(player, 1);
            }
            StartCoroutine(NewRound());
            return;
        }

        playerLives = false;

        foreach (PlayerNetworking player in GameManager.Instance.redTeam)
        {
            if (player.controlledPawn != null)
            {
                playerLives = true;
                break;
            }
        }


        if (!playerLives && GameManager.Instance.redTeam.Count != 0)
        {
            foreach (PlayerNetworking player in GameManager.Instance.greenTeam)
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

        yield return new WaitForSeconds(2.0f);
        GameManager.Instance.StartGame();
    }
}

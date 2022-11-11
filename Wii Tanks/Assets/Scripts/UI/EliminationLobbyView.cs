using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public sealed class EliminationLobbyView : LobbyView
{
    [SerializeField]
    private List<Button> colorButtons = new();

    [SerializeField]
    private List<Button> tankTypesButtons = new();

    [SerializeField]
    private LobbyPlayerTag[] greenPlayerTags = new LobbyPlayerTag[3];

    [SerializeField]
    private LobbyPlayerTag[] redPlayerTags = new LobbyPlayerTag[3];


    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        toggleReadyButton.onClick.AddListener(() => PlayerNetworking.Instance.IsReady = !PlayerNetworking.Instance.IsReady);

        foreach (Button button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.Color = button.name);
            button.onClick.AddListener(() => PlayerNetworking.Instance.SetTeams(button.name));
        }

        foreach (Button button in tankTypesButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.TankType = button.name);
        }

        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());

        startGameButton.gameObject.SetActive(true);
    }

    [Client]

    private void Update()
    {
        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            for (int i = 0; i < 3; i++)
            {
                if (eliminationGameMode.greenTeam.Count > i)
                {
                    greenPlayerTags[i].gameObject.SetActive(true);
                    greenPlayerTags[i].steamID = eliminationGameMode.greenTeam[i].PlayerSteamID;
                    greenPlayerTags[i].SetPlayerValues(eliminationGameMode.greenTeam[i]);
                }
                else
                {
                    greenPlayerTags[i].gameObject.SetActive(false);
                }

                if (eliminationGameMode.redTeam.Count > i)
                {
                    redPlayerTags[i].gameObject.SetActive(true);
                    redPlayerTags[i].steamID = eliminationGameMode.redTeam[i].PlayerSteamID;
                    redPlayerTags[i].SetPlayerValues(eliminationGameMode.redTeam[i]);
                }
                else
                {
                    redPlayerTags[i].gameObject.SetActive(false);
                }
            }
        }
        
    }
}


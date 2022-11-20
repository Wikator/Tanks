using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public sealed class DeathmatchLobbyView : LobbyView
{
    [SerializeField]
    private List<Button> colorButtons = new();

    [SerializeField]
    private List<Button> tankTypesButtons = new();

    [SerializeField]
    private LobbyPlayerTag[] playerTags = new LobbyPlayerTag[6];


    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        toggleReadyButton.onClick.AddListener(() => PlayerNetworking.Instance.IsReady = !PlayerNetworking.Instance.IsReady);

        foreach (Button button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.SetColor(button.name));
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
        foreach (LobbyPlayerTag tag in playerTags)
        {
            tag.gameObject.SetActive(false);
        }

        foreach (Button button in colorButtons)
        {
            button.gameObject.SetActive(true);

            foreach (PlayerNetworking player in GameManager.Instance.players)
            {
                if (player.color == button.name)
                {
                    button.gameObject.SetActive(false);

                    foreach (LobbyPlayerTag tag in playerTags)
                    {
                        if (tag.color == player.color)
                        {
                            tag.gameObject.SetActive(true);
                            tag.steamID = player.PlayerSteamID;
                            tag.SetPlayerValues(player);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
}


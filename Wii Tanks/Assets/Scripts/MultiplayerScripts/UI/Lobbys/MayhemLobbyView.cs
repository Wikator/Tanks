using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public sealed class MayhemLobbyView : LobbyView
{
    [SerializeField] private List<Button> colorButtons = new();

    [SerializeField] private LobbyPlayerTag[] playerTags = new LobbyPlayerTag[6];

    [Client]
    private void Update()
    {
        foreach (var tag in playerTags) tag.gameObject.SetActive(false);

        foreach (var button in colorButtons)
        {
            button.gameObject.SetActive(true);

            foreach (PlayerNetworking player in GameManager.Instance.players)
                if (player.color == button.name)
                {
                    button.gameObject.SetActive(false);

                    foreach (var tag in playerTags)
                        if (tag.color == player.color)
                        {
                            tag.gameObject.SetActive(true);
                            tag.steamID = player.PlayerSteamID;
                            tag.SetPlayerValues(player);
                            break;
                        }

                    break;
                }
        }
    }


    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        toggleReadyButton.onClick.AddListener(() =>
            PlayerNetworking.Instance.IsReady = !PlayerNetworking.Instance.IsReady);

        foreach (var button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.SetColor(button.name));
            button.onClick.AddListener(() => PlayerNetworking.Instance.TankType = "MediumTank");
        }

        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());

        startGameButton.gameObject.SetActive(true);
    }
}
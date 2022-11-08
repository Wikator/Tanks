using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class LobbyView : View
{
    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText, playersReadyCountText, chosenColorText, chosenTankTypeText;

    [SerializeField]
    protected Button toggleReadyButton;

    [SerializeField]
    protected Button startGameButton;

    [SerializeField]
    private LobbyPlayerTag[] playerTags = new LobbyPlayerTag[6];




    //Each player will have to choose a color and tank type, thanks to the subclasses EliminationLobbyView and DeathmatchLobbyView
    //Only when both are chosen toggleReadyButton will become interactable
    //Once all players are ready, startGameButton will become interactable

    private void LateUpdate()
    {
        if (!Initialized)
            return;

        if (PlayerNetworking.Instance.color == "None" || PlayerNetworking.Instance.tankType == "None")
        {
            toggleReadyButton.interactable = false;
        }
        else
        {
            toggleReadyButton.interactable = true;
        }

        toggleReadyButtonText.color = PlayerNetworking.Instance.isReady ? Color.green : Color.red;

        if (GameManager.Instance.canStart && IsHost)
        {
            startGameButton.interactable = true;
        }
        else
        {
            startGameButton.interactable = false;
        }
        playersReadyCountText.text = "Players ready: " + Convert.ToString(GameManager.Instance.NumberOfReadyPlayers()) + "/" + Convert.ToString(GameManager.Instance.players.Count);
        chosenColorText.text = "Chosen color: " + PlayerNetworking.Instance.color;
        chosenTankTypeText.text = "Chosen tank type: " + PlayerNetworking.Instance.tankType;

        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        for (int i = 0; i < playerTags.Length; i++)
        {
            if (GameManager.Instance.players.Count > i)
            {
                playerTags[i].gameObject.SetActive(true);
                playerTags[i].steamID = GameManager.Instance.players[i].playerSteamID;
                playerTags[i].SetPlayerValues(GameManager.Instance.players[i]);
            }
            else
            {
                playerTags[i].gameObject.SetActive(false);
            }
        }

    }

}

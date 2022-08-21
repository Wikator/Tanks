using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public abstract class LobbyView : View
{
    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText, playersReadyCountText, chosenColorText, chosenTankTypeText;

    [SerializeField]
    protected Button toggleReadyButton;

    [SerializeField]
    protected Button startGameButton;

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
        startGameButton.interactable = GameManager.Instance.canStart;
        playersReadyCountText.text = "Players ready: " + Convert.ToString(GameManager.Instance.playersReady) + "/" + Convert.ToString(GameManager.Instance.players.Count);
        chosenColorText.text = "Chosen color: " + PlayerNetworking.Instance.color;
        chosenTankTypeText.text = "Chosen tank type: " + PlayerNetworking.Instance.tankType;
    }
}

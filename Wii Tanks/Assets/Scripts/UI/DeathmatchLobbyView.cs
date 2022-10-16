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


    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        toggleReadyButton.onClick.AddListener(() => PlayerNetworking.Instance.ServerSetIsReady(!PlayerNetworking.Instance.isReady));

        foreach (Button button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.ChangeColor(button.name));
        }

        foreach (Button button in tankTypesButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.ChangeTankType(button.name));
        }

        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());

        startGameButton.gameObject.SetActive(true);
    }

    [Client]
    private void Update()
    {
        foreach (Button button in colorButtons)
        {
            button.interactable = true;

            foreach (PlayerNetworking player in GameManager.Instance.players)
            {
                if (player.color == button.name)
                {
                    button.interactable = false;
                    break;
                }
            }
        }
    }
}


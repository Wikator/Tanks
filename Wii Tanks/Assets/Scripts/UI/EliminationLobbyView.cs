using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public sealed class EliminationLobbyView : LobbyView
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
}


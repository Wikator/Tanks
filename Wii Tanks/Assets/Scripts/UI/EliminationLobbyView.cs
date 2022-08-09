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
        toggleReadyButton.onClick.AddListener(() => PlayerNetworking.Instance.ServerSetIsReady(!PlayerNetworking.Instance.isReady));

        foreach (Button button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.ChangeColor(button.name));
            button.onClick.AddListener(() => PlayerNetworking.Instance.SetTeams(button.name));
        }

        foreach (Button button in tankTypesButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.ChangeTankType(button.name));
        }

        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());

        startGameButton.gameObject.SetActive(true);

        base.Init();
    }
}


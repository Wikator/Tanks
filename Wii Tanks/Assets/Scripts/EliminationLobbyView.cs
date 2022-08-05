using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public sealed class EliminationLobbyView : View
{
    [SerializeField]
    private Button toggleReadyButton;

    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText, playersReadyCountText, chosenColorText;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private List<Button> colorButtons = new();


    public override void Init()
    {
        toggleReadyButton.onClick.AddListener(() => PlayerNetworking.Instance.ServerSetIsReady(!PlayerNetworking.Instance.isReady));

        foreach (Button button in colorButtons)
        {
            button.onClick.AddListener(() => PlayerNetworking.Instance.ChangeColor(button.name));
            button.onClick.AddListener(() => PlayerNetworking.Instance.SetTeams(button.name));
        }

        //if (InstanceFinder.IsHost)
        //{
        startGameButton.onClick.AddListener(() => GameManager.Instance.StartGame());

        startGameButton.gameObject.SetActive(true);
        //}
        //else
        //{
        //startGameButton.gameObject.SetActive(false);
        //}

        base.Init();
    }



    private void Update()
    {
        if (!Initialized)
            return;

        if (PlayerNetworking.Instance.color == "None")
        {
            toggleReadyButton.interactable = false;
        }
        else
        {
            toggleReadyButton.interactable = true;
        }

        toggleReadyButtonText.color = PlayerNetworking.Instance.isReady ? Color.green : Color.red;

        startGameButton.interactable = GameManager.Instance.canStart;

        playersReadyCountText.text = "Players ready: " + GameManager.Instance.playersReady.ToString() + "/" + GameManager.Instance.players.Count.ToString();

        chosenColorText.text = "Chosen color: " + PlayerNetworking.Instance.color.ToString();
    }
}


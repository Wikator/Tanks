using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class LobbyView : View
{
    [SerializeField] private TextMeshProUGUI toggleReadyButtonText, playersReadyCountText, chosenTankTypeText;

    [SerializeField] protected Button toggleReadyButton;

    [SerializeField] protected Button startGameButton;

    [SerializeField] private Button inviteButton;

    public static LobbyView Instance { get; private set; }


    private void Awake()
    {
        Instance = this;

        toggleReadyButton.interactable = false;
        startGameButton.interactable = false;
    }


    //Each player will have to choose a color and tank type, thanks to the subclasses EliminationLobbyView and DeathmatchLobbyView
    //Only when both are chosen toggleReadyButton will become interactable
    //Once all players are ready, startGameButton will become interactable

    private void LateUpdate()
    {
        if (!Initialized)
            return;


        toggleReadyButtonText.color = PlayerNetworking.Instance.IsReady ? Color.green : Color.red;

        if (PlayerNetworking.Instance.color == "None" || PlayerNetworking.Instance.TankType == "None")
            toggleReadyButton.interactable = false;
        else
            toggleReadyButton.interactable = true;

        if (IsHost)
            startGameButton.interactable =
                GameManager.Instance.NumberOfReadyPlayers() == GameManager.Instance.players.Count;

        playersReadyCountText.text =
            $"Players ready: {GameManager.Instance.NumberOfReadyPlayers()}/{GameManager.Instance.players.Count}";
        chosenTankTypeText.text = $"Chosen tank type: {PlayerNetworking.Instance.TankType}";
    }

    public override void Init()
    {
        base.Init();

        inviteButton.onClick.AddListener(() => SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.LobbyID));
    }
}
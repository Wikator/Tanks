using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameModesView : View
{
    [SerializeField] private Button deathmatchButton;

    [SerializeField] private Button eliminationButton;

    [SerializeField] private Button takedownButton;

    [SerializeField] private Button stockBattleButton;

    [SerializeField] private Button MayhemButton;


    //Game mode selection screen
    //Each player can choose a game mode

    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        deathmatchButton.onClick.AddListener(() => ChangeGameMode("Deathmatch"));

        eliminationButton.onClick.AddListener(() => ChangeGameMode("Elimination"));

        takedownButton.onClick.AddListener(() => ChangeGameMode("Takedown"));

        stockBattleButton.onClick.AddListener(() => ChangeGameMode("StockBattle"));

        MayhemButton.onClick.AddListener(() => ChangeGameMode("Mayhem"));
    }


    [ServerRpc(RequireOwnership = false)]
    private void ChangeGameMode(string gameMode)
    {
        GameManager.Instance.GameMode = gameMode;
        UIManager.Instance.SetUpAllUI(false, gameMode);
    }
}
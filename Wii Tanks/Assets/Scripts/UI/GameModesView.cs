using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public sealed class GameModesView : View
{
    [SerializeField]
    private Button deathmatchButton;

    [SerializeField]
    private Button eliminationButton;

    [SerializeField]
    private Button takedownButton;

    [SerializeField]
    private Button stockBattleButton;


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
    }


    [ServerRpc(RequireOwnership = false)]
    private void ChangeGameMode(string gameMode)
    {
        GameManager.Instance.gameMode = gameMode;
        Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>(gameMode + "Manager").WaitForCompletion(), transform.position, Quaternion.identity));
        UIManager.Instance.SetUpAllUI(false, gameMode);
    }
}

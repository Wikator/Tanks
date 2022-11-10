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


    //Game mode selection screen
    //Each player can choose a game mode

    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        deathmatchButton.onClick.AddListener(() => GameManager.Instance.GameMode = "Deathmatch");

        eliminationButton.onClick.AddListener(() => GameManager.Instance.GameMode = "Elimination");
    }

    //Once any player chooses a game mode, a ServerRPC is called so that the correct game mode will be selected on the server

    /*[ServerRpc(RequireOwnership = false)]
    private void ChangeGameMode(string gameMode)
    {
        GameManager.Instance.GameMode = gameMode;
        Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>(gameMode + "Manager").WaitForCompletion(), transform.position, Quaternion.identity));

        UIManager.Instance.SetUpAllUI(false, gameMode);
    }*/
}

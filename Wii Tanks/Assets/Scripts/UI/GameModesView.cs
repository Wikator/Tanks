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


    public override void Init()
    {
        base.Init();

        deathmatchButton.onClick.AddListener(() => ChangeGameMode("Deathmatch"));

        eliminationButton.onClick.AddListener(() => ChangeGameMode("Elimination"));
    }


    [ServerRpc(RequireOwnership = false)]
    private void ChangeGameMode(string gameMode)
    {
        GameManager.Instance.gameMode = gameMode;
        Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>(gameMode + "Manager").WaitForCompletion(), transform.position, Quaternion.identity));

        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            player.GameModeChosen(player.Owner, gameMode);
        }
    }
}

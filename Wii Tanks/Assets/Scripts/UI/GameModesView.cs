using FishNet.Object;
using UnityEngine;
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
    private void ChangeGameMode(string gameMode) => GameManager.Instance.gameMode = gameMode;
}

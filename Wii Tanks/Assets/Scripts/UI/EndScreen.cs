using System.Linq;
using UnityEngine;
using TMPro;
using FishNet.Object;

public sealed class EndScreen : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI leaderboardText;


    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        SetText();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            if (player.ControlledPawn)
            {
                player.ControlledPawn.Despawn();
            }
        }
    }

    //[ObserversRpc]
    private void SetText()
    {
        string[] colors = GameManager.Instance.scores.Keys.ToArray();

        foreach (string color in colors)
        {
            leaderboardText.text = leaderboardText.text + color + ": " + GameManager.Instance.scores[color] + "\n";

        }
    }
}

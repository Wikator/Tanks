using System.Linq;
using UnityEngine;
using TMPro;

public sealed class EndScreen : View
{
    [SerializeField]
    private TextMeshProUGUI leaderboardText;


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

    private void OnEnable()
    {
        SetText();
    }

    public void SetText()
    {
        string[] colors = GameMode.Instance.scores.Keys.ToArray();

        foreach (string color in colors)
        {
            leaderboardText.text += color + ": " + GameMode.Instance.scores[color] + "\n";
        }

        if (FindObjectOfType<StockBattleGameMode>())
        {
            PlayerNetworking winner = null;

            foreach (PlayerNetworking player in GameManager.Instance.players)
            {
                if (!FindObjectOfType<StockBattleGameMode>().defeatedPlayers.Contains(player))
                {
                    winner = player;
                    break;
                }
            }

            leaderboardText.text += "\n";

            leaderboardText.text += "Winner: " + winner.Color;
        }
    }
}

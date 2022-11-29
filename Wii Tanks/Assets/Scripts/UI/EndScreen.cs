using System.Linq;
using UnityEngine;
using TMPro;

public sealed class EndScreen : View
{
    [SerializeField]
    private TextMeshProUGUI leaderboardText;

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
                if (!StockBattleGameMode.defeatedPlayers.Contains(player))
                {
                    winner = player;
                    break;
                }
            }

            leaderboardText.text += "\n";

            leaderboardText.text += "Winner: " + winner.color;
        }
    }
}

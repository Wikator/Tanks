using System.Linq;
using UnityEngine;
using TMPro;

public sealed class EndScreen : View
{
    public static EndScreen Instance { get; private set; }


    [SerializeField]
    private TextMeshProUGUI leaderboardText;


    public override void Init()
    {
        base.Init();
        Instance = this;
    }

    public void UpdateScores()
    {
        string[] colors = GameMode.Instance.scores.Keys.ToArray();

        leaderboardText.text = "";

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

using System.Linq;
using TMPro;
using UnityEngine;

public sealed class EndScreen : View
{
    [SerializeField] private TextMeshProUGUI leaderboardText;

    public static EndScreen Instance { get; private set; }


    public override void Init()
    {
        base.Init();
        Instance = this;
    }

    public void UpdateScores()
    {
        var colors = GameMode.Instance.scores.Keys.ToArray();

        leaderboardText.text = "";

        foreach (var color in colors) leaderboardText.text += color + ": " + GameMode.Instance.scores[color] + "\n";

        if (FindObjectOfType<StockBattleGameMode>())
        {
            PlayerNetworking winner = null;

            foreach (PlayerNetworking player in GameManager.Instance.players)
                if (!StockBattleGameMode.defeatedPlayers.Contains(player))
                {
                    winner = player;
                    break;
                }

            leaderboardText.text += "\n";

            leaderboardText.text += "Winner: " + winner.color;
        }
    }
}
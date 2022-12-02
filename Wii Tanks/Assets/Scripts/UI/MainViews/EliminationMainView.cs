using UnityEngine;
using TMPro;

public sealed class EliminationMainView : MainView
{
    [SerializeField]
    private TextMeshProUGUI enemyScore;


    public override void UpdateScore(string color, int newScore)
    {
        base.UpdateScore(color, newScore);

        if (PlayerNetworking.Instance.color != color)
        {
            enemyScore.text = "Enemy score: " + newScore;
        }
    }
}

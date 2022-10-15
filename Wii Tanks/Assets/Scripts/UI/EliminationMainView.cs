using UnityEngine;
using TMPro;

public sealed class EliminationMainView : MainView
{
    [SerializeField]
    private TextMeshProUGUI enemyScore;


    protected override void Update()
    {
        base.Update();

        switch (PlayerNetworking.Instance.color)
        {
            case "Green":
                enemyScore.text = "Enemy score: " + GameMode.Instance.scores["Red"];
                break;
            case "Red":
                enemyScore.text = "Enemy score: " + GameMode.Instance.scores["Green"];
                break;
        }
    }
}

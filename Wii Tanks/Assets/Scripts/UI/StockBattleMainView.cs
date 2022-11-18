using System;
using TMPro;
using UnityEngine;

public class StockBattleMainView : MainView
{
    [SerializeField]
    private TextMeshProUGUI lifeRemainingText;

    private StockBattleGameMode stockBattleGameMode;


    void Start()
    {
        stockBattleGameMode = FindObjectOfType<StockBattleGameMode>();
    }

    private void Update()
    {
        if (IsClient)
        {
            lifeRemainingText.text = "Life: " + Convert.ToString(stockBattleGameMode.lifeRemaining[PlayerNetworking.Instance.Color]);
        }
    }
}

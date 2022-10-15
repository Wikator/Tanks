using UnityEngine;
using TMPro;

public abstract class MainView : View
{
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;


    //UI shown during the match

    protected virtual void Update()
    {
        PlayerNetworking player = PlayerNetworking.Instance;

        if (!player)
            return;

        scoreText.text = "Score: " + GameMode.Instance.scores[player.color];

        if (player.controlledPawn)
        {
            ammoCountText.text = "Ammo:" + player.controlledPawn.ammoCount;
        }
        else
        {
            ammoCountText.text = "Ammo:" + 0;
        }
    }
}

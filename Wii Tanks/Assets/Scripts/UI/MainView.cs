using UnityEngine;
using TMPro;

public sealed class MainView : View
{
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;

    private void Update()
    {
        PlayerNetworking player = PlayerNetworking.Instance;

        if (!player)
            return;

        scoreText.text = "Score: " + player.score;

        if (!player.controlledPawn)
            return;

        ammoCountText.text = "Ammo:" + player.controlledPawn.ammoCount;
    }
}

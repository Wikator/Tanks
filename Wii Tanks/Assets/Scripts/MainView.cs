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

        if (player == null || player.controlledPawn == null) return;

        scoreText.text = "Score: " + player.score;

        ammoCountText.text = "Ammo:" + player.controlledPawn.ammoCount;
    }
}

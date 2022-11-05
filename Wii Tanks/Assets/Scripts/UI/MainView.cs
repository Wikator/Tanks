using UnityEngine;
using TMPro;

public abstract class MainView : View
{
    public static MainView Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateAmmo(int newAmmoCount)
    {
        ammoCountText.text = "Ammo:" + newAmmoCount;
    }

    public virtual void UpdateScore(string color, int newScore)
    {
        if (PlayerNetworking.Instance.color == color)
        {
            scoreText.text = "Score: " + newScore;
        }
    }
}

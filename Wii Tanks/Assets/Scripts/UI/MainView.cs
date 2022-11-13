using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;

public abstract class MainView : View
{
    public static MainView Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;

    [SerializeField]
    private Image superBar;

    public float maxCharge = 1000;

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
        if (PlayerNetworking.Instance.Color == color)
        {
            scoreText.text = "Score: " + newScore;
        }
    }

    [Client]
    private void FixedUpdate()
    {
        superBar.fillAmount = (float)PlayerNetworking.Instance.superCharge / maxCharge;
        if(Mathf.Clamp((float)PlayerNetworking.Instance.superCharge, 0f, maxCharge) == maxCharge)
        {
            superBar.color = Color.green;
        }
        else
        {
            superBar.color = Color.red;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using FishNet.Connection;

public abstract class MainView : View
{
    public static MainView Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;

    [SerializeField]
    private Slider superBar;

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
        if (PlayerNetworking.Instance.color == color)
        {
            scoreText.text = "Score: " + newScore;
        }
    }

    [Client]
    private void FixedUpdate()
    {
        superBar.value = (float)PlayerNetworking.Instance.superCharge / maxCharge;

        if (Mathf.Clamp((float)PlayerNetworking.Instance.superCharge, 0f, maxCharge) == maxCharge)
        {
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
        }
        else
        {
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
        }
    }


    [TargetRpc]
    public void SetMaxCharge(NetworkConnection networkConnection, float maxCharge)
    {
        this.maxCharge = maxCharge;
    }
}

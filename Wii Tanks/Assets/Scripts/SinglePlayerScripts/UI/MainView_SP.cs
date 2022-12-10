using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainView_SP : MonoBehaviour
{
    public static MainView_SP Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI timeSurvivedText;

    [SerializeField]
    private TextMeshProUGUI ammoCountText;

    [SerializeField]
    private Slider superBar;

    private float timeSurvived = 0f;

    public float maxCharge = 1000;

    private void Awake()
    {
        Instance = this;
    }


    public void UpdateAmmo(int newAmmoCount)
    {
        ammoCountText.text = "Ammo:" + newAmmoCount;
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore;
    }

    private void FixedUpdate()
    {
        superBar.value = (float)Player.Instance.superCharge / maxCharge;

        if (Mathf.Clamp((float)Player.Instance.superCharge, 0f, maxCharge) == maxCharge)
        {
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
        }
        else
        {
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
        }

        timeSurvived += Time.deltaTime;

        int minutesSurvived = Mathf.FloorToInt(timeSurvived / 60);
        int secondsSurvived = Mathf.FloorToInt(timeSurvived % 60);

        timeSurvivedText.text = $"Time survived: {minutesSurvived:00}:{secondsSurvived:00}";
    }

    public void SetMaxCharge(float maxCharge)
    {
        this.maxCharge = maxCharge;
    }
}

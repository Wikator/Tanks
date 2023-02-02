using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainView_SP : View_SP
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

    [HideInInspector]
    public float TimeSurvived { get; private set; }

    public float maxCharge = 1000;

    protected virtual void Awake()
    {
        Instance = this;
        TimeSurvived = 0f;
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", Mathf.Max(TimeSurvived, PlayerPrefs.GetFloat(SceneManager.GetActiveScene().name + "HighScore")));
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

        TimeSurvived += Time.deltaTime;

        int minutesSurvived = Mathf.FloorToInt(TimeSurvived / 60);
        int secondsSurvived = Mathf.FloorToInt(TimeSurvived % 60);

        timeSurvivedText.text = $"Time survived: {minutesSurvived:00}:{secondsSurvived:00}";
    }

    public void SetMaxCharge(float maxCharge)
    {
        this.maxCharge = maxCharge;
    }
}

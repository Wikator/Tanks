using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainView_SP : View_SP
{
    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private TextMeshProUGUI timeSurvivedText;

    [SerializeField] private TextMeshProUGUI ammoCountText;

    [SerializeField] private Slider superBar;

    public float maxCharge = 1000;
    public static MainView_SP Instance { get; private set; }

    [HideInInspector] public float TimeSurvived { get; private set; }

    protected virtual void Awake()
    {
        Instance = this;
        TimeSurvived = 0f;
    }

    private void FixedUpdate()
    {
        superBar.value = (float)Player.Instance.superCharge / maxCharge;

        if (Mathf.Clamp((float)Player.Instance.superCharge, 0f, maxCharge) == maxCharge)
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
        else
            superBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;

        TimeSurvived += Time.deltaTime;

        var minutesSurvived = Mathf.FloorToInt(TimeSurvived / 60);
        var secondsSurvived = Mathf.FloorToInt(TimeSurvived % 60);

        timeSurvivedText.text = $"Time survived: {minutesSurvived:00}:{secondsSurvived:00}";
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore",
            Mathf.Max(TimeSurvived, PlayerPrefs.GetFloat(SceneManager.GetActiveScene().name + "HighScore")));
    }


    public void UpdateAmmo(int newAmmoCount)
    {
        ammoCountText.text = "Ammo:" + newAmmoCount;
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore;
    }

    public void SetMaxCharge(float maxCharge)
    {
        this.maxCharge = maxCharge;
    }
}
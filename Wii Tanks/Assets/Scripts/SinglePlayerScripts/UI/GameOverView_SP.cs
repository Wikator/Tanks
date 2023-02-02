using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverView_SP : View_SP
{
    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button mapSelectionButton;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        mapSelectionButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection_SP")); 
    }

    private void OnEnable()
    {
        int minutesSurvived = Mathf.FloorToInt(MainView_SP.Instance.TimeSurvived / 60);
        int secondsSurvived = Mathf.FloorToInt(MainView_SP.Instance.TimeSurvived % 60);

        scoreText.text = $"Your score: {minutesSurvived:00}:{secondsSurvived:00}";
    }
}

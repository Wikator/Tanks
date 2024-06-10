using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverView_SP : View_SP
{
    [SerializeField] private Button restartButton;

    [SerializeField] private Button mapSelectionButton;

    [SerializeField] private TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        var minutesSurvived = Mathf.FloorToInt(MainView_SP.Instance.TimeSurvived / 60);
        var secondsSurvived = Mathf.FloorToInt(MainView_SP.Instance.TimeSurvived % 60);

        scoreText.text = $"Your score: {minutesSurvived:00}:{secondsSurvived:00}";
    }

    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        mapSelectionButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection_SP"));
    }
}
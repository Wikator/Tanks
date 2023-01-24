using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;

public sealed class ArenaSelectionScene_SP : ArenaSelectionScene
{
    [SerializeField]
    private TextMeshProUGUI highScoreText;

    protected override void Start()
    {
        base.Start();

        int minutesSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("DeathmatchHighScore") / 60);
        int secondsSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("DeathmatchHighScore") % 60);

        highScoreText.text = $"High score: {minutesSurvived:00}:{secondsSurvived:00}";
    }


    protected override void OnSpacePressed(string arenaName)
    {
        SceneManager.LoadScene(arenaName);
    }
}

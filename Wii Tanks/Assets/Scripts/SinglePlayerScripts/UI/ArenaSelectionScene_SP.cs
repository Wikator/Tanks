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

        int minutesSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("Arena1_SPHighScore") / 60);
        int secondsSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("Arena1_SPHighScore") % 60);

        highScoreText.text = $"High score: {minutesSurvived:00}:{secondsSurvived:00}";
    }

    private void LateUpdate()
    {
        foreach (GameObject arena in allArenasArray)
        {
            if (allArenasDictionary[arena] == Vector3.zero)
            {
                int minutesSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat(arena.name + "HighScore") / 60);
                int secondsSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat(arena.name + "HighScore") % 60);

                highScoreText.text = $"High score: {minutesSurvived:00}:{secondsSurvived:00}";
            }
        }
    }

    protected override void OnSpacePressed(string arenaName)
    {
        SceneManager.LoadScene(arenaName);
    }
}

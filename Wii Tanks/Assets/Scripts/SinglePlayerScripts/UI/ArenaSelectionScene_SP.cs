using AbstractClasses;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SinglePlayerScripts.UI
{
    public sealed class ArenaSelectionScene_SP : ArenaSelectionScene
    {
        [SerializeField] private TextMeshProUGUI highScoreText;

        protected override void Start()
        {
            base.Start();

            var minutesSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("Arena1_SPHighScore") / 60);
            var secondsSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat("Arena1_SPHighScore") % 60);

            highScoreText.text = $"High score: {minutesSurvived:00}:{secondsSurvived:00}";
        }

        private void LateUpdate()
        {
            foreach (var arena in allArenasArray)
                if (allArenasDictionary[arena] == Vector3.zero)
                {
                    var minutesSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat(arena.name + "HighScore") / 60);
                    var secondsSurvived = Mathf.FloorToInt(PlayerPrefs.GetFloat(arena.name + "HighScore") % 60);

                    highScoreText.text = $"High score: {minutesSurvived:00}:{secondsSurvived:00}";
                }
        }

        protected override void OnSpacePressed(string arenaName)
        {
            if (rotating)
                return;

            SceneManager.LoadScene(arenaName);
        }
    }
}
using TMPro;
using UnityEngine;

public sealed class DeathmatchMainView : MainView
{
    [SerializeField]
    private TextMeshProUGUI timeRemainingText;

    private DeathmatchGameMode deathmatchGameMode;


    private void Start()
    {
        deathmatchGameMode = FindObjectOfType<DeathmatchGameMode>();
    }

    private void Update()
    {
        if (IsClient)
        {
            float minutesRemaining = Mathf.FloorToInt(deathmatchGameMode.time / 60);
            float secondsRemaining = Mathf.FloorToInt(deathmatchGameMode.time % 60);

            timeRemainingText.text = string.Format("Time remaining\n{0:00}:{1:00}", minutesRemaining, secondsRemaining);
        }
    }
}

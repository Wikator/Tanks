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

    protected override void Update()
    {
        base.Update();

        //if (IsServer && GameManager.Instance.gameInProgress)
        //{

            //if (deathmatchGameMode.time > 0)
            //{
                deathmatchGameMode.time -= Time.deltaTime;
            //}
        //}

        if (IsClient)
        {
            float minutesRemaining = Mathf.FloorToInt(deathmatchGameMode.time / 60);
            float secondsRemaining = Mathf.FloorToInt(deathmatchGameMode.time % 60);

            timeRemainingText.text = string.Format("Time remaining\n{0:00}:{1:00}", minutesRemaining, secondsRemaining);
        }
    }
}

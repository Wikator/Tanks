using UnityEngine;
using TMPro;

public class CampaignMainView : MainView_SP
{
    public static CampaignMainView Instance1 { get; private set; }

    [SerializeField]
    private TextMeshProUGUI levelText;

    protected override void Awake()
    {
        base.Awake();
        Instance1 = this;
    }

    public void UpdateLevelText(int newLevel)
    {
        levelText.text = $"Level: {newLevel}";
    }
}

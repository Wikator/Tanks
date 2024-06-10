using TMPro;
using UnityEngine;

public class CampaignMainView : MainView_SP
{
    [SerializeField] private TextMeshProUGUI levelText;

    public static CampaignMainView Instance1 { get; private set; }

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
using System.Linq;
using UnityEngine;
using TMPro;
using FishNet.Object;

public sealed class EndScreen : NetworkBehaviour
{
    [SerializeField]
    private TextMeshProUGUI leaderboardText;

    private void Start()
    {
        SetText();
    }

    //[ObserversRpc]
    private void SetText()
    {
        string[] colors = GameMode.Instance.scores.Keys.ToArray();

        foreach (string color in colors)
        {
            leaderboardText.text = leaderboardText.text + color + ": " + GameMode.Instance.scores[color] + "\n";

        }
    }
}

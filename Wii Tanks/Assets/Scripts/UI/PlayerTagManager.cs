using UnityEngine;

public class PlayerTagManager : MonoBehaviour
{
    [SerializeField]
    private LobbyPlayerTag[] playerTags = new LobbyPlayerTag[6];

    private void LateUpdate()
    {
        if (!GameManager.Instance)
            return;


        for (int i = 0; i < playerTags.Length; i++)
        {
            if (GameManager.Instance.players.Count > i)
            {
                playerTags[i].gameObject.SetActive(true);
                playerTags[i].steamID = GameManager.Instance.players[i].playerSteamID;
                playerTags[i].SetPlayerValues(GameManager.Instance.players[i]);
            }
            else
            {
                playerTags[i].gameObject.SetActive(false);
            }
        }
    }
}

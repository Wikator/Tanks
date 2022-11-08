using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class LobbyView : View
{
    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText, playersReadyCountText, chosenColorText, chosenTankTypeText;

    [SerializeField]
    protected Button toggleReadyButton;

    [SerializeField]
    protected Button startGameButton;

    [SerializeField]
    private GameObject viewContent;

    private List<LobbyPlayerTag> playerTags = new();

    


    //Each player will have to choose a color and tank type, thanks to the subclasses EliminationLobbyView and DeathmatchLobbyView
    //Only when both are chosen toggleReadyButton will become interactable
    //Once all players are ready, startGameButton will become interactable

    private void LateUpdate()
    {
        if (!Initialized)
            return;

        if (PlayerNetworking.Instance.color == "None" || PlayerNetworking.Instance.tankType == "None")
        {
            toggleReadyButton.interactable = false;
        }
        else
        {
            toggleReadyButton.interactable = true;
        }

        toggleReadyButtonText.color = PlayerNetworking.Instance.isReady ? Color.green : Color.red;

        if (GameManager.Instance.canStart && IsHost)
        {
            startGameButton.interactable = true;
        }
        else
        {
            startGameButton.interactable = false;
        }
        playersReadyCountText.text = "Players ready: " + Convert.ToString(GameManager.Instance.NumberOfReadyPlayers()) + "/" + Convert.ToString(GameManager.Instance.players.Count);
        chosenColorText.text = "Chosen color: " + PlayerNetworking.Instance.color;
        chosenTankTypeText.text = "Chosen tank type: " + PlayerNetworking.Instance.tankType;

        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        if (playerTags.Count < GameManager.Instance.players.Count)
        {
            Debug.Log("test");
            foreach (PlayerNetworking player in GameManager.Instance.players)
            {
                bool tagFound = false;
                foreach (LobbyPlayerTag tag in playerTags)
                {
                    if (tag.playerNameText.text == player.playerUsername)
                    {
                        tagFound = true;
                        break;
                    }
                }
                if (!tagFound)
                {
                    GameObject newPlayerTag = Instantiate(Addressables.LoadAssetAsync<GameObject>("PlayerTag").WaitForCompletion());
                    LobbyPlayerTag newPlayerTagScript = newPlayerTag.GetComponent<LobbyPlayerTag>();

                    newPlayerTagScript.SetPlayerValues(player);

                    newPlayerTag.transform.SetParent(viewContent.transform);
                    newPlayerTag.transform.localEulerAngles = Vector3.one;

                    playerTags.Add(newPlayerTagScript);
                }
                
            }
        }

        if (playerTags.Count > GameManager.Instance.players.Count)
        {
            foreach (LobbyPlayerTag tag in playerTags)
            {
                bool playerFound = false;
                foreach (PlayerNetworking player in GameManager.Instance.players)
                {
                    if (tag.playerNameText.text == player.playerUsername)
                    {
                        playerFound = true;
                        break;
                    }
                }
                if (!playerFound)
                {
                    playerTags.Remove(tag);
                    Destroy(tag);
                }
                
            }
        }

        if (playerTags.Count == GameManager.Instance.players.Count)
        {

        }
    }
}

using FishNet;
using UnityEngine;
using UnityEngine.UI;

public sealed class Menu : MonoBehaviour
{
    private SteamLobby steamLobby;

    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private Button connectButton;


    //Start-up screen
    //User will have a choice to either host a server or connect as a client
    //With a PlayFlow dedicated server active, hostButton should be disabled

    private void Start()
    {
        steamLobby = FindObjectOfType<SteamLobby>();

        Debug.Log(steamLobby);

        hostButton.onClick.AddListener(steamLobby.HostLobby);

        connectButton.onClick.AddListener(steamLobby.JoinLobby);
    }

    private void OnDestroy()
    {
        hostButton.onClick.RemoveAllListeners();
    }
}

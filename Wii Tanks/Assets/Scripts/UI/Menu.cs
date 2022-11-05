using FishNet;
using UnityEngine;
using UnityEngine.UI;

public sealed class Menu : MonoBehaviour
{
    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private Button connectButton;


    //Start-up screen
    //User will have a choice to either host a server or connect as a client
    //With a PlayFlow dedicated server active, hostButton should be disabled

    private void Start()
    {
        hostButton.onClick.AddListener(() => InstanceFinder.ServerManager.StartConnection());

        //hostButton.onClick.AddListener(() => InstanceFinder.ClientManager.StartConnection());

        connectButton.onClick.AddListener(() => InstanceFinder.ClientManager.StartConnection());
    }
}

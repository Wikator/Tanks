using FishNet;
using UnityEngine;

public sealed class ArenaSelectionScene_MP : ArenaSelectionScene
{
    [SerializeField]
    private bool testLocally;
    
    protected override void OnSpacePressed(string arenaName)
    {
        FindObjectOfType<DefaultScene>().SetOnlineScene(arenaName);

        if (testLocally)
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
        else
        {
            FindObjectOfType<SteamLobby>().HostLobby();
        }
    }
}

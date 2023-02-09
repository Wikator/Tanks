using FishNet;
using UnityEngine;

public sealed class ArenaSelectionScene_MP : ArenaSelectionScene
{
    [SerializeField]
    private bool testLocally;
    
    protected override void OnSpacePressed(string arenaName)
    {
        if (rotating)
            return;

        FindObjectOfType<DefaultScene>().SetOnlineScene(arenaName + "_MP");

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

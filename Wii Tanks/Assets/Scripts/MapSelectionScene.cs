using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;

public sealed class MapSelectionScene : NetworkBehaviour
{
    public static MapSelectionScene Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }


    [ServerRpc(RequireOwnership = false)]
    public void LoadScene(string sceneName)
    {
        List<NetworkObject> movedObjects = new();

        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            movedObjects.Add(player.gameObject.GetComponent<NetworkObject>());
        }

        movedObjects.Add(GameManager.Instance.gameObject.GetComponent<NetworkObject>());

        LoadOptions loadOptions = new()
        {
            AutomaticallyUnload = true,
        };

        SceneLoadData sld = new(sceneName)
        {
            MovedNetworkObjects = movedObjects.ToArray(),
            ReplaceScenes = ReplaceOption.All,
            Options = loadOptions
        };

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
}


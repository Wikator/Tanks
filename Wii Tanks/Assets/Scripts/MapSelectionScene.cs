using FishNet;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class MapSelectionScene : NetworkBehaviour
{

    public static MapSelectionScene Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        //InstanceFinder.ServerManager.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>("GameManager").WaitForCompletion()));
    }


    [ServerRpc(RequireOwnership = false)]
    [Server]
    public void LoadScene(string sceneName)
    {
        //if (!NetworkManager.IsServer)
            //return;

        if (!PlayerNetworking.Instance.gameObject.GetComponent<NetworkObject>().Owner.IsActive)
            return;

        List<NetworkObject> movedObjects = new List<NetworkObject>();

        foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            movedObjects.Add(player.gameObject.GetComponent<NetworkObject>());
        }

        movedObjects.Add(GameManager.Instance.gameObject.GetComponent<NetworkObject>());
        //movedObjects.Add(PlayerNetworking.Instance.gameObject.GetComponent<NetworkObject>());

        LoadOptions loadOptions = new LoadOptions
        {
            AutomaticallyUnload = true,
        };

        Debug.Log(movedObjects[0]);

        SceneLoadData sld = new(sceneName)
        {
            MovedNetworkObjects = movedObjects.ToArray(),
            ReplaceScenes = ReplaceOption.All,
            Options = loadOptions
        };

       

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    [ServerRpc(RequireOwnership = false)]
    [Server]
    public void UnloadScene()
    {
        if (!InstanceFinder.NetworkManager.IsServer)
            return;

        UnloadOptions unloadOptions = new UnloadOptions()
        {
            Mode = (true) ? UnloadOptions.ServerUnloadMode.UnloadUnused : UnloadOptions.ServerUnloadMode.KeepUnused
        };

        SceneUnloadData sud = new SceneUnloadData("MapSelection");
        sud.Options = unloadOptions;

        InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
    }
}


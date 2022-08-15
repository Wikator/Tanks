using FishNet;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class MapSelectionScene : MonoBehaviour
{

    public static MapSelectionScene Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        //InstanceFinder.ServerManager.Spawn(Instantiate(Addressables.LoadAssetAsync<GameObject>("GameManager").WaitForCompletion()));
    }


    [Server(Logging = LoggingType.Off)]
    public void LoadScene(string sceneName)
    {
        SceneLoadData sld = new(sceneName);
        sld.MovedNetworkObjects = new NetworkObject[] { GameManager.Instance.gameObject.GetComponent<NetworkObject>() };
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
}

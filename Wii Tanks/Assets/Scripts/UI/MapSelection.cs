using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapSelection : NetworkBehaviour
{
    [SerializeField]
    private List<Button> mapButtons = new();

    private void Start()
    {
        foreach (Button button in mapButtons)
        {
            button.onClick.AddListener(() => LoadScene(button.name));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadScene(string sceneName)
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

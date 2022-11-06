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

    [SerializeField]
    private Button inviteButton;

    //Map selection screen
    //Each player has an option to choose a map

    private void Start()
    {
        inviteButton.onClick.AddListener(() => Steamworks.SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.LobbyID));

        foreach (Button button in mapButtons)
        {
            button.onClick.AddListener(() => LoadScene(button.name));
        }
    }

    //Once any players chooses a map, each user loads an appriopriate screen
    //Only one instance of a map is allowed at the moment, so each player that connects AFTER the map is chosen will automatically load the map

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

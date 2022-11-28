using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArenaSelectionScene : NetworkBehaviour
{
    [SerializeField]
    private GameObject[] allArenasArray = new GameObject[4];

    private readonly Dictionary<GameObject, Vector3> allArenasDictionary = new();

    private bool rotating;

    private readonly Vector3[] arenaPositions = new Vector3[5];

    [SerializeField]
    private Button inviteButton;

    [SerializeField]
    private float rotateSpeed;


    private void Start()
    {
        inviteButton.onClick.AddListener(() => Steamworks.SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.LobbyID));

        rotating = false;

        arenaPositions[0] = new Vector3(-230, -70, 140);
        arenaPositions[1] = new Vector3(-115, -35, 70);
        arenaPositions[2] = Vector3.zero;
        arenaPositions[3] = new Vector3(115, -35, 70);
        arenaPositions[4] = new Vector3(230, -70, 140);

        for (int i = 0; i < 4; i++)
        {
            allArenasDictionary[allArenasArray[i]] = allArenasArray[i].transform.position;
        }
    }



    private void Update()
    {
        if (rotating || !InstanceFinder.IsHost)
            return;


        if (Input.GetKeyDown(KeyCode.D))
        {
            rotating = true;

            for (int i = 1; i < 4; i++)
            {
                foreach (GameObject arena in allArenasArray)
                {
                    if (arena.transform.position == arenaPositions[i])
                    {
                        allArenasDictionary[arena] = arenaPositions[i+1];
                        break;
                    }
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[0] || arena.transform.position == arenaPositions[4])
                {

                    arena.transform.position = arenaPositions[0];
                    allArenasDictionary[arena] = arenaPositions[1];
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            rotating = true;

            for (int i = 3; i > 0; i--)
            {
                foreach (GameObject arena in allArenasArray)
                {
                    if (arena.transform.position == arenaPositions[i])
                    {
                        allArenasDictionary[arena] = arenaPositions[i-1];
                        break;
                    }
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[0] || arena.transform.position == arenaPositions[4])
                {
                    arena.transform.position = arenaPositions[4];
                    allArenasDictionary[arena] = arenaPositions[3];
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == Vector3.zero)
                {
                    LoadScene(arena.name);
                }
            }
        }
    }


    private void FixedUpdate()
    {
        if (rotating)
        {
            foreach (GameObject arena in allArenasArray)
            {
                arena.transform.position = Vector3.MoveTowards(arena.transform.position, allArenasDictionary[arena], rotateSpeed);

                if (arena.transform.position == allArenasDictionary[arena])
                {
                    arena.transform.position = allArenasDictionary[arena];

                    rotating = false;
                }
            }
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

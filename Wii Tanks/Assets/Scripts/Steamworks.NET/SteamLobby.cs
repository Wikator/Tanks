using FishNet;
using FishNet.Object;
using Steamworks;
using UnityEngine;

public class SteamLobby : MonoBehaviour
{
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEnter;

    private const string HOST_ADDRESS_KEY = "HostAddress";

    public static CSteamID LobbyID { get; private set; }

    private void Awake()
    {
        if (!SteamManager.Initialized)
            return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    private void OnDestroy()
    {
        lobbyCreated.Unregister();
        gameLobbyJoinRequested.Unregister();
        lobbyCreated.Unregister();
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 6);
    }

    public void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(LobbyID);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning("Lobby creation failed!");
            return;
        }

        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        InstanceFinder.ServerManager.StartConnection();

        SteamMatchmaking.SetLobbyData(LobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());

        var username = SteamFriends.GetPersonaName();
        SteamMatchmaking.SetLobbyData(LobbyID, "name", $"{username}'s lobby");

        Debug.Log("Hosted Lobby Successfully");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(LobbyID, HOST_ADDRESS_KEY);

        InstanceFinder.ClientManager.StartConnection(hostAddress);

        Debug.Log("Connected to Lobby Successfully");
    }
}

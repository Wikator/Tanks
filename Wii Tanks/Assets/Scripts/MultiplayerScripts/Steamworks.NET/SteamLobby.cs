using FishNet;
using Steamworks;
using UnityEngine;

public class SteamLobby : MonoBehaviour
{
    private const string HOST_ADDRESS_KEY = "HostAddress";
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEnter;

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

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        InstanceFinder.ServerManager.StartConnection();

        SteamMatchmaking.SetLobbyData(LobbyID, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());

        var username = SteamFriends.GetPersonaName();
        SteamMatchmaking.SetLobbyData(LobbyID, "name", $"{username}'s lobby");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        var hostAddress = SteamMatchmaking.GetLobbyData(LobbyID, HOST_ADDRESS_KEY);

        InstanceFinder.ClientManager.StartConnection(hostAddress);
    }
}
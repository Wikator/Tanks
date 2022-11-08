using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;
using FishNet.Object.Synchronizing;

public class LobbyPlayerTag : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;

    private bool avatarReceived;

    public RawImage playerIcon;


    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    //[HideInInspector]
    public ulong steamID;


    private void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID.m_SteamID == steamID)
        {
            playerIcon.texture = GetSteamImageAsTexture(callback.m_iImage);
        }
    }


    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);

        if (isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height* 4));

            if (isValid)
            {
                texture = new Texture2D ((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        avatarReceived = true;
        return texture;
    }

    private void GetPlayerIcon(PlayerNetworking player)
    {
        int ImageID = SteamFriends.GetLargeFriendAvatar((CSteamID)player.playerSteamID);

        if (ImageID == -1)
            return;

        playerIcon.texture = GetSteamImageAsTexture(ImageID);
    }

    public void SetPlayerValues(PlayerNetworking player)
    {
        playerNameText.text = player.playerUsername;

        if (!avatarReceived)
            GetPlayerIcon(player);
    }
}

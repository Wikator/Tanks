using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public static class Settings
{
    private static DepthOfField depthOfField;

    private static string chosenBackground;
    
    private static bool showPlayerNames;


    [HideInInspector]
    public static bool ShowPlayerNames
    {
        get
        {
            return showPlayerNames;
        }
        set
        {
            showPlayerNames = value;
        }
    }


    [HideInInspector]
    public static string ChosenBackground
    {
        get
        {
            return chosenBackground;
        }

        set
        {
            /*if (!depthOfField)
            {
                VolumeProfile volumeProfile = Addressables.LoadAssetAsync<VolumeProfile>("Profile").WaitForCompletion();
                for (int i = 0; i < volumeProfile.components.Count; i++)
                {
                    if (volumeProfile.components[i].name == "DepthOfField")
                    {
                        depthOfField = (DepthOfField)volumeProfile.components[i];
                    }
                }
            }*/

            chosenBackground = value;

            Color color = GameObject.Find("Background").GetComponent<MeshRenderer>().material.color;
            GameObject.Find("Background").GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(chosenBackground).WaitForCompletion();
            GameObject.Find("Background").GetComponent<MeshRenderer>().material.color = color;

            /*switch (chosenBackground)
            {
                case "Background1":
                    depthOfField.active = true;
                    break;
                case "Background2":
                    depthOfField.active = false;
                    break;
            }*/

            PlayerPrefs.SetString("Background", ChosenBackground);
        }
    }
}

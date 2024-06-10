using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;

public static class Settings
{
    private static DepthOfField depthOfField;

    private static string chosenBackground;

    private static string camera;


    public static bool ShowPlayerNames { get; set; }

    public static string Camera
    {
        get => camera;
        set
        {
            camera = value;
            PlayerPrefs.SetString("Camera", Camera);
        }
    }


    public static string ChosenBackground
    {
        get => chosenBackground;

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

            var color = GameObject.Find("Background").GetComponent<MeshRenderer>().material.color;
            GameObject.Find("Background").GetComponent<MeshRenderer>().material =
                Addressables.LoadAssetAsync<Material>(chosenBackground).WaitForCompletion();
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
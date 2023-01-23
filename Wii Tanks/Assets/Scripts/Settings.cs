using UnityEngine;
using UnityEngine.AddressableAssets;

public static class Settings
{
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
            chosenBackground = value;

            Color color = GameObject.Find("Background").GetComponent<MeshRenderer>().material.color;
            GameObject.Find("Background").GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(chosenBackground).WaitForCompletion();
            GameObject.Find("Background").GetComponent<MeshRenderer>().material.color = color;
        }
    }
}

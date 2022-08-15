using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelection : MonoBehaviour
{
    public static MapSelection Instance { get; private set; }

    [SerializeField]
    private List<Button> mapButtons = new();

    private void Start()
    {
        Instance = this;

        foreach (Button button in mapButtons)
        {
            button.onClick.AddListener(() => GameManager.Instance.MapChosen(button));
        }
    }


    public void LoadScene(NetworkObject nob, string sceneName)
    {
        Debug.Log(1);
        if (!nob.Owner.IsActive)
            return;
        Debug.Log(2);
        SceneLoadData sld = new(sceneName);
        sld.MovedNetworkObjects = new NetworkObject[] { nob };
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
}

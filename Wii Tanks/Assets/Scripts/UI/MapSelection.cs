using FishNet;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelection : MonoBehaviour
{
    [SerializeField]
    private List<Button> mapButtons = new();

    private void Start()
    {
        foreach (Button button in mapButtons)
        {
            button.onClick.AddListener(() => MapSelectionScene.Instance.LoadScene(button.name));
        }
    }
}

using UnityEngine;
using UnityEngine.AddressableAssets;

public class BackgroundScript : MonoBehaviour
{
    [SerializeField] [ColorUsage(hdr: true, showAlpha: true)]
    private Color color;

    private void Awake()
    {
        gameObject.GetComponent<MeshRenderer>().material =
            Addressables.LoadAssetAsync<Material>(Settings.ChosenBackground).WaitForCompletion();

        gameObject.GetComponent<MeshRenderer>().material.color = color;

        Destroy(this);
    }
}
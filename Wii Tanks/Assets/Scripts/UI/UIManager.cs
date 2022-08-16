using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private View[] views;


    private void Awake()
    {
        Instance = this;

        /*foreach (PlayerNetworking player in GameManager.Instance.players)
        {
            player.OnSceneLoaded();
        }*/
    }

    public void Init()
    {
        foreach (View view in views)
        {
            view.Init();
        }
    }

    public void Show<T>() where T : View
    {
        foreach (View view in views)
        {
            view.gameObject.SetActive(view is T);
        }
    }
}

using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public sealed class UIManager : NetworkBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private View[] views;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        SetUpUI(GameManager.Instance.gameInProgress, GameManager.Instance.gameMode);
    }


    [ObserversRpc(BufferLast = true)]
    public void SetUpUI(bool gameInProgress, string gameMode)
    {
        Init();

        if (gameInProgress)
        {
            Show<MainView>();
        }
        else
        {
            switch (gameMode)
            {
                case "Deathmatch":
                    Show<DeathmatchLobbyView>();
                    break;
                case "Elimination":
                    Show<EliminationLobbyView>();
                    break;
                default:
                    Show<GameModesView>();
                    break;
            }
        }
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

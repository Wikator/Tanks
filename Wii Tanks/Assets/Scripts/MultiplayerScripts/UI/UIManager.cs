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


    //A function that refreshes the UI when needed
    //An ObserversRpc is called, so the UI is changed for each player connected to the server

    [ObserversRpc(BufferLast = true)]
    public void SetUpAllUI(bool gameInProgress, string gameMode)
    {
        Init();

        if (gameInProgress)
        {
            switch (gameMode)
            {
                case "Mayhem":
                case "Deathmatch":
                    Show<DeathmatchMainView>();
                    break;
                case "StockBattle":
                    Show<StockBattleMainView>();
                    break;
                case "Takedown":
                case "Elimination":
                    Show<EliminationMainView>();
                    break;
            }
        }
        else
        {
            switch (gameMode)
            {
                case "StockBattle":
                case "Deathmatch":
                    Show<DeathmatchLobbyView>();
                    break;
                case "Mayhem":
                    Show<MayhemLobbyView>();
                    break;
                case "Takedown":
                case "Elimination":
                    Show<EliminationLobbyView>();
                    break;
                case "GameFinished":
                    Show<EndScreen>();
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

    //Each UI is a subclass of the View class, which allows for easy cycling between different UIs

    public void Show<T>() where T : View
    {
        foreach (View view in views)
        {
            view.gameObject.SetActive(view is T);
        }
    }
}

using UnityEngine;
using System;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private View[] views;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        try
        {
            PlayerNetworking.Instance.OnSceneLoaded();
        }
        catch (NullReferenceException)
        {
            return;
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

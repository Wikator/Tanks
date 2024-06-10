using UnityEngine;

public abstract class View_SP : MonoBehaviour
{
    public bool Initialized { get; private set; }

    public virtual void Init()
    {
        Initialized = true;
    }
}
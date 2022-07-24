using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Spawn : NetworkBehaviour
{
    [SyncVar]
    public bool isOccupied = false;

    private Collider other;

    private void OnTriggerStay(Collider other)
    {
        isOccupied = true;
        this.other = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isOccupied)
        {
            isOccupied = false;
        }
    }

    private void Update()
    {
        if (isOccupied && !other)
        {
            isOccupied = false;
        }
    }
}

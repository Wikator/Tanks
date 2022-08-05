using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Spawn : NetworkBehaviour
{
    [SyncVar]
    public bool isOccupied = false;


    private void FixedUpdate()
    {
        if (isOccupied)
        {
            isOccupied = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        isOccupied = true;
    }
}

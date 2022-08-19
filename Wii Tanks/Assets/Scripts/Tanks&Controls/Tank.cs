using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class Tank : NetworkBehaviour
{
    private struct MoveData
    {
        public float MoveAxis;
        public float RotateAxis;
        public Vector3 TurretLookDirection;

        public MoveData(float moveAxis, float rotateAxis, Vector3 turretLookDirection)
        {
            MoveAxis = moveAxis;
            RotateAxis = rotateAxis;
            TurretLookDirection = turretLookDirection;
        }
    }

    private struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion TankRotation;
        public Quaternion TurretRotation;

        public ReconcileData(Vector3 tankPosition, Quaternion tankRotation, Quaternion turretRotation)
        {
            Position = tankPosition;
            TankRotation = tankRotation;
            TurretRotation = turretRotation;
        }
    }


    [SerializeField]
    private float moveSpeed, rotateSpeed;

    [SerializeField]
    protected int maxAmmo;

    [SerializeField]
    private string bulletType;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty;

    [HideInInspector]
    protected GameObject bullet, pointer;

    [SyncVar, HideInInspector]
    public int ammoCount;

    [SyncVar, HideInInspector]
    public PlayerNetworking controllingPlayer;


    private float moveAxis;
    private float rotateAxis;

    private bool isSubscribed = false;

    private GameMode gameModeManager;
    private CharacterController controller;
    private Transform explosionEmpty;
    private GameObject explosion, turret;
    private Camera cam;

    private LayerMask raycastLayer;


    public override void OnStartClient()
    {
        base.OnStartClient();
        raycastLayer = (1 << 9);
        cam = Camera.main;
        ammoCount = maxAmmo;
        controller = GetComponent<CharacterController>();
        gameModeManager = FindObjectOfType<GameMode>();
        turret = transform.GetChild(0).gameObject;
        bulletSpawn = turret.transform.GetChild(0).transform;
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        ChangeColours(controllingPlayer.color);
        SubscribeToTimeManager(true);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).gameObject;
        ChangeColours(controllingPlayer.color);
        SubscribeToTimeManager(true);
    }

    private void SubscribeToTimeManager(bool subscribe)
    {
        if (TimeManager == null || subscribe == isSubscribed) 
            return;

        isSubscribed = subscribe;

        if (subscribe)
        {
            TimeManager.OnTick += TimeManager_OnTick;
        }
        else
        {
            TimeManager.OnTick -= TimeManager_OnTick;
        }
    }

    private void TimeManager_OnTick()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            Reconciliation(default, false);
            GatherInputs(out MoveData data);
            Move(data, false);
        }

        if (IsServer)
        {
            Move(default, true);
            ReconcileData rd = new(transform.position, transform.rotation, turret.transform.rotation);
            Reconciliation(rd, true);
        }
    }

    public void GameOver()
    {
        //Destroy(pointer);
        controllingPlayer.controlledPawn = null;
        gameModeManager.OnKilled(controllingPlayer);
        Spawn(Instantiate(explosion, transform.position, transform.rotation, explosionEmpty));
        Despawn();
    }

    public override void OnDespawnServer(NetworkConnection connection)
    {
        base.OnDespawnServer(connection);
        SubscribeToTimeManager(false);
    }

    private void OnDestroy()
    {
        if (TimeManager != null)
            SubscribeToTimeManager(false);
    }

    private void GatherInputs(out MoveData data)
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        data = new MoveData(moveAxis, rotateAxis, hit.point);
    }

    public void ChangeColours (string color)
    {
        gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        bullet = Addressables.LoadAssetAsync<GameObject>(color + bulletType + "Bullet").WaitForCompletion();
        explosion = Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
    }

    protected abstract void Fire();

    protected IEnumerator AddAmmo(float timeToReload, float timeToAddAmmo)
    {
        yield return new WaitForSeconds(timeToReload);
        ammoCount++;

        if (ammoCount != maxAmmo)
            StartCoroutine(AddAmmo(timeToAddAmmo, timeToAddAmmo));
    }



    [Replicate]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Move(MoveData data, bool asServer, bool replaying = false)
    {
        if (!IsSpawned)
            return;

        controller.Move((float)TimeManager.TickDelta * data.MoveAxis * moveSpeed * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * rotateSpeed * (float)TimeManager.TickDelta, 0f));

        turret.transform.LookAt(data.TurretLookDirection, Vector3.up);
        turret.transform.localEulerAngles = new Vector3(0, turret.transform.localEulerAngles.y, 0);

        //pointer.transform.position = data.TurretLookDirection;
    }

    [Reconcile]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Reconciliation(ReconcileData data, bool asServer)
    {
        transform.SetPositionAndRotation(data.Position, data.TankRotation);

        turret.transform.rotation = data.TurretRotation;
    }
}


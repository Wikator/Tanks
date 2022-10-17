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

    [System.Serializable]
    protected struct TankStats
    {
        public float moveSpeed;
        public float rotateSpeed;
        public float timeToReload;
        public float timeToAddAmmo;
        public int maxAmmo;
    }

    public bool canUseSpecialMove = true;

    [SerializeField]
    protected TankStats stats;

    [HideInInspector]
    protected Transform bulletSpawn, muzzleFlashEmpty, bulletEmpty;

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
    private Transform explosionEmpty, turret;
    private GameObject explosion;
    protected GameObject muzzleFlash;
    private Camera cam;

    private LayerMask raycastLayer;

    protected Coroutine routine;


    private void Start()
    {
        raycastLayer = (1 << 9);
        cam = Camera.main;
        ammoCount = stats.maxAmmo;
        controller = GetComponent<CharacterController>();
        gameModeManager = FindObjectOfType<GameMode>();
        turret = transform.GetChild(0);
        bulletSpawn = turret.GetChild(0);
        muzzleFlashEmpty = turret.GetChild(1);
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        ChangeColours(controllingPlayer.color);
        SubscribeToTimeManager(true);
    }


    //Firing weapons need to be server authoritive so there is no delay, so this will need to be changed

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0) && ammoCount > 0)
        {
            Fire();

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            ammoCount--;
            routine = StartCoroutine(AddAmmo(stats.timeToReload));
        }

        if (Input.GetMouseButtonDown(1) && canUseSpecialMove)
            SpecialMove();
    }


    //ServerRpc means that the bullet will spawn on the server, so each other client can see it

    [ServerRpc]
    protected virtual void Fire()
    {
        GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        Spawn(bulletInstance);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;

        GameObject flashInstance = Instantiate(muzzleFlash, muzzleFlashEmpty.position, muzzleFlashEmpty.rotation, muzzleFlashEmpty);
        Spawn(flashInstance);
    }

    protected abstract void SpecialMove();


    protected IEnumerator AddAmmo(float time)
    {
        yield return new WaitForSeconds(time);
        ammoCount++;

        if (ammoCount != stats.maxAmmo)
        {
            routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
        }
        else
        {
            routine = null;
        }   
    }


    //Tank needs to subscribe to TimeManager, so its movement is server authoritive

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


    //Tank will gather input on the client, and then move on the server

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
            ReconcileData rd = new(transform.position, transform.rotation, turret.rotation);
            Reconciliation(rd, true);
        }
    }

    [Server]
    public void GameOver()
    {
        controllingPlayer.controlledPawn = null;
        gameModeManager.OnKilled(controllingPlayer);
        Spawn(Instantiate(explosion, transform.position, transform.rotation, explosionEmpty));
        Despawn();
    }


    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (TimeManager)
        {
            SubscribeToTimeManager(false);
        }
    }

    [Client]
    private void GatherInputs(out MoveData data)
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        data = new MoveData(moveAxis, rotateAxis, hit.point);
    }

    public virtual void ChangeColours (string color)
    {
        gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        turret.gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        explosion = Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
        muzzleFlash = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlash").WaitForCompletion();
    }


    [Replicate]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Move(MoveData data, bool asServer, bool replaying = false)
    {
        if (!IsSpawned)
            return;

        controller.Move((float)TimeManager.TickDelta * data.MoveAxis * stats.moveSpeed * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * stats.rotateSpeed * (float)TimeManager.TickDelta, 0f));

        turret.LookAt(data.TurretLookDirection, Vector3.up);
        turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
    }

    [Reconcile]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Reconciliation(ReconcileData data, bool asServer)
    {
        transform.SetPositionAndRotation(data.Position, data.TankRotation);

        turret.rotation = data.TurretRotation;
    }
}


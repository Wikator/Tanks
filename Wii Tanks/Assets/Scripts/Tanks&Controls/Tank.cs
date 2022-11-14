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
        public bool FireWeapon;
        public bool UseSuper;
        public Vector3 TurretLookDirection;

        public MoveData(float moveAxis, float rotateAxis, bool fireWeapon, bool useSuper, Vector3 turretLookDirection)
        {
            MoveAxis = moveAxis;
            RotateAxis = rotateAxis;
            FireWeapon = fireWeapon;
            UseSuper = useSuper;
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
        public int requiredSuperCharge;
        public int onKillSuperCharge;
    }

    [SyncVar(ReadPermissions = ReadPermission.OwnerOnly)]
    private bool canUseSuper = true;

    [SerializeField]
    private bool animateShader;

    [SerializeField]
    private TextMesh namePlate;

    [SerializeField]
    protected bool poolBullets;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    [HideInInspector]
    protected GameObject bullet, pointer;

    [SyncVar(OnChange = nameof(OnAmmoChange), ReadPermissions = ReadPermission.OwnerOnly)]
    protected int ammoCount;

    [SyncVar, HideInInspector]
    public PlayerNetworking controllingPlayer;


    private float moveAxis;
    private float rotateAxis;

    private bool isSubscribed = false;
    private bool firingQueued = false, superQueued = false;

    private GameMode gameModeManager;
    private CharacterController controller;
    private Transform explosionEmpty, turret;
    private GameObject explosion;
    protected GameObject muzzleFlash;
    private Camera cam;

    private LayerMask raycastLayer;

    protected Coroutine routine;

    [SerializeField]
    protected TankStats stats;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void OnAmmoChange(int oldAmmo, int newAmmo, bool asServer)
    {
        ammoCount = newAmmo;

        if (!MainView.Instance || !IsOwner)
            return;

        MainView.Instance.UpdateAmmo(newAmmo);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(1);
        SubscribeToTimeManager(true);
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        controller.enabled =  IsServer || IsOwner;
        namePlate.text = controllingPlayer.PlayerUsername;
        raycastLayer = (1 << 9);
        ChangeColours(controllingPlayer.Color);

        if (IsOwner)
            MainView.Instance.maxCharge = stats.requiredSuperCharge;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        canUseSuper = false;
        gameModeManager = FindObjectOfType<GameMode>();
        bulletSpawn = turret.GetChild(0).GetChild(0);
        muzzleFlashSpawn = turret.GetChild(0).GetChild(1);
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        muzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        ammoCount = stats.maxAmmo;
    }

    [Client]
    public virtual void ChangeColours(string color)
    {
        if (animateShader)
        {
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + color).WaitForCompletion();
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + color).WaitForCompletion();
            gameObject.GetComponent<Light>().color = transform.GetChild(0).GetComponent<MeshRenderer>().material.GetColor("_Color01");
        }
        else
        {
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        }
        explosion = Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
        muzzleFlash = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlash").WaitForCompletion();
    }


    [Server]
    public void GameOver()
    {
        ammoCount = 0;
        NetworkObject explosionInstance = NetworkManager.GetPooledInstantiated(explosion, true);
        explosionInstance.transform.SetParent(explosionEmpty);
        explosionInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
        Spawn(explosionInstance);
        Despawn();
        controllingPlayer.ControlledPawn = null;
        gameModeManager.OnKilled(controllingPlayer);
    }


    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (TimeManager)
        {
            SubscribeToTimeManager(false);
        }
    }

    [ServerRpc]
    protected virtual void Fire()
    {
        if (poolBullets)
        {
            NetworkObject bulletInstance = NetworkManager.GetPooledInstantiated(bullet, true);
            bulletInstance.transform.SetParent(bulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().chargeTimeToAdd = stats.onKillSuperCharge;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
            //bulletInstance.GetComponent<Bullet>().AfterSpawning(bulletSpawn, 0);
            Spawn(bulletInstance);
            bulletInstance.GetComponent<Bullet>().AfterSpawning(bulletSpawn, 0);
        }
        else
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().chargeTimeToAdd = stats.onKillSuperCharge;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);
        }

        NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
        flashInstance.transform.SetParent(muzzleFlashEmpty);
        flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
        Spawn(flashInstance);

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    protected abstract void SpecialMove();


    [Server]
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

    [Client]
    private void Update()
    {
        if (!IsSpawned)
            return;

        namePlate.transform.LookAt(cam.transform);

        namePlate.transform.Rotate(new Vector3(0f, 180f, 0f));

        if (!IsOwner)
            return;

        firingQueued |= Input.GetMouseButtonDown(0);

        superQueued |= Input.GetMouseButtonDown(1);
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
            ReconcileData rd = new(transform.position, transform.rotation, turret.rotation);
            Reconciliation(rd, true);

            if (controllingPlayer.superCharge >= stats.requiredSuperCharge)
            {
                canUseSuper = true;
            }
            else
            {
                canUseSuper = false;
                controllingPlayer.superCharge += TimeManager.TickDelta;
                Mathf.Clamp((float)controllingPlayer.superCharge, 0f, stats.requiredSuperCharge);
            }
        }
    }

    private void GatherInputs(out MoveData data)
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        data = new MoveData(moveAxis, rotateAxis, firingQueued, superQueued, hit.point);

        firingQueued = false;
        superQueued = false;
    }


    [Replicate]
    private void Move(MoveData data, bool asServer, bool replaying = false)
    {
        if (!GameManager.Instance.GameInProgress)
            return;

        controller.Move((float)TimeManager.TickDelta * stats.moveSpeed * data.MoveAxis * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * stats.rotateSpeed * (float)TimeManager.TickDelta, 0f));

        transform.position = new Vector3(transform.position.x, 0.8f, transform.position.z);

        if (data.TurretLookDirection != Vector3.zero)
        {
            turret.LookAt(data.TurretLookDirection, Vector3.up);
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
        }

        if (!replaying && !asServer)
        {
            if (data.FireWeapon && ammoCount > 0)
            {
                Fire();
            }
            if (data.UseSuper && canUseSuper)
            {
                Debug.Log("test");
                SpecialMove();
            }
        }
    }

    [Reconcile]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Reconciliation(ReconcileData data, bool asServer)
    {
        transform.SetPositionAndRotation(data.Position, data.TankRotation);

        turret.rotation = data.TurretRotation;
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
}

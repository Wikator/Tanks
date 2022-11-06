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
        public Vector3 TurretLookDirection;

        public MoveData(float moveAxis, float rotateAxis, bool fireWeapon, Vector3 turretLookDirection)
        {
            MoveAxis = moveAxis;
            RotateAxis = rotateAxis;
            FireWeapon = fireWeapon;
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
    private bool animateShader;

    [SerializeField]
    protected TankStats stats;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    [HideInInspector]
    protected GameObject bullet, pointer;

    [SyncVar(OnChange = nameof(OnAmmoChange)), HideInInspector]
    public int ammoCount;

    [SyncVar, HideInInspector]
    public PlayerNetworking controllingPlayer;


    private float moveAxis;
    private float rotateAxis;

    private bool isSubscribed = false;
    private bool firingQueued = false;

    private GameMode gameModeManager;
    private CharacterController controller;
    private Transform explosionEmpty, turret;
    private GameObject explosion;
    protected GameObject muzzleFlash;
    private Camera cam;

    private LayerMask raycastLayer;

    protected Coroutine routine;

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
        raycastLayer = (1 << 9);
        cam = Camera.main;
        ammoCount = stats.maxAmmo;
        controller = GetComponent<CharacterController>();
        gameModeManager = FindObjectOfType<GameMode>();
        turret = transform.GetChild(1);
        bulletSpawn = turret.GetChild(0).GetChild(0);
        muzzleFlashSpawn = turret.GetChild(0).GetChild(1);
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        muzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        ChangeColours(controllingPlayer.color);
        SubscribeToTimeManager(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        controller.enabled =  IsServer || IsOwner;
    }
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

    [ServerRpc]
    protected virtual void Fire()
    {
        GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        Spawn(bulletInstance);

        GameObject flashInstance = Instantiate(muzzleFlash, muzzleFlashSpawn.position, muzzleFlashSpawn.rotation, muzzleFlashEmpty);
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

    private void Update()
    {
        if (!IsOwner)
            return;

        firingQueued |= Input.GetMouseButtonDown(0);

        if (Input.GetMouseButtonDown(1) && canUseSpecialMove)
            SpecialMove();
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
        }
    }

    private void GatherInputs(out MoveData data)
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        data = new MoveData(moveAxis, rotateAxis, firingQueued, hit.point);

        firingQueued = false;
    }


    [Replicate]
    private void Move(MoveData data, bool asServer, bool replaying = false)
    {
        if (!IsSpawned)
            return;

        controller.Move((float)TimeManager.TickDelta * stats.moveSpeed * data.MoveAxis * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * stats.rotateSpeed * (float)TimeManager.TickDelta, 0f));

        if (data.TurretLookDirection != Vector3.zero)
        {
            turret.LookAt(data.TurretLookDirection, Vector3.up);
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
        }

        if (!replaying && !asServer && data.FireWeapon && ammoCount > 0)
        {
            Fire();
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

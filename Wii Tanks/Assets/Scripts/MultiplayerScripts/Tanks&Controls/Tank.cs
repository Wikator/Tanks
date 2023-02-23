using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Graphics;

public abstract class Tank : NetworkBehaviour
{
    #region Structs

    // Structs used by Client Side Prediction

    private struct MoveData : IReplicateData
    {
        public float MoveAxis;
        public float RotateAxis;
        public Vector3 TurretLookDirection;

        private uint _tick;

        public MoveData(float moveAxis, float rotateAxis, Vector3 turretLookDirection)
        {
            MoveAxis = moveAxis;
            RotateAxis = rotateAxis;
            TurretLookDirection = turretLookDirection;
            _tick = 0;
        }

        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;  
    }

    private struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion TankRotation;
        public Quaternion TurretRotation;

        private uint _tick;

        public ReconcileData(Vector3 tankPosition, Quaternion tankRotation, Quaternion turretRotation)
        {
            Position = tankPosition;
            TankRotation = tankRotation;
            TurretRotation = turretRotation;
            _tick = 0;
        }
        
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #endregion

    [SerializeField]
    protected Stats stats;


    [HideInInspector]
    protected Transform bulletSpawn, muzzleFlashSpawn;

    [SyncVar, HideInInspector]
    protected bool canUseSuper;

    [SyncVar(OnChange = nameof(OnAmmoChange), ReadPermissions = ReadPermission.OwnerOnly), HideInInspector]
    protected int ammoCount;

    [SyncVar, HideInInspector]
    public PlayerNetworking controllingPlayer;

    [SyncVar, HideInInspector]
    public bool isDespawning = false;

    private bool isSubscribed = false;

    private CharacterController controller;
    private Transform turret;
    private Camera cam;
    private TextMesh namePlate;
    protected GameObject bullet, muzzleFlash;
    private GameObject explosion;

    private TankGraphics graphics;

    protected Coroutine routine;

    private LayerMask raycastLayer;


    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void OnAmmoChange(int oldAmmo, int newAmmo, bool asServer)
    {
        ammoCount = newAmmo;

        if (!MainView.Instance || !IsOwner)
            return;

        MainView.Instance.UpdateAmmo(newAmmo);
    }


    // This script uses A LOT of variables, and some only need to be set on client, and some need to be set on server
    // OnStartNetwork is called on both client and server
    // Before setting up tank's graphics, it first needs to create a struct to pass on as an argument. See more in Graphics namespace
    // The script needs to subscribe to TimeManager on both client and server for CSP to work


	public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).GetChild(0);
        isDespawning = false;

        if (GameManager.Instance.GameMode == "Mayhem")
        {
            stats.requiredSuperCharge = 10;
            controllingPlayer.superCharge = stats.requiredSuperCharge;
        }

        SubscribeToTimeManager(true);
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        controller.enabled = IsServer || IsOwner;
        namePlate = transform.GetChild(1).GetComponent<TextMesh>();
        namePlate.text = controllingPlayer.PlayerUsername;
        raycastLayer = 1 << 9;

        graphics = new TankGraphics(
            controllingPlayer.color,
            gameObject.GetComponent<Light>(),
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
            transform.GetChild(0).GetChild(1).gameObject.GetComponent<MeshRenderer>(),
            transform.GetChild(0).GetChild(2).gameObject.GetComponent<MeshRenderer>()
            );

        if (IsOwner)
        {
            MainView.Instance.maxCharge = stats.requiredSuperCharge;
            //GameObject.Find("Main Camera").transform.Rotate(-40.345f, 0f, 0f);
            GameObject.Find("Main Camera").AddComponent<CameraFollow>();
            CameraFollow.Player = transform;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        canUseSuper = false;
        bulletSpawn = turret.GetChild(0).GetChild(0);
        muzzleFlashSpawn = turret.GetChild(0).GetChild(1);
        ammoCount = 0;

        Dictionary<string, GameObject> prefabs = TankGraphics.ChangePrefabsColours(controllingPlayer.color, "Multiplayer", controllingPlayer.TankType);

        explosion = prefabs["Explosion"];
        muzzleFlash = prefabs["MuzzleFlash"];
        bullet = prefabs["Bullet"];

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
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
    public void DespawnForNewRound()
    {
        isDespawning = false;
        ammoCount = 0;
        Despawn();
    }


    [Server]
    public void GameOver()
    {
        ammoCount = 0;
        NetworkObject explosionInstance = NetworkManager.GetPooledInstantiated(explosion, true);
        explosionInstance.transform.SetParent(SceneManagerScript.ExplosionEmpty);
        explosionInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
        Spawn(explosionInstance);
        controllingPlayer.ControlledPawn = null;
        GameMode.Instance.OnKilled(controllingPlayer);
        Despawn();
    }


    // Movement input isn't collected in Update
    // Update collects input only for firing and Super
    // Name tag above the tank changes colour if its Super is ready

    [Client]
    private void Update()
    {
        if (!IsSpawned)
            return;


        if (namePlate)
        {
            if (Settings.ShowPlayerNames)
            {
                namePlate.gameObject.SetActive(true);
                namePlate.transform.LookAt(cam.transform);
                namePlate.transform.Rotate(new Vector3(0f, 180f, 0f));

                if (canUseSuper)
                {
                    namePlate.color = Color.green;
                }
                else
                {
                    namePlate.color = Color.white;
                }
            }
            else
            {
                namePlate.gameObject.SetActive(false);
            }
        }

        if (!IsOwner || !GameManager.Instance.GameInProgress || isDespawning)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SpecialMove();
        }
    }

    // Spawning and despawning animations play in FixedUpgrade
    // TankGraphics.DespawnAnimation returns a bool that informs if the tank has fully despawned

    [Client]
    private void FixedUpdate()
    {

        if (!IsSpawned || graphics == null)
			return;

        if (isDespawning)
        {
            if (graphics.DespawnAnimation())
            {
                DespawnForNewRound();
            }
        }
        else
        {
            graphics.SpawnAnimation();
        }
	}

    // Because Fire is a ServerRPC, there is a delay when shootings as a client

    [ServerRpc]
    protected virtual void Fire()
    {
        if (ammoCount <= 0 || !IsSpawned)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));

        GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, SceneManagerScript.BulletEmpty);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        Spawn(bulletInstance);

        NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
        flashInstance.transform.SetParent(SceneManagerScript.MuzzleFlashEmpty);
        flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
        Spawn(flashInstance);
    }


    // Supers are largely different based on the tank's type, so the method is overriden in the subclasses

    protected abstract void SpecialMove();

    // There are two stats that are used in reloading
    // timeToReload is the time that needs to pass after the last shot for the tank to start reloading
    // timeToAddTime is the time between each reload after the first one
    // Because of that, player needs to wait quite a bit for the tank to start reloading, but after it reloads the first bullet, the rest will reload much faster

    [Server]
    protected IEnumerator AddAmmo(float time)
    {
        yield return new WaitForSeconds(time);

        if (IsSpawned)
        {
            ammoCount++;

            if (ammoCount < stats.maxAmmo)
            {
                routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
            }
            else
            {
                routine = null;
            }
        }
    }

    // This method is responsible for movement
    // Because the game is server authorotive, and movement needs to be responsive, Client Side Prediction has been implemented
    // TBH I don't fully understand how it works, but basically the movement is predicted in Replicate method, and the Reconcile smooths out any inconsistencies

    private void TimeManager_OnTick()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            Reconciliation(default, false);
            Move(GatherInputs(), false);
        }

        if (IsServer)
        {
            Move(default, true);
            Reconciliation(new ReconcileData(transform.position, transform.rotation, turret.rotation), true);

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

    // Gathered input is returned in MoveData struct, so the tank can move on server, and predict the movement on owning client

    private MoveData GatherInputs()
    {

        float moveAxis = Input.GetAxis("Vertical");
        float rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        return new MoveData(moveAxis, rotateAxis, hit.point);
    }

    // Replicate method is called on both owning client and server
    // Movement on owning client is predicted, so small inconsistencies will be smoothed out in Reconcile

    [Replicate]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Move(MoveData data, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
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
    }

    [Reconcile]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Reconciliation(ReconcileData data, bool asServer, Channel channel = Channel.Unreliable)
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

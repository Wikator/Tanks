using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.HighDefinition;

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
        public int requiredSuperCharge;
        public int onKillSuperCharge;
    }

    [SerializeField]
    protected TankStats stats;

    [SerializeField]
    private bool animateShader;

    [SerializeField]
    protected bool poolBullets;

    [SerializeField]
    private float maxLightIntensity;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    [HideInInspector]
    protected GameObject bullet;

    //[SyncVar(ReadPermissions = ReadPermission.OwnerOnly)]
    //protected bool canUseSuper;

    [SyncVar, HideInInspector]
    protected bool canUseSuper;

    [SyncVar(OnChange = nameof(OnAmmoChange), ReadPermissions = ReadPermission.OwnerOnly), HideInInspector]
    protected int ammoCount;

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
    private Material tankMaterial, turretMaterial;

    private TextMesh namePlate;

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


	public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).GetChild(0);

        if (GameManager.Instance.gameMode == "Mayhem")
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
        raycastLayer = (1 << 9);
        ChangeColours(controllingPlayer.color);

        if (IsOwner)
        {
            MainView.Instance.maxCharge = stats.requiredSuperCharge;
        }
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
        ammoCount = 0;

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


    [Server]
    public void GameOver()
    {
        ammoCount = 0;
        NetworkObject explosionInstance = NetworkManager.GetPooledInstantiated(explosion, true);
        explosionInstance.transform.SetParent(explosionEmpty);
        explosionInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
        Spawn(explosionInstance);
        controllingPlayer.ControlledPawn = null;
        gameModeManager.OnKilled(controllingPlayer);
        Despawn();
    }


    [Client]
    private void Update()
    {
        if (!IsSpawned)
            return;


        if (namePlate)
        {
            if (PlayerNetworking.Instance.ShowPlayerNames)
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

        if (!IsOwner || !GameManager.Instance.GameInProgress)
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


	[Client]
	private void FixedUpdate()
	{
		    
		if (!IsSpawned || !tankMaterial || !turretMaterial)
			return;

        SpawnAnimation();
	}

    [ServerRpc]
    protected virtual void Fire()
    {
        if (ammoCount <= 0)
            return;

        if (poolBullets)
        {
            NetworkObject bulletInstance = NetworkManager.GetPooledInstantiated(bullet, true);
            bulletInstance.transform.SetParent(bulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);
            bulletInstance.GetComponent<Bullet>().AfterSpawning(bulletSpawn, 0);
        }
        else
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
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

    private MoveData GatherInputs()
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        return new MoveData(moveAxis, rotateAxis, hit.point);
    }


    [Replicate]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
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
    }

    [Reconcile]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private void Reconciliation(ReconcileData data, bool asServer)
    {
        transform.SetPositionAndRotation(data.Position, data.TankRotation);

        turret.rotation = data.TurretRotation;
    }




    [Client]
    public virtual void ChangeColours(string color)
    {
        if (animateShader)
        {
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + color).WaitForCompletion();
            tankMaterial = transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material;
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>("Animated" + color).WaitForCompletion();
            turretMaterial = turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().material;
            gameObject.GetComponent<HDAdditionalLightData>().color = tankMaterial.GetColor("_Color01");
            gameObject.GetComponent<HDAdditionalLightData>().intensity = 0f;
            tankMaterial.SetFloat("_CurrentAppearence", 0.34f);
            turretMaterial.SetFloat("_CurrentAppearence", 0.3f);
			transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = true;
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = true;
		}
        else
        {
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(color).WaitForCompletion();
        }
        explosion = Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
        muzzleFlash = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlash").WaitForCompletion();
    }

    [Client]
    private void SpawnAnimation()
    {
        if (turretMaterial.GetFloat("_CurrentAppearence") > -0.3f)
        {
            if (tankMaterial.GetFloat("_CurrentAppearence") > -0f)
            {
                tankMaterial.SetFloat("_CurrentAppearence", tankMaterial.GetFloat("_CurrentAppearence") - 0.01f);
            }
            else
            {
                if (tankMaterial.GetFloat("_CurrentAppearence") > -0.3f)
                {
                    turretMaterial.SetFloat("_CurrentAppearence", turretMaterial.GetFloat("_CurrentAppearence") - 0.01f);
                    tankMaterial.SetFloat("_CurrentAppearence", tankMaterial.GetFloat("_CurrentAppearence") - 0.01f);
                }
                else
                {
                    turretMaterial.SetFloat("_CurrentAppearence", turretMaterial.GetFloat("_CurrentAppearence") - 0.01f);


                    if (gameObject.GetComponent<HDAdditionalLightData>().intensity < maxLightIntensity)
                    {
                        gameObject.GetComponent<HDAdditionalLightData>().intensity += 0.00005f;
                    }
                }
            }
        }

		if (gameObject.GetComponent<HDAdditionalLightData>().intensity < 0.15f)
		{
			gameObject.GetComponent<HDAdditionalLightData>().intensity += 0.003f;
		}
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

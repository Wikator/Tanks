using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObjectPoolManager;
using ObjectGraphics;
using UnityEngine.Rendering.HighDefinition;

public abstract class Tank_SP : MonoBehaviour
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

    //[SerializeField]
    public Stats stats;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    [HideInInspector]
    protected GameObject bullet;


    [HideInInspector]
    public bool canUseSuper;

    protected int ammoCount;

    private float moveAxis;
    private float rotateAxis;

    private CharacterController controller;
    private Transform explosionEmpty;
    private Transform turret;
    private GameObject explosion;
    protected GameObject muzzleFlash;
    private Camera cam;

    private TankGraphics graphics;

    protected Coroutine routine;

    private LayerMask raycastLayer;


	private void Start()
	{
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).GetChild(0);
        raycastLayer = 1 << 9;

        graphics = new(
            "Green",
            gameObject.GetComponent<HDAdditionalLightData>(),
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>()
            );

        Dictionary<string, GameObject> prefabs = TankGraphics.ChangePrefabsColours("Green", "Singleplayer", "MediumTank");

        explosion = prefabs["Explosion"];
        muzzleFlash = prefabs["MuzzleFlash"];
        bullet = prefabs["Bullet"];

        canUseSuper = false;
        bulletSpawn = turret.GetChild(0).GetChild(0);
        muzzleFlashSpawn = turret.GetChild(0).GetChild(1);
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        muzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        ammoCount = 0;
        MainView_SP.Instance.maxCharge = stats.requiredSuperCharge;

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }


    public void GameOver()
    {
        ammoCount = 0;
        ObjectPoolManager_SP.GetPooledInstantiated(explosion, transform.position, transform.rotation, explosionEmpty);
        SpawnManager_SP.Instance.OnKilled(gameObject);
		Destroy(gameObject);
	}

    private void Update()
    {
        //if (!GameManager_SP.Instance.GameInProgress)
        //   return;

        Move(GatherInputs());

        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SpecialMove();
        }
    }

    private void FixedUpdate()
    {
        graphics.SpawnAnimation();
    }

    protected virtual void Fire()
    {
        if (ammoCount <= 0)
            return;

        GameObject bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        bulletInstance.GetComponent<Bullet_SP>().ChargeTimeToAdd = stats.onKillSuperCharge;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

        ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);

		if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));

        MainView_SP.Instance.UpdateAmmo(ammoCount);
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

        MainView_SP.Instance.UpdateAmmo(ammoCount);
    }


    private MoveData GatherInputs()
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        return new MoveData(moveAxis, rotateAxis, hit.point);
    }


    private void Move(MoveData data)
    {
        //if (!GameManager_SP.Instance.GameInProgress)
        //    return;

        controller.Move(Time.deltaTime * stats.moveSpeed * data.MoveAxis * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * stats.rotateSpeed * Time.deltaTime, 0f));

        //transform.position = new Vector3(transform.position.x, 0.8f, transform.position.z);

        if (data.TurretLookDirection != Vector3.zero)
        {
            turret.LookAt(data.TurretLookDirection, Vector3.up);
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
        }
    }
}

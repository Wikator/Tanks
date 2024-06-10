using System.Collections;
using Graphics;
using ObjectPoolManager;
using SinglePlayerScripts.Environment;
using UnityEngine;

public abstract class Tank_SP : MonoBehaviour
{
    //[SerializeField]
    public Stats stats;


    [HideInInspector] public bool canUseSuper;

    protected int ammoCount;

    [HideInInspector] protected GameObject bullet;

    [HideInInspector] protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    private Camera cam;

    private CharacterController controller;
    private GameObject explosion;
    private Transform explosionEmpty;

    private TankGraphics graphics;

    private float moveAxis;
    protected GameObject muzzleFlash;

    private LayerMask raycastLayer;
    private float rotateAxis;

    protected Coroutine routine;
    private Transform turret;


    private void Start()
    {
        //GameObject.Find("Main Camera").transform.Rotate(-40.345f, 0f, 0f);
        GameObject.Find("Main Camera").AddComponent<CameraFollow>();
        CameraFollow.Player = transform;
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).GetChild(0);
        raycastLayer = 1 << 9;

        graphics = new TankGraphics(
            "Green",
            gameObject.GetComponent<Light>(),
            transform.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
            turret.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
            transform.GetChild(0).GetChild(1).gameObject.GetComponent<MeshRenderer>(),
            transform.GetChild(0).GetChild(2).gameObject.GetComponent<MeshRenderer>()
        );

        var prefabs = TankGraphics.ChangePrefabsColours("Green", "Singleplayer", "MediumTank");

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

    private void Update()
    {
        //if (!GameManager_SP.Instance.GameInProgress)
        //   return;

        Move(GatherInputs());

        if (Input.GetMouseButtonDown(0)) Fire();

        if (Input.GetMouseButtonDown(1)) SpecialMove();
    }

    private void FixedUpdate()
    {
        graphics.SpawnAnimation();
    }


    public void GameOver()
    {
        ammoCount = 0;
        ObjectPoolManager_SP.GetPooledInstantiated(explosion, transform.position, transform.rotation, explosionEmpty);
        SpawnManager_SP.Instance.OnKilled(gameObject);
        Destroy(gameObject);
    }

    protected virtual void Fire()
    {
        if (ammoCount <= 0)
            return;

        var bulletInstance =
            ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        bulletInstance.GetComponent<Bullet_SP>().ChargeTimeToAdd = stats.onKillSuperCharge;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(),
            true);
        bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

        ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation,
            muzzleFlashEmpty);

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
            routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
        else
            routine = null;

        MainView_SP.Instance.UpdateAmmo(ammoCount);
    }


    private MoveData GatherInputs()
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, raycastLayer);

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

    private struct MoveData
    {
        public readonly float MoveAxis;
        public readonly float RotateAxis;
        public readonly Vector3 TurretLookDirection;

        public MoveData(float moveAxis, float rotateAxis, Vector3 turretLookDirection)
        {
            MoveAxis = moveAxis;
            RotateAxis = rotateAxis;
            TurretLookDirection = turretLookDirection;
        }
    }
}
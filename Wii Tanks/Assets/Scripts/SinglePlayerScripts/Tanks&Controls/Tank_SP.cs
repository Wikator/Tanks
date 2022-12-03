using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

    [SerializeField]
    protected Stats stats;

    [SerializeField]
    private bool animateShader;


    [SerializeField]
    private float maxLightIntensity;

    [HideInInspector]
    protected Transform bulletSpawn, bulletEmpty, muzzleFlashSpawn, muzzleFlashEmpty;

    [HideInInspector]
    protected GameObject bullet;


    [HideInInspector]
    protected bool canUseSuper;

    [HideInInspector]
    protected int ammoCount;


    private float moveAxis;
    private float rotateAxis;



    private CharacterController controller;
    private Transform explosionEmpty, turret;
    private GameObject explosion;
    protected GameObject muzzleFlash;
    private Camera cam;
    private Material tankMaterial, turretMaterial;


    protected Coroutine routine;

    private LayerMask raycastLayer;






	private void Start()
	{
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).GetChild(0);
        raycastLayer = (1 << 9);
        ChangeColours("Green");
        canUseSuper = false;
        bulletSpawn = turret.GetChild(0).GetChild(0);
        muzzleFlashSpawn = turret.GetChild(0).GetChild(1);
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
        muzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        ammoCount = 0;

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }


    public void GameOver()
    {
        ammoCount = 0;
        Instantiate(explosion, transform.position, transform.rotation, explosionEmpty);
		Destroy(gameObject);
	}

    private void Update()
    {
        if (!GameManager.Instance.GameInProgress)
            return;

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

        if (!tankMaterial || !turretMaterial)
            return;

        SpawnAnimation();
    }

    protected virtual void Fire()
    {
        if (ammoCount <= 0)
            return;

        GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);

		Instantiate(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);

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


    private MoveData GatherInputs()
    {
        moveAxis = Input.GetAxis("Vertical");
        rotateAxis = Input.GetAxis("Horizontal");

        Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, raycastLayer);

        return new MoveData(moveAxis, rotateAxis, hit.point);
    }


    private void Move(MoveData data)
    {
        if (!GameManager.Instance.GameInProgress)
            return;

        controller.Move(Time.deltaTime * stats.moveSpeed * data.MoveAxis * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * stats.rotateSpeed * Time.deltaTime, 0f));

        transform.position = new Vector3(transform.position.x, 0.8f, transform.position.z);

        if (data.TurretLookDirection != Vector3.zero)
        {
            turret.LookAt(data.TurretLookDirection, Vector3.up);
            turret.localEulerAngles = new Vector3(0, turret.localEulerAngles.y, 0);
        }
    }


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
}

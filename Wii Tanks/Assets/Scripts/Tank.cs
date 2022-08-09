using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using System.Collections;
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
    private int maxAmmo;

    [SerializeField]
    private string bulletType;

    [SerializeField]
    private LayerMask raycastLayer;

    [HideInInspector]
    public Transform bulletSpawn, bulletEmpty;

    [HideInInspector]
    public GameObject bullet, pointer;

    [HideInInspector]
    [SyncVar]
    public int ammoCount;

    [HideInInspector]
    [SyncVar]
    public PlayerNetworking controllingPlayer;


    private float moveAxis;
    private float rotateAxis;

    private bool isSubscribed = false;

    private GameMode gameModeManager;
    private CharacterController controller;
    private Transform explosionEmpty;
    private GameObject explosion, turret;
    private Camera cam;


    public override void OnStartClient()
    {
        base.OnStartClient();
        cam = Camera.main;
        ammoCount = maxAmmo;
        gameModeManager = FindObjectOfType<GameMode>();
        controller = GetComponent<CharacterController>();
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
        cam = Camera.main;
        ammoCount = maxAmmo;
        gameModeManager = FindObjectOfType<GameMode>();
        controller = GetComponent<CharacterController>();
        turret = transform.GetChild(0).gameObject;
        bulletSpawn = turret.transform.GetChild(0).transform;
        bulletEmpty = GameObject.Find("Bullets").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
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
        bullet = Addressables.LoadAssetAsync<GameObject>(color + bulletType).WaitForCompletion();
        explosion = Addressables.LoadAssetAsync<GameObject>(color + "Explosion").WaitForCompletion();
    }

    public IEnumerator AddAmmo (float time)
    {
        yield return new WaitForSeconds(time);
        ammoCount++;

        if (ammoCount != maxAmmo)
            StartCoroutine(AddAmmo(0.35f));
    }


    [Replicate]
    private void Move(MoveData data, bool asServer, bool replaying = false)
    {
        controller.Move((float)TimeManager.TickDelta * data.MoveAxis * moveSpeed * transform.forward);
        transform.Rotate(new Vector3(0f, data.RotateAxis * rotateSpeed * (float)TimeManager.TickDelta, 0f));

        turret.transform.LookAt(data.TurretLookDirection, Vector3.up);
        turret.transform.localEulerAngles = new Vector3(0, turret.transform.localEulerAngles.y, 0);

        //pointer.transform.position = data.TurretLookDirection;
    }

    [Reconcile]
    private void Reconciliation(ReconcileData data, bool asServer)
    {
        transform.SetPositionAndRotation(data.Position, data.TankRotation);

        turret.transform.rotation = data.TurretRotation;
    }
}


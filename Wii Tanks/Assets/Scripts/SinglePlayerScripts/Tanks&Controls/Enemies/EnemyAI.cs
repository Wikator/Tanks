using Graphics;
using ObjectPoolManager;
using SinglePlayerScripts.Environment;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyAI : MonoBehaviour
{
    public LayerMask whatIsGround, whatIsWall, whatIsPlayer, raycastLayer;

    [SerializeField] private float walkPointRange;

    [SerializeField] private float timeBetweenAttacks;

    [SerializeField] private float roundSightRange, forwardSightRange;

    protected NavMeshAgent agent;
    private bool alreadyAttacked;
    protected GameObject bullet;
    protected Transform bulletEmpty;
    protected Transform bulletSpawn;

    protected string color;

    protected GameObject explosion;
    private Transform explosionEmpty;

    protected TankGraphics graphics;
    protected GameObject muzzleFlash;
    protected Transform muzzleFlashEmpty;
    private bool playerInSightRange;

    protected Transform target;

    protected Transform turret;

    protected Vector3 walkPoint;
    protected bool walkPointSet;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        //target = Player.Instance.gameObject.transform;
        agent.updateRotation = false;
        turret = transform.GetChild(0).GetChild(0);
        bulletSpawn = turret.GetChild(0).GetChild(0);
        bulletEmpty = GameObject.Find("Bullets").transform;
        muzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        explosionEmpty = GameObject.Find("Explosions").transform;
    }

    private void Update()
    {
        if (!playerInSightRange && Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f,
                bulletSpawn.forward, out var hit, forwardSightRange, whatIsPlayer))
            if (!hit.collider.CompareTag(gameObject.tag))
            {
                playerInSightRange = true;
                target = hit.collider.transform;
            }

        playerInSightRange = Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f, bulletSpawn.forward,
            forwardSightRange, whatIsPlayer);

        if (Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f, bulletSpawn.forward, out var hit1,
                forwardSightRange, whatIsPlayer))
            if (!hit1.collider.CompareTag(gameObject.tag))
                target = hit1.collider.transform;
            else
                playerInSightRange = false;

        if (playerInSightRange)
        {
            ChasePlayer();
            AttackPlayer();
        }
        else
        {
            Patroling();
            turret.transform.Rotate(50 * Time.deltaTime * Vector3.up);
        }

        var distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }

    private void FixedUpdate()
    {
        graphics.SpawnAnimation();
    }

    protected virtual void OnEnable()
    {
        alreadyAttacked = true;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
        //target = Player.Instance.ControlledPawn.transform;

        if (GameObject.Find("Scene"))
            switch (GameObject.Find("Scene").GetComponent<BigTeamBattleSpawnManager_SP>().color)
            {
                case 0:
                    color = "Green";
                    gameObject.tag = "GreenEnemy";
                    break;
                case 1:
                    color = "Blue";
                    gameObject.tag = "BlueEnemy";
                    break;
                case 2:
                    color = "Cyan";
                    gameObject.tag = "CyanEnemy";
                    break;
                case 3:
                    color = "Yellow";
                    gameObject.tag = "YellowEnemy";
                    break;
                case 4:
                    color = "Purple";
                    gameObject.tag = "PurpleEnemy";
                    break;
                case 5:
                    color = "Red";
                    gameObject.tag = "RedEnemy";
                    break;
            }
        else
            switch (Random.Range(0, 5))
            {
                case 0:
                    color = "Red";
                    gameObject.tag = "RedEnemy";
                    break;
                case 1:
                    color = "Blue";
                    gameObject.tag = "BlueEnemy";
                    break;
                case 2:
                    color = "Cyan";
                    gameObject.tag = "CyanEnemy";
                    break;
                case 3:
                    color = "Yellow";
                    gameObject.tag = "YellowEnemy";
                    break;
                case 4:
                    color = "Purple";
                    gameObject.tag = "PurpleEnemy";
                    break;
            }
    }


    private void Patroling()
    {
        if (!walkPointSet)
        {
            SearchWalkPoint();
            agent.SetDestination(walkPoint);
        }

        if (agent.velocity.magnitude == 0)
            walkPointSet = false;

        if (agent.velocity.normalized != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);

        var distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        var randomZ = Random.Range(-walkPointRange, walkPointRange);
        var randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (!Physics.Raycast(walkPoint + new Vector3(0, 10, 0), -transform.up, 25f, whatIsWall))
            if (Physics.Raycast(walkPoint + new Vector3(0, 10, 0), -transform.up, 25f, whatIsGround))
                walkPointSet = true;
    }

    protected abstract void ChasePlayer();


    private void AttackPlayer()
    {
        turret.LookAt(target.position);

        if (!alreadyAttacked)
        {
            Physics.Raycast(bulletSpawn.position, bulletSpawn.forward, out var hit, forwardSightRange, ~raycastLayer);
            Debug.DrawRay(bulletSpawn.position, bulletSpawn.forward);
            if ((!hit.collider.tag.Contains("Player") && !hit.collider.tag.Contains("Enemy") &&
                 !hit.collider.CompareTag("Bullet")) || hit.collider.CompareTag(gameObject.tag)) return;
            Debug.DrawRay(bulletSpawn.position, bulletSpawn.forward * 20, Color.green);

            var direction = (target.transform.position - transform.position).normalized;
            var distance = Vector3.Distance(target.transform.position, transform.position);
            var distance2 = Vector3.Distance(target.transform.position, transform.position);
            var timeToReachPlayer = distance2 / bullet.GetComponent<Bullet_SP>().moveSpeed;
            var leadOffset = target.GetComponent<CharacterController>().velocity * timeToReachPlayer;
            var predictedPosition = target.transform.position + leadOffset;

            turret.LookAt(predictedPosition);

            Fire();

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    protected abstract void Fire();


    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void GameOver()
    {
        SpawnManager_SP.Instance.OnKilled(gameObject);
        ObjectPoolManager_SP.GetPooledInstantiated(explosion, transform.position, transform.rotation, explosionEmpty);
        gameObject.SetActive(false);
    }
}
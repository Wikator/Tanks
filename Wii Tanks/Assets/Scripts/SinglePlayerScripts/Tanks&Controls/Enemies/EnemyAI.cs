using UnityEngine;
using UnityEngine.AI;
using ObjectPoolManager;
using Graphics;

public abstract class EnemyAI : MonoBehaviour
{
	protected NavMeshAgent agent;

	protected Transform target;
	protected Transform bulletSpawn;
	protected Transform bulletEmpty;
	protected Transform muzzleFlashEmpty;
	private Transform explosionEmpty;

	public LayerMask whatIsGround, whatIsWall, whatIsPlayer, raycastLayer;

	protected Vector3 walkPoint;
	protected bool walkPointSet;

	protected Transform turret;

	[SerializeField]
	private float walkPointRange;

	[SerializeField]
	private float timeBetweenAttacks;
	private bool alreadyAttacked;

	[SerializeField]
	private float roundSightRange, forwardSightRange;
	private bool playerInSightRange;

	protected GameObject explosion;
	protected GameObject muzzleFlash;
	protected GameObject bullet;

	protected TankGraphics graphics;

	protected string color;


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

    protected virtual void OnEnable()
    {
		alreadyAttacked = true;
		Invoke(nameof(ResetAttack), timeBetweenAttacks);
		//target = Player.Instance.ControlledPawn.transform;
		
		if (GameObject.Find("Scene"))
		{
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
		}
		else
		{
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

		
	}

    private void FixedUpdate()
	{
		graphics.SpawnAnimation();
	}

	private void Update()
	{
		if (!playerInSightRange && Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f, bulletSpawn.forward, out RaycastHit hit, forwardSightRange, whatIsPlayer))
		{
			if (!hit.collider.CompareTag(gameObject.tag))
			{
				playerInSightRange = true;
				target = hit.collider.transform;
			}
		}
		
		playerInSightRange = Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f, bulletSpawn.forward, forwardSightRange, whatIsPlayer);

		if (Physics.Raycast(bulletSpawn.position + bulletSpawn.forward * 1.5f, bulletSpawn.forward, out RaycastHit hit1, forwardSightRange, whatIsPlayer))
			if (!hit1.collider.CompareTag(gameObject.tag))
			{
				target = hit1.collider.transform;
			}
			else
			{
				playerInSightRange = false;
			}

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

		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (distanceToWalkPoint.magnitude < 1f)
			walkPointSet = false;
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

		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (distanceToWalkPoint.magnitude < 1f)
			walkPointSet = false;
	}

	private void SearchWalkPoint()
	{
		float randomZ = Random.Range(-walkPointRange, walkPointRange);
		float randomX = Random.Range(-walkPointRange, walkPointRange);

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
			Physics.Raycast(bulletSpawn.position, bulletSpawn.forward, out RaycastHit hit, forwardSightRange, ~raycastLayer);
			Debug.DrawRay(bulletSpawn.position, bulletSpawn.forward);
			if ((!hit.collider.CompareTag("Tank") && !hit.collider.tag.Contains("Enemy") && !hit.collider.CompareTag("Bullet")) || hit.collider.CompareTag(gameObject.tag))
			{
				return;
			}
			Debug.DrawRay(bulletSpawn.position, bulletSpawn.forward * 20, Color.green);
			
			Vector3 direction = (target.transform.position - transform.position).normalized;
			float distance = Vector3.Distance(target.transform.position, transform.position);
			float distance2 = Vector3.Distance(target.transform.position, transform.position);
			float timeToReachPlayer = distance2 / bullet.GetComponent<Bullet_SP>().moveSpeed;
			Vector3 leadOffset = target.GetComponent<CharacterController>().velocity * timeToReachPlayer;
			Vector3 predictedPosition = target.transform.position + leadOffset;
			
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
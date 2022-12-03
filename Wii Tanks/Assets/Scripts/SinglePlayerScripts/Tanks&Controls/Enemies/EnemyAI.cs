using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
	private NavMeshAgent agent;

	private Transform target;

	public LayerMask whatIsGround, whatIsPlayer;

	private Vector3 walkPoint;
	private bool walkPointSet;
	private float walkPointRange;

	[SerializeField]
	private float timeBetweenAttacks;
	private bool alreadyAttacked;

	private float sightRange;
	private bool playerInSightRange;


	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		target = Player.Instance.gameObject.transform;
	}



	private void Update()
	{
		playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

		if (!playerInSightRange)
			Patroling();

		if (playerInSightRange)
		{
			ChasePlayer();
			AttackPlayer();
		}
	}


	private void Patroling()
	{
		if (!walkPointSet)
			SearchWalkPoint();


		if (walkPointSet)
			agent.SetDestination(walkPoint);

		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (distanceToWalkPoint.magnitude < 1f)
			walkPointSet = false;
	}

	private void SearchWalkPoint()
	{
		float randomZ = Random.Range(-walkPointRange, walkPointRange);
		float randomX = Random.Range(-walkPointRange, walkPointRange);

		walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

		if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
			walkPointSet = true;
	}

	private void ChasePlayer()
	{
		agent.SetDestination(target.position);
	}

	private void AttackPlayer()
	{
		agent.SetDestination(transform.position);

		transform.LookAt(target);
		
		if (!alreadyAttacked)
		{
			// Attack code here

			alreadyAttacked = false;
			Invoke(nameof(ResetAttack), timeBetweenAttacks);
		}
	}

	private void ResetAttack()
	{
		alreadyAttacked = false;
	}
}
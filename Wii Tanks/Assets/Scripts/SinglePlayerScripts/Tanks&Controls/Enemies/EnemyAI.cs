using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.Rendering.HighDefinition;

public abstract class EnemyAI : MonoBehaviour
{
	protected NavMeshAgent agent;

	protected Transform target;

	public LayerMask whatIsGround, whatIsWall, whatIsPlayer;

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

	private Material tankMaterial, turretMaterial;


	private GameObject explosion;
	protected GameObject muzzleFlash;

	protected string color;


	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		//target = Player.Instance.gameObject.transform;
		target = Player.Instance.ControlledPawn.transform;
		agent.updateRotation = false;
		turret = transform.GetChild(0).GetChild(0);;
	}

    private void OnEnable()
    {
		switch (Random.Range(0, 5))
		{
			case 0:
				color = "Red";
				break;
			case 1:
				color = "Blue";
				break;
			case 2:
				color = "Cyan";
				break;
			case 3:
				color = "Yellow";
				break;
			case 4:
				color = "Purple";
				break;
		}
        
		ChangeColours(color);
	}

    private void FixedUpdate()
	{

		if (!tankMaterial || !turretMaterial)
			return;

		SpawnAnimation();
	}

	private void Update()
	{

		if (!playerInSightRange && (Physics.CheckSphere(transform.position, roundSightRange, whatIsPlayer) || Physics.Raycast(turret.GetChild(0).GetChild(0).position + turret.GetChild(0).GetChild(0).transform.transform.forward * 1.5f, turret.GetChild(0).GetChild(0).transform.forward, forwardSightRange, whatIsPlayer)))
		{
			walkPointSet = false;
			Debug.Log("Reset");
		}


		playerInSightRange = Physics.CheckSphere(transform.position, roundSightRange, whatIsPlayer) || Physics.Raycast(turret.GetChild(0).GetChild(0).position + turret.GetChild(0).GetChild(0).transform.transform.forward * 1.5f, turret.GetChild(0).GetChild(0).transform.forward, forwardSightRange, whatIsPlayer);

		Debug.DrawRay(turret.GetChild(0).GetChild(0).position + turret.GetChild(0).GetChild(0).transform.transform.forward * 1.5f, turret.GetChild(0).GetChild(0).transform.transform.forward * forwardSightRange, Color.red);



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
			SearchWalkPoint();

		if (agent.velocity.magnitude == 0)
			SearchWalkPoint();


		if (walkPointSet)
			agent.SetDestination(walkPoint);

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
		turret.LookAt(target.transform.position);

		if (!alreadyAttacked)
		{
			// Attack code here
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
		ObjectPoolManager_SP.GetPooledInstantiated(explosion, transform.position, transform.rotation, GameObject.Find("Explosions").transform);
		gameObject.SetActive(false);
	}




	public virtual void ChangeColours(string color)
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
		
		explosion = Addressables.LoadAssetAsync<GameObject>(color + "ExplosionSP").WaitForCompletion();
		muzzleFlash = Addressables.LoadAssetAsync<GameObject>(color + "MuzzleFlashSP").WaitForCompletion();
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


					if (gameObject.GetComponent<HDAdditionalLightData>().intensity < 0.15f)
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
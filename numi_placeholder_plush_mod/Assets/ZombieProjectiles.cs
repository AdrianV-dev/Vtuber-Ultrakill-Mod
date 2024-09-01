using UnityEngine;
using UnityEngine.AI;

public class ZombieProjectiles : MonoBehaviour
{
	public bool stationary;

	public bool alwaysStationary;

	public bool smallRay;

	public bool wanderer;

	public bool afraid;

	public bool chaser;

	public bool hasMelee;

	private Zombie zmb;

	private GameObject player;

	private GameObject camObj;

	private NavMeshAgent nma;

	private NavMeshPath nmp;

	private NavMeshHit hit;

	private Animator anim;

	private Rigidbody rb;

	public Vector3 targetPosition;

	private float coolDown = 1f;

	private AudioSource aud;

	public TrailRenderer tr;

	public GameObject projectile;

	private GameObject currentProjectile;

	public Transform shootPos;

	public GameObject head;

	public bool playerSpotted;

	private RaycastHit rhit;

	private RaycastHit bhit;

	public LayerMask lookForPlayerMask;

	public bool seekingPlayer = true;

	private Vector3 wanderTarget;

	private float raySize = 1f;

	private bool musicRequested;

	public GameObject decProjectileSpawner;

	public GameObject decProjectile;

	private GameObject currentDecProjectile;

	public bool swinging;

	[HideInInspector]
	public bool blocking;

	[HideInInspector]
	public int difficulty;

	private float coolDownReduce;

	private EnemyIdentifier eid;

	private GameObject origWP;

	public Transform aimer;

	private Quaternion aimerDefaultRotation;

	private bool aiming;

	private Quaternion origRotation;

	private float aimEase;

	private Vector3 predictedPosition;

	private float predictionLerp;

	private bool predictionLerping;

	private bool moveForward;

	private float forwardSpeed;

	private SwingCheck2[] swingChecks;

	private float lengthOfStop;

	private Vector3 spawnPos;

	private bool valuesSet;

	private void Awake()
	{
		zmb = GetComponent<Zombie>();
		nma = GetComponent<NavMeshAgent>();
		anim = GetComponent<Animator>();
		eid = GetComponent<EnemyIdentifier>();
		rb = GetComponent<Rigidbody>();
	}

	private void SetValues()
	{
		if (!valuesSet)
		{
			valuesSet = true;
			player = MonoSingleton<PlayerTracker>.Instance.GetPlayer().gameObject;
			camObj = MonoSingleton<PlayerTracker>.Instance.GetTarget().gameObject;
			if ((bool)aimer)
			{
				aimerDefaultRotation = Quaternion.Inverse(base.transform.rotation) * aimer.rotation;
			}
			nmp = new NavMeshPath();
			if (hasMelee && (swingChecks == null || swingChecks.Length == 0))
			{
				swingChecks = GetComponentsInChildren<SwingCheck2>();
			}
			if (eid.difficultyOverride >= 0)
			{
				difficulty = eid.difficultyOverride;
			}
			else
			{
				difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
			}
			origWP = eid.weakPoint;
			spawnPos = base.transform.position;
			if (alwaysStationary)
			{
				stationary = true;
			}
			if (stationary || smallRay)
			{
				raySize = 0.25f;
			}
			if (difficulty >= 3)
			{
				coolDownReduce = 1f;
			}
		}
	}

	private void Start()
	{
		SetValues();
		if (!stationary && wanderer && eid.target != null)
		{
			Invoke("Wander", 0.5f);
		}
		SlowUpdate();
	}

	private void OnEnable()
	{
		SetValues();
		if (!musicRequested && playerSpotted && (bool)zmb && !eid.IgnorePlayer)
		{
			musicRequested = true;
			zmb.musicRequested = true;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if (instance != null)
			{
				instance.PlayBattleMusic();
			}
		}
		if (hasMelee)
		{
			MeleeDamageEnd();
		}
		if (tr != null)
		{
			tr.emitting = false;
		}
		if (currentDecProjectile != null)
		{
			Object.Destroy(currentDecProjectile);
			eid.weakPoint = origWP;
		}
		swinging = false;
	}

	private void OnDisable()
	{
		if (musicRequested && !eid.IgnorePlayer && !zmb.limp)
		{
			musicRequested = false;
			zmb.musicRequested = false;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if (instance != null)
			{
				instance.PlayCleanMusic();
			}
		}
		coolDown = Random.Range(1f, 2.5f) - coolDownReduce;
	}

	private void SlowUpdate()
	{
		if (base.gameObject.activeInHierarchy)
		{
			if (zmb.grounded && (bool)nma && !zmb.limp && eid.target != null && !swinging)
			{
				Vector3 vector = eid.target.position - base.transform.position;
				Vector3 normalized = (eid.target.headPosition - head.transform.position).normalized;
				float num = Vector3.Distance(eid.target.position, base.transform.position);
				if (afraid && !swinging && num < 15f && nma.enabled)
				{
					nma.updateRotation = true;
					targetPosition = new Vector3(base.transform.position.x + vector.normalized.x * -10f, base.transform.position.y, base.transform.position.z + vector.normalized.z * -10f);
					if (nma.enabled && nma.isOnNavMesh)
					{
						if (NavMesh.SamplePosition(targetPosition, out hit, 1f, nma.areaMask))
						{
							SetDestination(targetPosition);
						}
						else if (NavMesh.FindClosestEdge(targetPosition, out hit, nma.areaMask))
						{
							targetPosition = hit.position;
							SetDestination(targetPosition);
						}
					}
					if (nma.velocity.magnitude < 1f)
					{
						lengthOfStop += 0.5f;
					}
					else
					{
						lengthOfStop = 0f;
					}
				}
				if (num > 15f || lengthOfStop > 0.75f || !afraid)
				{
					lengthOfStop = 0f;
					if (playerSpotted && (!chaser || Vector3.Distance(base.transform.position, eid.target.position) < 3f || coolDown == 0f) && (Vector3.Distance(base.transform.position, eid.target.position) < 30f || (Vector3.Distance(base.transform.position, eid.target.position) < 60f && coolDown == 0f) || stationary || (nmp.status != 0 && (nmp.corners.Length == 0 || Vector3.Distance(base.transform.position, nmp.corners[nmp.corners.Length - 1]) < 3f))) && !Physics.Raycast(head.transform.position, normalized, out bhit, Vector3.Distance(eid.target.headPosition, head.transform.position), lookForPlayerMask))
					{
						seekingPlayer = false;
						if (!wanderer)
						{
							SetDestination(base.transform.position);
						}
						else if (wanderer && !chaser && coolDown <= 0f)
						{
							SetDestination(base.transform.position);
						}
						if (hasMelee && Vector3.Distance(base.transform.position, eid.target.position) <= 3f)
						{
							Melee();
						}
						else if (coolDown <= 0f && (!nma.enabled || nma.velocity.magnitude <= 2.5f))
						{
							Swing();
						}
						else if (wanderer && coolDown > 0f && nma.velocity.magnitude < 1f)
						{
							Wander();
						}
					}
					else if (!stationary && nma.enabled)
					{
						if (chaser)
						{
							if (nma == null)
							{
								nma = zmb.nma;
							}
							if (zmb.grounded && nma != null && nma.enabled && nma.isOnNavMesh && eid.target != null)
							{
								if (Physics.Raycast(eid.target.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, float.PositiveInfinity, lookForPlayerMask))
								{
									SetDestination(hitInfo.point);
								}
								else
								{
									SetDestination(eid.target.position);
								}
							}
						}
						else if ((bool)nma && nma.enabled && nma.isOnNavMesh)
						{
							seekingPlayer = true;
							nma.updateRotation = true;
							if (Physics.Raycast(eid.target.position + Vector3.up * 0.1f, Vector3.down, out rhit, float.PositiveInfinity, lookForPlayerMask))
							{
								SetDestination(rhit.point);
							}
							else
							{
								SetDestination(eid.target.position);
							}
						}
					}
				}
			}
			if (stationary && !alwaysStationary && Vector3.Distance(base.transform.position, spawnPos) > 5f)
			{
				stationary = false;
			}
		}
		if (!eid.dead)
		{
			if (chaser || eid.enemyType == EnemyType.Soldier)
			{
				Invoke("SlowUpdate", 0.1f);
			}
			else
			{
				Invoke("SlowUpdate", 0.5f);
			}
		}
	}

	private void Update()
	{
		if (!zmb.grounded || zmb.limp)
		{
			return;
		}
		if (coolDown > 0f)
		{
			coolDown = Mathf.MoveTowards(coolDown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (!playerSpotted && eid.target != null)
		{
			Vector3 normalized = (eid.target.headPosition - head.transform.position).normalized;
			if (!Physics.Raycast(head.transform.position, normalized, out rhit, Vector3.Distance(eid.target.headPosition, head.transform.position), lookForPlayerMask))
			{
				seekingPlayer = false;
				playerSpotted = true;
				coolDown = (float)Random.Range(1, 2) - coolDownReduce / 2f;
				if (eid.target.isPlayer && !musicRequested)
				{
					musicRequested = true;
					zmb.musicRequested = true;
					MusicManager instance = MonoSingleton<MusicManager>.Instance;
					if (instance != null)
					{
						instance.PlayBattleMusic();
					}
				}
			}
		}
		if (eid.target == null)
		{
			if (!nma.enabled || nma.velocity.magnitude <= 2.5f)
			{
				anim.SetBool("Running", value: false);
				nma.updateRotation = false;
			}
		}
		else if ((!nma.enabled || nma.velocity.magnitude <= 2.5f) && playerSpotted && !seekingPlayer && (!wanderer || !swinging || chaser))
		{
			anim.SetBool("Running", value: false);
			nma.updateRotation = false;
			base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		}
		else if (nma.enabled && nma.velocity.magnitude > 2.5f)
		{
			anim.SetBool("Running", value: true);
			nma.updateRotation = true;
		}
		else
		{
			if ((nma.enabled && !(nma.velocity.magnitude <= 2.5f)) || !playerSpotted || seekingPlayer || !wanderer || !swinging)
			{
				return;
			}
			anim.SetBool("Running", value: false);
			nma.updateRotation = false;
			if (difficulty >= 2)
			{
				Vector3 vector = new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z);
				Quaternion b = Quaternion.LookRotation((vector - base.transform.position).normalized);
				if (difficulty == 2)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * 3.5f * eid.totalSpeedModifier);
				}
				else if (difficulty == 3)
				{
					base.transform.LookAt(vector);
				}
				else if (difficulty > 3)
				{
					base.transform.LookAt(vector);
				}
			}
		}
	}

	private void LateUpdate()
	{
		if (aimer != null && aiming && eid.target != null)
		{
			Vector3 vector = eid.target.headPosition;
			if (predictionLerping)
			{
				predictionLerp = Mathf.MoveTowards(predictionLerp, 1f, Time.deltaTime * 0.75f * anim.speed * eid.totalSpeedModifier);
				vector = Vector3.Lerp(vector, predictedPosition, predictionLerp);
			}
			Quaternion quaternion = Quaternion.LookRotation((vector - aimer.position).normalized);
			Quaternion quaternion2 = Quaternion.Inverse(base.transform.rotation * aimerDefaultRotation) * aimer.rotation;
			aimer.rotation = quaternion * quaternion2;
			if (aimEase < 1f)
			{
				aimEase = Mathf.MoveTowards(aimEase, 1f, Time.deltaTime * (20f - aimEase * 20f) * eid.totalSpeedModifier);
			}
			aimer.rotation = Quaternion.Slerp(origRotation, quaternion, aimEase);
		}
	}

	private void FixedUpdate()
	{
		if (moveForward)
		{
			float num = forwardSpeed * anim.speed * eid.totalSpeedModifier;
			forwardSpeed /= 1f + Time.fixedDeltaTime * forwardSpeed / 3f;
			if (Physics.Raycast(base.transform.position + Vector3.up + base.transform.forward * 2.5f, Vector3.down, out var hitInfo, (eid.target == null) ? 11f : Mathf.Max(11f, base.transform.position.y - eid.target.position.y + 2.5f), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore) && Vector3.Dot(base.transform.up, hitInfo.normal) > 0.25f)
			{
				rb.velocity = new Vector3(base.transform.forward.x * num, Mathf.Min(0f, rb.velocity.y), base.transform.forward.z * num);
			}
			else
			{
				rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
			}
		}
	}

	public void MoveForward(float speed)
	{
		forwardSpeed = speed * 10f;
		if ((bool)nma)
		{
			nma.enabled = false;
		}
		moveForward = true;
		rb.isKinematic = false;
	}

	private void StopMoveForward()
	{
		moveForward = false;
		if (zmb.grounded)
		{
			if ((bool)nma)
			{
				nma.enabled = true;
			}
			rb.isKinematic = true;
		}
	}

	private void SetDestination(Vector3 position)
	{
		if ((bool)nma && nma.isOnNavMesh)
		{
			NavMesh.CalculatePath(base.transform.position, position, nma.areaMask, nmp);
			nma.SetPath(nmp);
		}
	}

	public void Melee()
	{
		swinging = true;
		seekingPlayer = false;
		nma.updateRotation = false;
		base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		nma.enabled = false;
		if (tr == null)
		{
			tr = GetComponentInChildren<TrailRenderer>();
		}
		tr.GetComponent<AudioSource>().Play();
		anim.SetTrigger("Melee");
	}

	public void MeleePrep()
	{
		zmb.ParryableCheck();
	}

	public void MeleeDamageStart()
	{
		if (tr == null)
		{
			tr = GetComponentInChildren<TrailRenderer>();
		}
		if (tr != null)
		{
			tr.enabled = true;
			tr.emitting = true;
		}
		SwingCheck2[] array = swingChecks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DamageStart();
		}
	}

	public void MeleeDamageEnd()
	{
		if (tr != null)
		{
			tr.emitting = false;
		}
		SwingCheck2[] array = swingChecks;
		foreach (SwingCheck2 swingCheck in array)
		{
			if ((bool)swingCheck)
			{
				swingCheck.DamageStop();
			}
		}
		zmb.attacking = false;
	}

	public void Swing()
	{
		swinging = true;
		seekingPlayer = false;
		nma.updateRotation = false;
		base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		nma.enabled = false;
		if (difficulty >= 4 && eid.enemyType == EnemyType.Schism)
		{
			aiming = true;
			predictionLerp = 0f;
			predictionLerping = false;
			anim.SetFloat("AttackType", 0f);
		}
		else if (eid.target.position.y - 5f > base.transform.position.y || eid.target.position.y + 5f < base.transform.position.y)
		{
			anim.SetFloat("AttackType", 1f);
		}
		else
		{
			anim.SetFloat("AttackType", Random.Range(0, 2));
		}
		if (!stationary && zmb.grounded && eid.enemyType == EnemyType.Soldier && difficulty >= 4)
		{
			MoveForward(25f);
			anim.Play("RollShoot", -1, 0f);
		}
		else
		{
			anim.SetTrigger("Swing");
		}
		coolDown = 99f;
	}

	public void SwingEnd()
	{
		swinging = false;
		aiming = false;
		if (zmb.grounded)
		{
			nma.enabled = true;
		}
		coolDown = Random.Range(1f, 2.5f) - coolDownReduce;
		if (wanderer)
		{
			if (difficulty >= 4 && eid.enemyType == EnemyType.Soldier && Random.Range(0f, 1f) > 0.66f)
			{
				chaser = true;
				coolDown = 1f;
			}
			else
			{
				chaser = false;
				Wander();
				coolDown = Mathf.Max(Random.Range(0.5f, 2f) - coolDownReduce, 0.5f);
			}
		}
		if (blocking)
		{
			coolDown = 0f;
		}
		blocking = false;
		moveForward = false;
		if (tr != null)
		{
			tr.enabled = false;
		}
	}

	public void SpawnProjectile()
	{
		if (swinging)
		{
			currentDecProjectile = Object.Instantiate(decProjectile, decProjectileSpawner.transform.position, decProjectileSpawner.transform.rotation);
			currentDecProjectile.transform.SetParent(decProjectileSpawner.transform, worldPositionStays: true);
			currentDecProjectile.GetComponentInChildren<Breakable>().interruptEnemy = eid;
			eid.weakPoint = currentDecProjectile;
		}
	}

	public void DamageStart()
	{
		if (!hasMelee)
		{
			if (tr == null)
			{
				tr = GetComponentInChildren<TrailRenderer>();
			}
			if (tr != null)
			{
				tr.enabled = true;
			}
		}
		zmb.ParryableCheck();
		if (aimer != null && (eid.enemyType != EnemyType.Schism || difficulty >= 4))
		{
			origRotation = aimer.rotation;
			aiming = true;
		}
	}

	public void ThrowProjectile()
	{
		if (currentDecProjectile != null)
		{
			Object.Destroy(currentDecProjectile);
			eid.weakPoint = origWP;
		}
		currentProjectile = Object.Instantiate(projectile, shootPos.position, base.transform.rotation);
		Projectile componentInChildren = currentProjectile.GetComponentInChildren<Projectile>();
		if (componentInChildren != null)
		{
			componentInChildren.target = eid.target;
			componentInChildren.safeEnemyType = EnemyType.Stray;
			if (difficulty > 2)
			{
				componentInChildren.speed *= 1.35f;
			}
			else if (difficulty == 1)
			{
				componentInChildren.speed *= 0.75f;
			}
			else if (difficulty == 0)
			{
				componentInChildren.speed *= 0.5f;
			}
			componentInChildren.damage *= eid.totalDamageModifier;
		}
		Vector3 worldPosition = currentProjectile.transform.position + base.transform.forward;
		EnemyTarget target = eid.target;
		if (target != null && target.isPlayer)
		{
			worldPosition = ((difficulty < 4) ? camObj.transform.position : MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(Vector3.Distance(currentProjectile.transform.position, camObj.transform.position) / (float)((difficulty == 5) ? 90 : Random.Range(110, 180)), aimAtHead: true));
		}
		else if (eid.target != null)
		{
			EnemyIdentifierIdentifier componentInChildren2 = eid.target.targetTransform.GetComponentInChildren<EnemyIdentifierIdentifier>();
			worldPosition = ((!componentInChildren2) ? eid.target.position : componentInChildren2.transform.position);
		}
		currentProjectile.transform.LookAt(worldPosition);
		ProjectileSpread componentInChildren3 = currentProjectile.GetComponentInChildren<ProjectileSpread>();
		if (!componentInChildren3)
		{
			return;
		}
		componentInChildren3.target = eid.target;
		if (difficulty <= 2)
		{
			if (difficulty == 2)
			{
				componentInChildren3.spreadAmount = 5f;
			}
			else if (difficulty == 1)
			{
				componentInChildren3.spreadAmount = 3f;
			}
			else if (difficulty == 0)
			{
				componentInChildren3.spreadAmount = 2f;
			}
			componentInChildren3.projectileAmount = 3;
		}
	}

	public void ShootProjectile(int skipOnEasy)
	{
		if (skipOnEasy > 0 && difficulty < 2)
		{
			return;
		}
		swinging = true;
		if (difficulty >= 4 && eid.enemyType == EnemyType.Schism && !predictionLerping && eid.target != null)
		{
			predictedPosition = (eid.target.isPlayer ? MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(4f, aimAtHead: true, ignoreCollision: true) : eid.target.headPosition);
			if (eid.target.isPlayer)
			{
				predictedPosition.y = eid.target.headPosition.y;
			}
			predictionLerp = 0f;
			predictionLerping = true;
		}
		if (currentDecProjectile != null)
		{
			Object.Destroy(currentDecProjectile);
			eid.weakPoint = origWP;
		}
		currentProjectile = Object.Instantiate(projectile, decProjectileSpawner.transform.position, decProjectileSpawner.transform.rotation);
		Projectile component = currentProjectile.GetComponent<Projectile>();
		component.safeEnemyType = EnemyType.Schism;
		component.target = eid.target;
		if (difficulty > 2)
		{
			component.speed *= 1.25f;
		}
		else if (difficulty == 1)
		{
			component.speed *= 0.75f;
		}
		else if (difficulty == 0)
		{
			component.speed *= 0.5f;
		}
		component.damage *= eid.totalDamageModifier;
	}

	public void StopTracking()
	{
	}

	public void DamageEnd()
	{
		if (!hasMelee && tr != null)
		{
			tr.enabled = false;
		}
		if (currentDecProjectile != null)
		{
			Object.Destroy(currentDecProjectile);
			eid.weakPoint = origWP;
		}
		zmb.attacking = false;
		moveForward = false;
		if (aimer != null)
		{
			aimEase = 0f;
			aiming = false;
		}
	}

	public void CancelAttack()
	{
		swinging = false;
		blocking = false;
		aiming = false;
		coolDown = 0f;
		moveForward = false;
		if (currentDecProjectile != null)
		{
			Object.Destroy(currentDecProjectile);
			eid.weakPoint = origWP;
		}
		if (tr != null)
		{
			tr.enabled = false;
		}
		zmb.attacking = false;
	}

	private void Wander()
	{
		if (nma.isOnNavMesh)
		{
			Vector3 onUnitSphere = Random.onUnitSphere;
			onUnitSphere.y = 0f;
			if (Physics.Raycast(base.transform.position + Vector3.up, onUnitSphere, out var hitInfo, 15f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				wanderTarget = hitInfo.point;
			}
			else if (Physics.Raycast(base.transform.position + Vector3.up + onUnitSphere * 15f, Vector3.down, out hitInfo, 15f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				wanderTarget = hitInfo.point;
			}
			else
			{
				wanderTarget = base.transform.position + onUnitSphere * 15f;
			}
			if (NavMesh.SamplePosition(wanderTarget, out var navMeshHit, 15f, 1))
			{
				wanderTarget = navMeshHit.position;
				SetDestination(navMeshHit.position);
			}
		}
	}

	public void Block(Vector3 attackPosition)
	{
		if (swinging)
		{
			CancelAttack();
		}
		swinging = true;
		blocking = true;
		aiming = false;
		seekingPlayer = false;
		nma.updateRotation = false;
		base.transform.LookAt(new Vector3(attackPosition.x, base.transform.position.y, attackPosition.z));
		zmb.KnockBack(base.transform.forward * -1f * 500f);
		Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.ineffectiveSound, base.transform.position, Quaternion.identity);
		nma.enabled = false;
		anim.Play("Block", -1, 0f);
	}
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Guttertank : MonoBehaviour, IHitTargetCallback
{
	private bool gotValues;

	private EnemyIdentifier eid;

	private NavMeshAgent nma;

	private Machine mach;

	private Rigidbody rb;

	private Animator anim;

	private AudioSource aud;

	private Collider col;

	private int difficulty;

	public bool stationary;

	private Vector3 stationaryPosition;

	private NavMeshPath path;

	private bool walking;

	private Vector3 walkTarget;

	private bool dead;

	[SerializeField]
	private SwingCheck2 sc;

	private bool inAction;

	private bool moveForward;

	private bool trackInAction;

	private bool overrideTarget;

	private bool lookAtTarget;

	private bool punching;

	private Vector3 overrideTargetPosition;

	private float aimRotationLerp;

	private float punchCooldown;

	private bool punchHit;

	public Transform shootPoint;

	public Grenade rocket;

	public GameObject rocketParticle;

	public Transform aimBone;

	private Quaternion torsoDefaultRotation;

	private float shootCooldown = 1f;

	private float lineOfSightTimer;

	public Landmine landmine;

	private float mineCooldown = 2f;

	private List<Landmine> placedMines = new List<Landmine>();

	private GoreZone gz;

	public AudioSource punchPrepSound;

	public AudioSource rocketPrepSound;

	public AudioSource minePrepSound;

	public AudioSource fallImpactSound;

	private void Start()
	{
		GetValues();
	}

	private void GetValues()
	{
		if (!gotValues)
		{
			gotValues = true;
			eid = GetComponent<EnemyIdentifier>();
			nma = GetComponent<NavMeshAgent>();
			mach = GetComponent<Machine>();
			rb = GetComponent<Rigidbody>();
			anim = GetComponent<Animator>();
			aud = GetComponent<AudioSource>();
			col = GetComponent<Collider>();
			shootCooldown = Random.Range(0.75f, 1.25f);
			mineCooldown = Random.Range(2f, 3f);
			stationaryPosition = base.transform.position;
			torsoDefaultRotation = Quaternion.Inverse(base.transform.rotation) * aimBone.rotation;
			path = new NavMeshPath();
			if (eid.difficultyOverride >= 0)
			{
				difficulty = eid.difficultyOverride;
			}
			else
			{
				difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
			}
			gz = GoreZone.ResolveGoreZone(base.transform);
			SetSpeed();
			SlowUpdate();
		}
	}

	private void UpdateBuff()
	{
		SetSpeed();
	}

	private void SetSpeed()
	{
		GetValues();
		if (difficulty >= 3)
		{
			anim.speed = 1f;
		}
		else if (difficulty == 2)
		{
			anim.speed = 0.9f;
		}
		else if (difficulty == 1)
		{
			anim.speed = 0.8f;
		}
		else if (difficulty == 0)
		{
			anim.speed = 0.6f;
		}
		anim.speed *= eid.totalSpeedModifier;
		nma.speed = 20f * anim.speed;
	}

	private void Update()
	{
		if (dead || eid.target == null)
		{
			return;
		}
		if (inAction)
		{
			Vector3 headPosition = eid.target.headPosition;
			if (overrideTarget)
			{
				headPosition = overrideTargetPosition;
			}
			if (trackInAction || moveForward)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(new Vector3(headPosition.x, base.transform.position.y, headPosition.z) - base.transform.position), (float)(trackInAction ? 360 : 90) * Time.deltaTime);
			}
		}
		else
		{
			RaycastHit hitInfo;
			bool flag = !Physics.Raycast(base.transform.position + Vector3.up, eid.target.headPosition - (base.transform.position + Vector3.up), out hitInfo, Vector3.Distance(eid.target.position, base.transform.position + Vector3.up), LayerMaskDefaults.Get(LMD.Environment));
			lineOfSightTimer = Mathf.MoveTowards(lineOfSightTimer, flag ? 1 : 0, Time.deltaTime * eid.totalSpeedModifier);
			if (shootCooldown > 0f)
			{
				shootCooldown = Mathf.MoveTowards(shootCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
			}
			if (mineCooldown > 0f)
			{
				mineCooldown = Mathf.MoveTowards(mineCooldown, 0f, Time.deltaTime * ((lineOfSightTimer >= 0.5f) ? 0.5f : 1f) * eid.totalSpeedModifier);
			}
			if (lineOfSightTimer >= 0.5f)
			{
				if (difficulty <= 1 && Vector3.Distance(base.transform.position, eid.target.position) > 10f && Vector3.Distance(base.transform.position, eid.target.PredictTargetPosition(0.5f)) > 10f)
				{
					punchCooldown = ((difficulty == 1) ? 1 : 2);
				}
				if (punchCooldown <= 0f && (Vector3.Distance(base.transform.position, eid.target.position) < 10f || Vector3.Distance(base.transform.position, eid.target.PredictTargetPosition(0.5f)) < 10f))
				{
					Punch();
				}
				else if (shootCooldown <= 0f && Vector3.Distance(base.transform.position, eid.target.PredictTargetPosition(1f)) > 15f)
				{
					PrepRocket();
				}
			}
			if (!inAction && mineCooldown <= 0f)
			{
				if (CheckMines())
				{
					PrepMine();
				}
				else
				{
					mineCooldown = 0.5f;
				}
			}
		}
		punchCooldown = Mathf.MoveTowards(punchCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		anim.SetBool("Walking", nma.velocity.magnitude > 2.5f);
	}

	private void LateUpdate()
	{
		if (dead || eid.target == null)
		{
			return;
		}
		aimRotationLerp = Mathf.MoveTowards(aimRotationLerp, (inAction && lookAtTarget) ? 1 : 0, Time.deltaTime * 5f);
		if (aimRotationLerp > 0f)
		{
			Vector3 vector = eid.target.headPosition;
			if (overrideTarget)
			{
				vector = overrideTargetPosition;
			}
			if (punching)
			{
				vector = eid.target.position;
			}
			Quaternion quaternion = Quaternion.LookRotation(aimBone.position - vector, Vector3.up);
			Quaternion quaternion2 = Quaternion.Inverse(base.transform.rotation * torsoDefaultRotation) * aimBone.rotation;
			aimBone.rotation = Quaternion.Lerp(aimBone.rotation, quaternion * quaternion2, aimRotationLerp);
			sc.knockBackDirection = aimBone.forward * -1f;
		}
	}

	private void FixedUpdate()
	{
		if (dead || !inAction)
		{
			return;
		}
		rb.isKinematic = !moveForward;
		if (moveForward && !Physics.SphereCast(new Ray(base.transform.position + Vector3.up * 3f, base.transform.forward), 1.5f, 75f * Time.fixedDeltaTime * eid.totalSpeedModifier, LayerMaskDefaults.Get(LMD.Player)))
		{
			if (Physics.Raycast(base.transform.position + Vector3.up + base.transform.forward, Vector3.down, out var _, (eid.target == null) ? 22f : Mathf.Max(22f, base.transform.position.y - eid.target.position.y + 2.5f), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				rb.velocity = base.transform.forward * 75f * anim.speed * eid.totalSpeedModifier;
			}
			else
			{
				rb.velocity = Vector3.zero;
			}
		}
	}

	private void SlowUpdate()
	{
		if (dead)
		{
			return;
		}
		Invoke("SlowUpdate", 0.25f);
		if (eid.target == null)
		{
			return;
		}
		if (!inAction && mach.grounded && nma.isOnNavMesh)
		{
			if (stationary)
			{
				if (!(Vector3.Distance(base.transform.position, stationaryPosition) > 1f))
				{
					return;
				}
				NavMesh.CalculatePath(base.transform.position, stationaryPosition, nma.areaMask, path);
				if (path.status == NavMeshPathStatus.PathComplete)
				{
					nma.path = path;
					return;
				}
			}
			bool flag = false;
			if (Vector3.Distance(base.transform.position, eid.target.position) > 30f || Physics.CheckSphere(aimBone.position - Vector3.up * 0.5f, 1.5f, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)) || Physics.SphereCast(aimBone.position - Vector3.up * 0.5f, 1.5f, eid.target.position + Vector3.up - aimBone.position, out var hitInfo, Vector3.Distance(eid.target.position + Vector3.up, aimBone.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)))
			{
				if ((eid.target.isPlayer && ((MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS && !MonoSingleton<NewMovement>.Instance.gc.onGround) || (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer && !MonoSingleton<PlatformerMovement>.Instance.groundCheck.onGround))) || (eid.target.isEnemy && (!eid.target.enemyIdentifier.gce || !eid.target.enemyIdentifier.gce.onGround)))
				{
					if (Physics.Raycast(eid.target.position, Vector3.down, out hitInfo, 120f, LayerMaskDefaults.Get(LMD.Environment)))
					{
						NavMesh.CalculatePath(base.transform.position, hitInfo.point, nma.areaMask, path);
					}
				}
				else
				{
					NavMesh.CalculatePath(base.transform.position, eid.target.position, nma.areaMask, path);
				}
				if (path.status == NavMeshPathStatus.PathComplete)
				{
					walking = false;
					flag = true;
					nma.path = path;
				}
			}
			if (!walking && !flag)
			{
				Vector3 onUnitSphere = Random.onUnitSphere;
				onUnitSphere = new Vector3(onUnitSphere.x, 0f, onUnitSphere.z);
				RaycastHit hitInfo3;
				if (Physics.Raycast(aimBone.position, onUnitSphere, out var hitInfo2, 25f, LayerMaskDefaults.Get(LMD.Environment)))
				{
					if (NavMesh.SamplePosition(hitInfo2.point, out var hit, 5f, nma.areaMask))
					{
						walkTarget = hit.position;
					}
					else if (Physics.SphereCast(hitInfo2.point, 1f, Vector3.down, out hitInfo2, 25f, LayerMaskDefaults.Get(LMD.Environment)))
					{
						walkTarget = hitInfo2.point;
					}
				}
				else if (Physics.Raycast(aimBone.position + onUnitSphere * 25f, Vector3.down, out hitInfo3, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.Environment)))
				{
					walkTarget = hitInfo3.point;
				}
				NavMesh.CalculatePath(base.transform.position, walkTarget, nma.areaMask, path);
				nma.path = path;
				walking = true;
			}
			else if (Vector3.Distance(base.transform.position, walkTarget) < 1f || nma.path.status != 0)
			{
				walking = false;
			}
		}
		else
		{
			walking = false;
		}
	}

	private bool CheckMines()
	{
		if (placedMines.Count >= 5)
		{
			for (int num = placedMines.Count - 1; num >= 0; num--)
			{
				if (placedMines[num] == null)
				{
					placedMines.RemoveAt(num);
				}
			}
			if (placedMines.Count >= 5)
			{
				return false;
			}
		}
		for (int num2 = MonoSingleton<ObjectTracker>.Instance.landmineList.Count - 1; num2 >= 0; num2--)
		{
			if (MonoSingleton<ObjectTracker>.Instance.landmineList[num2] != null && Vector3.Distance(base.transform.position, MonoSingleton<ObjectTracker>.Instance.landmineList[num2].transform.position) < 15f)
			{
				return false;
			}
		}
		return true;
	}

	private void PrepMine()
	{
		anim.Play("Landmine", 0, 0f);
		Object.Instantiate(minePrepSound, base.transform);
		inAction = true;
		nma.enabled = false;
		lookAtTarget = false;
		mineCooldown = Random.Range(2f, 3f);
	}

	private void PlaceMine()
	{
		Landmine landmine = Object.Instantiate(this.landmine, base.transform.position, base.transform.rotation, gz.transform);
		placedMines.Add(landmine);
		if (landmine.TryGetComponent<Landmine>(out var component))
		{
			component.originEnemy = eid;
		}
	}

	private void PrepRocket()
	{
		anim.Play("Shoot", 0, 0f);
		Object.Instantiate(rocketPrepSound, base.transform);
		inAction = true;
		nma.enabled = false;
		trackInAction = true;
		lookAtTarget = true;
		punching = false;
	}

	private void PredictTarget()
	{
		Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.unparryableFlash, shootPoint.position + base.transform.forward, base.transform.rotation).transform.localScale *= 10f;
		if (eid.target != null)
		{
			overrideTarget = true;
			float num = 1f;
			if (difficulty == 1)
			{
				num = 0.75f;
			}
			else if (difficulty == 0)
			{
				num = 0.5f;
			}
			overrideTargetPosition = eid.target.PredictTargetPosition((Random.Range(0.75f, 1f) + Vector3.Distance(shootPoint.position, eid.target.headPosition) / 150f) * num);
			if (Physics.Raycast(eid.target.position, Vector3.down, 15f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				overrideTargetPosition = new Vector3(overrideTargetPosition.x, eid.target.headPosition.y, overrideTargetPosition.z);
			}
			bool flag = false;
			if (Physics.Raycast(aimBone.position, overrideTargetPosition - aimBone.position, out var hitInfo, Vector3.Distance(overrideTargetPosition, aimBone.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)) && (!hitInfo.transform.TryGetComponent<Breakable>(out var component) || !component.playerOnly))
			{
				flag = true;
				overrideTargetPosition = eid.target.headPosition;
			}
			if (!flag && overrideTargetPosition != eid.target.headPosition && col.Raycast(new Ray(eid.target.headPosition, (overrideTargetPosition - eid.target.headPosition).normalized), out hitInfo, Vector3.Distance(eid.target.headPosition, overrideTargetPosition)))
			{
				overrideTargetPosition = eid.target.headPosition;
			}
		}
	}

	private void FireRocket()
	{
		Object.Instantiate(rocketParticle, shootPoint.position, Quaternion.LookRotation(overrideTargetPosition - shootPoint.position));
		Grenade grenade = Object.Instantiate(rocket, MonoSingleton<WeaponCharges>.Instance.rocketFrozen ? (shootPoint.position + shootPoint.forward * 2.5f) : shootPoint.position, Quaternion.LookRotation(overrideTargetPosition - shootPoint.position));
		grenade.proximityTarget = eid.target;
		grenade.ignoreEnemyType.Add(eid.enemyType);
		grenade.originEnemy = eid;
		if (eid.totalDamageModifier != 1f)
		{
			grenade.totalDamageMultiplier = eid.totalDamageModifier;
		}
		if (difficulty == 1)
		{
			grenade.rocketSpeed *= 0.8f;
		}
		else if (difficulty == 0)
		{
			grenade.rocketSpeed *= 0.6f;
		}
		shootCooldown = Random.Range(1.25f, 1.75f) - ((difficulty >= 4) ? 0.5f : 0f);
	}

	private void Death()
	{
		PunchStop();
		dead = true;
		if (TryGetComponent<Collider>(out var component))
		{
			component.enabled = false;
		}
		if (mach.gc.onGround)
		{
			rb.isKinematic = true;
		}
		else
		{
			rb.constraints = (RigidbodyConstraints)122;
		}
		mach.parryable = false;
		base.enabled = false;
	}

	private void Punch()
	{
		if (difficulty <= 2)
		{
			punchCooldown = 4.5f - (float)difficulty;
		}
		else if (difficulty == 4)
		{
			punchCooldown = 1.5f;
		}
		anim.Play("Punch", 0, 0f);
		Object.Instantiate(punchPrepSound, base.transform);
		Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.unparryableFlash, sc.transform.position + base.transform.forward, base.transform.rotation).transform.localScale *= 5f;
		inAction = true;
		nma.enabled = false;
		trackInAction = true;
		lookAtTarget = true;
		punching = true;
		punchHit = false;
	}

	private void PunchActive()
	{
		sc.DamageStart();
		sc.knockBackDirectionOverride = true;
		sc.knockBackDirection = base.transform.forward;
		moveForward = true;
		trackInAction = false;
	}

	public void TargetBeenHit()
	{
		punchHit = true;
	}

	private void PunchStop()
	{
		sc.DamageStop();
		moveForward = false;
		if (!punchHit || difficulty < 3)
		{
			bool flag = difficulty < 4 && !punchHit;
			if (!flag && (!punchHit || difficulty < 3))
			{
				Vector3Int voxelPosition = StainVoxelManager.WorldToVoxelPosition(base.transform.position + Vector3.down * 1.8333334f);
				flag = MonoSingleton<StainVoxelManager>.Instance.HasProxiesAt(voxelPosition, 3, VoxelCheckingShape.VerticalBox, ProxySearchMode.AnyFloor);
			}
			if (flag)
			{
				anim.Play("PunchStagger");
			}
		}
	}

	private void FallImpact()
	{
		Object.Instantiate(fallImpactSound, new Vector3(eid.weakPoint.transform.position.x, base.transform.position.y, eid.weakPoint.transform.position.z), Quaternion.identity);
		eid.hitter = "";
		eid.DeliverDamage(mach.chest, Vector3.zero, mach.chest.transform.position, 0.1f, tryForExplode: false);
		if (!eid.dead)
		{
			mach.parryable = true;
			Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.parryableFlash, sc.transform.position + base.transform.forward * 5f, base.transform.rotation).transform.localScale *= 10f;
		}
		else
		{
			mach.parryable = false;
			MonoSingleton<StyleHUD>.Instance.AddPoints(50, "SLIPPED");
		}
	}

	private void GotParried()
	{
		if (!eid.dead && (bool)anim)
		{
			anim.Play("PunchStagger", -1, 0.7f);
		}
		mach.parryable = false;
	}

	private void StopParryable()
	{
		mach.parryable = false;
	}

	private void StopAction()
	{
		if (!dead)
		{
			inAction = false;
			nma.enabled = true;
			overrideTarget = false;
			punching = false;
			lookAtTarget = false;
		}
	}
}

using ULTRAKILL.Cheats.UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class Mannequin : MonoBehaviour
{
	private bool gotValues;

	private Animator anim;

	private NavMeshAgent nma;

	private NavMeshPath nmp;

	private Machine mach;

	private EnemyIdentifier eid;

	private Rigidbody rb;

	private SwingCheck2 sc;

	public GameObject bloodSpray;

	private bool skitterMode;

	private float walkSpeed = 22f;

	private float skitterSpeed = 64f;

	private int difficulty;

	public bool inAction;

	public MannequinBehavior behavior;

	public bool dontChangeBehavior;

	public bool dontAutoDrop;

	public bool stationary;

	private Vector3 randomMovementTarget;

	private bool trackTarget;

	private bool moveForward;

	[SerializeField]
	private TrailRenderer[] trails;

	[SerializeField]
	private Transform shootPoint;

	private bool aiming;

	[SerializeField]
	private Transform aimBone;

	private Vector3 aimPoint;

	public Projectile projectile;

	public GameObject chargeProjectile;

	[HideInInspector]
	public GameObject currentChargeProjectile;

	private bool chargingProjectile;

	private float meleeCooldown = 0.5f;

	private float projectileCooldown = 1f;

	private float jumpCooldown = 2f;

	private float meleeBehaviorCancel = 3.5f;

	public bool inControl;

	private bool canCling = true;

	[HideInInspector]
	public bool clinging;

	private Collider clungSurfaceCollider;

	private int attacksWhileClinging;

	private Vector3 clingNormal;

	private Vector3? clungMovementTarget;

	[SerializeField]
	private float clungMovementTolerance = 1.25f;

	private bool firstClingCheck = true;

	public AudioSource clingSound;

	private Collider col;

	[SerializeField]
	private AudioSource skitterSound;

	public string mostRecentAction;

	[HideInInspector]
	public bool jumping;

	private static bool debug => MannequinDebugGizmos.Enabled;

	private void Awake()
	{
		anim = GetComponent<Animator>();
		nma = GetComponent<NavMeshAgent>();
		mach = GetComponent<Machine>();
		eid = GetComponent<EnemyIdentifier>();
		rb = GetComponent<Rigidbody>();
		sc = GetComponentInChildren<SwingCheck2>();
		col = GetComponent<Collider>();
		nmp = new NavMeshPath();
	}

	private void Start()
	{
		GetValues();
		SlowUpdate();
	}

	private void OnEnable()
	{
		CancelActions(changeBehavior: false);
	}

	private void GetValues()
	{
		if (!gotValues)
		{
			gotValues = true;
			if (eid.difficultyOverride >= 0)
			{
				difficulty = eid.difficultyOverride;
			}
			else
			{
				difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
			}
			skitterSound.priority = Random.Range(100, 200);
			SetSpeed();
			if (behavior == MannequinBehavior.Random)
			{
				ChangeBehavior();
			}
		}
	}

	private void UpdateBuff()
	{
		SetSpeed();
	}

	private void SetSpeed()
	{
		GetValues();
		if (difficulty == 0)
		{
			anim.speed = 0.75f;
			walkSpeed = 10f;
			skitterSpeed = 32f;
		}
		else if (difficulty == 1)
		{
			anim.speed = 0.85f;
			walkSpeed = 12f;
			skitterSpeed = 48f;
		}
		else if (difficulty >= 4)
		{
			anim.speed = 1.25f;
			walkSpeed = 20f;
			skitterSpeed = 64f;
		}
		else
		{
			anim.speed = 1f;
			walkSpeed = 16f;
			skitterSpeed = 64f;
		}
		walkSpeed *= eid.totalSpeedModifier;
		skitterSpeed *= eid.totalSpeedModifier;
		anim.speed *= eid.totalSpeedModifier;
		if (difficulty <= 2)
		{
			anim.SetFloat("DifficultyDependentSpeed", 0.66f);
		}
		else
		{
			anim.SetFloat("DifficultyDependentSpeed", 1f);
		}
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 0.1f);
		if (!inAction && eid.target != null)
		{
			if (mach.gc.onGround)
			{
				nma.enabled = true;
			}
			if (nma.enabled && mach.gc.onGround && nma.isOnNavMesh)
			{
				canCling = true;
				if (meleeCooldown <= 0f && Vector3.Distance(eid.target.position, base.transform.position) < 5f)
				{
					MeleeAttack();
				}
				else if (behavior == MannequinBehavior.Melee || Physics.Raycast(eid.overrideCenter.position, eid.target.position - eid.overrideCenter.position, Vector3.Distance(eid.target.position, eid.overrideCenter.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Ignore))
				{
					if (!stationary)
					{
						randomMovementTarget = base.transform.position;
						MoveToTarget(GetTargetPosition(), forceSkitter: true);
					}
				}
				else if (projectileCooldown <= 0f)
				{
					ProjectileAttack();
				}
				else if (!stationary && Vector3.Distance(eid.target.position, base.transform.position) > 50f)
				{
					SetMovementTarget(eid.target.position - base.transform.position, Vector3.Distance(eid.target.position, base.transform.position) - 40f);
				}
				else if (!stationary)
				{
					RaycastHit hitInfo;
					if (behavior == MannequinBehavior.RunAway && Vector3.Distance(eid.target.position, base.transform.position) < 15f)
					{
						SetMovementTarget(base.transform.position - eid.target.position, 20f - Vector3.Distance(eid.target.position, base.transform.position));
					}
					else if (canCling && behavior == MannequinBehavior.Jump && jumpCooldown <= 0f && Physics.Raycast(base.transform.position + Vector3.up, Vector3.up, out hitInfo, 40f, LayerMaskDefaults.Get(LMD.Environment)) && !Physics.Raycast(hitInfo.point - Vector3.up * 3f, eid.target.position - (hitInfo.point - Vector3.up * 3f), Vector3.Distance(eid.target.position, hitInfo.point - Vector3.up * 3f), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Ignore))
					{
						Jump();
					}
					else if (Vector3.Distance(base.transform.position, randomMovementTarget) < 5f)
					{
						SetMovementTarget(Random.onUnitSphere);
					}
					else
					{
						nma.SetDestination(randomMovementTarget);
					}
				}
			}
			else if (clinging)
			{
				if (!stationary && Physics.Raycast(eid.overrideCenter.position, eid.target.position - eid.overrideCenter.position, Vector3.Distance(eid.target.position, eid.overrideCenter.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Ignore))
				{
					inControl = true;
					anim.SetBool("InControl", value: true);
					Uncling();
				}
				else if (projectileCooldown <= 0f && !clungMovementTarget.HasValue)
				{
					ProjectileAttack();
				}
			}
		}
		else
		{
			nma.enabled = false;
		}
	}

	private void Update()
	{
		if (mach.gc.onGround && nma.velocity.magnitude > 3f)
		{
			anim.SetBool("Walking", value: true);
		}
		else
		{
			anim.SetBool("Walking", value: false);
		}
		anim.SetBool("Skittering", skitterMode);
		anim.SetBool("InControl", inControl);
		nma.speed = (skitterMode ? skitterSpeed : walkSpeed);
		if (skitterMode && nma.velocity.magnitude > 3f && !skitterSound.isPlaying)
		{
			skitterSound.pitch = Random.Range(0.9f, 1.1f);
			skitterSound.Play();
			skitterSound.time = Random.Range(0f, skitterSound.clip.length);
		}
		else
		{
			skitterSound.Stop();
		}
		if ((inAction || clinging) && trackTarget && eid.target != null)
		{
			float num = Vector3.Dot(base.transform.up, eid.target.position - base.transform.position);
			Quaternion quaternion = Quaternion.LookRotation(eid.target.position - base.transform.up * num - base.transform.position, base.transform.up);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, Mathf.Max(Quaternion.Angle(base.transform.rotation, quaternion), 10f) * 10f * Time.deltaTime);
		}
		if (meleeCooldown > 0f)
		{
			meleeCooldown = Mathf.MoveTowards(meleeCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (projectileCooldown > 0f)
		{
			projectileCooldown = Mathf.MoveTowards(projectileCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (jumpCooldown > 0f)
		{
			jumpCooldown = Mathf.MoveTowards(jumpCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (behavior == MannequinBehavior.Melee && !inAction && meleeBehaviorCancel > 0f)
		{
			meleeBehaviorCancel = Mathf.MoveTowards(meleeBehaviorCancel, 0f, Time.deltaTime * eid.totalSpeedModifier);
			if (meleeBehaviorCancel <= 0f)
			{
				ChangeBehavior();
			}
		}
		if (((nma.enabled && !inAction && behavior == MannequinBehavior.RunAway) || behavior == MannequinBehavior.Wander) && nma.velocity.magnitude > 2f)
		{
			Vector3 origin = eid.overrideCenter.position + Vector3.up * 0.5f;
			Vector3 normalized = nma.velocity.normalized;
			normalized.y = 0f;
			if (Physics.Raycast(new Ray(origin, normalized), out var hitInfo, 6f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				Ray ray = new Ray(origin, Quaternion.Euler(0f, -90f, 0f) * normalized);
				Ray ray2 = new Ray(origin, Quaternion.Euler(0f, 90f, 0f) * normalized);
				float maxDistance = 2f;
				if (Physics.Raycast(ray, out var hitInfo2, maxDistance, LayerMaskDefaults.Get(LMD.Environment)) || Physics.Raycast(ray2, out hitInfo2, maxDistance, LayerMaskDefaults.Get(LMD.Environment)))
				{
					if (debug)
					{
						Debug.Log("Space too tight, ignoring cling attempt", base.gameObject);
					}
					return;
				}
				clungMovementTarget = null;
				ClingToSurface(hitInfo);
				RelocateWhileClinging(ClungMannequinMovementDirection.Vertical);
				_ = debug;
			}
			_ = debug;
		}
		if (clungMovementTarget.HasValue && clinging && !inAction)
		{
			_ = debug;
			base.transform.position = Vector3.MoveTowards(base.transform.position, clungMovementTarget.Value, 30f * Time.deltaTime * eid.totalSpeedModifier);
			if (Vector3.Distance(base.transform.position, clungMovementTarget.Value) < 0.1f)
			{
				if (debug)
				{
					Debug.Log("Reached clung movement target", base.gameObject);
				}
				clungMovementTarget = null;
				skitterMode = false;
				RaycastHit hitInfo3;
				bool num2 = Physics.Raycast(new Ray(base.transform.position, Vector3.down), out hitInfo3, 3f, LayerMaskDefaults.Get(LMD.Environment));
				_ = debug;
				if (num2)
				{
					if (debug)
					{
						Debug.Log("We've hit the floor while cling walking. Let's jump off", base.gameObject);
					}
					Uncling();
				}
			}
		}
		if (clinging && (clungSurfaceCollider == null || !clungSurfaceCollider.enabled || !clungSurfaceCollider.gameObject.activeInHierarchy))
		{
			Uncling();
		}
	}

	private void FixedUpdate()
	{
		if (inAction && moveForward && !Physics.Raycast(base.transform.position + Vector3.up * 3f, base.transform.forward, 55f * Time.fixedDeltaTime * eid.totalSpeedModifier, LayerMaskDefaults.Get(LMD.Player), QueryTriggerInteraction.Ignore))
		{
			if (Physics.Raycast(base.transform.position + Vector3.up + base.transform.forward, Vector3.down, out var _, (eid.target == null) ? 22f : Mathf.Max(22f, base.transform.position.y - eid.target.position.y + 2.5f), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				rb.velocity = base.transform.forward * 55f * anim.speed * eid.totalSpeedModifier;
			}
			else
			{
				rb.velocity = Vector3.zero;
			}
		}
		if (canCling && !mach.gc.onGround)
		{
			CheckClings();
		}
		anim.SetFloat("ySpeed", rb.isKinematic ? 0f : rb.velocity.y);
	}

	private void LateUpdate()
	{
		if (aiming)
		{
			if (trackTarget && eid.target != null)
			{
				aimPoint = aimBone.position - eid.target.position;
			}
			aimBone.LookAt(aimBone.position + aimPoint, base.transform.up);
		}
	}

	private float EvaluateMaxClingWalkDistance(Vector3 origin, Vector3 movementDirection, Vector3 backToWallDirection, float maxDistance = 20f, float incrementLength = 1.5f)
	{
		float num = 0f;
		Vector3 vector = origin;
		Vector3 vector2 = clingNormal * clungMovementTolerance;
		while (num < maxDistance)
		{
			RaycastHit hitInfo;
			bool flag = Physics.Raycast(new Ray(vector + vector2, backToWallDirection), out hitInfo, clungMovementTolerance * 2f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore);
			_ = debug;
			if (!(Vector3.Angle(hitInfo.normal, clingNormal) < 5f))
			{
				flag = false;
			}
			if (flag)
			{
				bool num2 = Physics.Raycast(new Ray(vector + vector2 - movementDirection.normalized * 0.1f, movementDirection), out hitInfo, incrementLength * 1.25f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore);
				_ = debug;
				if (num2)
				{
					return num - incrementLength * 1.5f;
				}
				num += incrementLength;
				vector += movementDirection * incrementLength;
				continue;
			}
			return num - incrementLength * 1.5f;
		}
		if (num == 0f)
		{
			return 0f;
		}
		return num - incrementLength * 1.5f;
	}

	private void RelocateWhileClinging(ClungMannequinMovementDirection direction)
	{
		Vector3 position = base.transform.position;
		_ = debug;
		Vector3 vector = ((!(Mathf.Abs(Vector3.Dot(clingNormal, Vector3.up)) < 0.99f)) ? Vector3.Cross(clingNormal, Vector3.right).normalized : Vector3.Cross(clingNormal, Vector3.up).normalized);
		Vector3 normalized = Vector3.Cross(clingNormal, vector).normalized;
		Vector3 vector2;
		if (direction == ClungMannequinMovementDirection.Horizontal)
		{
			vector2 = vector;
			_ = debug;
			if (!debug)
			{
			}
		}
		else
		{
			vector2 = normalized;
			_ = debug;
			_ = debug;
		}
		float max = EvaluateMaxClingWalkDistance(position, vector2, -clingNormal);
		_ = debug;
		float num = EvaluateMaxClingWalkDistance(position, -vector2, -clingNormal);
		_ = debug;
		float num2 = Random.Range(0f - num, max);
		if (Mathf.Abs(num2) <= 2f)
		{
			return;
		}
		Vector3 vector3 = position + vector2 * num2;
		if (Physics.Raycast(new Ray(vector3 + clingNormal * clungMovementTolerance, -clingNormal), out var hitInfo, clungMovementTolerance * 2f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			if (debug)
			{
				Debug.Log($"Accounting for bump at target. Distance: {Vector3.Distance(vector3, hitInfo.point)}", base.gameObject);
			}
			vector3 = hitInfo.point;
		}
		_ = debug;
		MoveToTarget(vector3, forceSkitter: true, clungMode: true);
	}

	private void CheckClings()
	{
		RaycastHit hitInfo;
		bool num = Physics.Raycast(base.transform.position, Vector3.up, out hitInfo, firstClingCheck ? 9.5f : 7f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore);
		_ = debug;
		if (num && hitInfo.normal.y <= 0f)
		{
			ClingToSurface(hitInfo);
		}
		else if (firstClingCheck || new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > 3f)
		{
			Collider[] array = Physics.OverlapSphere(col.bounds.center, 2f, LayerMaskDefaults.Get(LMD.Environment));
			_ = debug;
			if (array == null || array.Length == 0)
			{
				return;
			}
			if (Physics.Raycast(col.bounds.center, array[0].ClosestPoint(col.bounds.center) - col.bounds.center, out hitInfo, firstClingCheck ? 3.5f : 2f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				ClingToSurface(hitInfo);
			}
			_ = debug;
		}
		firstClingCheck = false;
	}

	private void ClingToSurface(RaycastHit hit)
	{
		CancelActions();
		Vector3 point = hit.point;
		Vector3 normal = hit.normal;
		canCling = false;
		clinging = true;
		clungSurfaceCollider = hit.collider;
		skitterMode = false;
		mach.gc.ForceOff();
		base.transform.position = point;
		base.transform.up = normal;
		trackTarget = true;
		clingNormal = normal.normalized;
		nma.enabled = false;
		mach.overrideFalling = true;
		rb.isKinematic = true;
		rb.useGravity = false;
		anim.SetBool("Clinging", value: true);
		anim.Play("WallCling");
		if (!firstClingCheck)
		{
			Object.Instantiate(clingSound, base.transform.position, Quaternion.identity);
		}
		projectileCooldown = Random.Range(0f, 0.5f);
	}

	public void Uncling()
	{
		clinging = false;
		clungSurfaceCollider = null;
		CancelActions();
		Vector3 vector = new Vector3(clingNormal.x * 2f, clingNormal.y * 6f, clingNormal.z * 2f);
		if (Mathf.Abs(vector.y) < 6f && Physics.Raycast(new Ray(col.bounds.center, Vector3.up), out var _, 4f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			vector.y = -6f;
		}
		if ((bool)eid && eid.target != null)
		{
			base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		}
		else
		{
			base.transform.LookAt(base.transform.position + clingNormal);
		}
		base.transform.position += vector;
		trackTarget = false;
		Invoke("DelayedGroundCheckReenable", 0.1f);
		jumpCooldown = 2f;
		skitterMode = false;
		attacksWhileClinging = 0;
		mach.overrideFalling = false;
		rb.isKinematic = false;
		rb.useGravity = true;
		if (inControl)
		{
			rb.AddForce(Vector3.down * 50f, ForceMode.VelocityChange);
		}
		anim.SetBool("Clinging", value: false);
	}

	private void MeleeAttack()
	{
		if (!inAction)
		{
			inAction = true;
			mostRecentAction = "Melee Attack";
			meleeCooldown = 2f / eid.totalSpeedModifier;
			nma.enabled = false;
			anim.Play("MeleeAttack");
			trackTarget = true;
		}
	}

	private void ProjectileAttack()
	{
		if (!inAction)
		{
			inAction = true;
			mostRecentAction = "Projectile Attack";
			projectileCooldown = Random.Range(6f - (float)difficulty, 8f - (float)difficulty) / eid.totalSpeedModifier;
			nma.enabled = false;
			anim.Play(clinging ? "WallClingProjectile" : "ProjectileAttack");
			trackTarget = true;
			aiming = true;
			chargingProjectile = true;
			if (clinging)
			{
				attacksWhileClinging++;
			}
		}
	}

	private void Jump()
	{
		if (!inAction)
		{
			inAction = true;
			jumping = true;
			mach.overrideFalling = true;
			skitterMode = false;
			mostRecentAction = "Jump";
			nma.enabled = false;
			jumpCooldown = 2f;
			anim.SetBool("Jump", value: true);
		}
	}

	private void JumpNow()
	{
		mach.gc.ForceOff();
		Invoke("DelayedGroundCheckReenable", 0.1f);
		rb.isKinematic = false;
		rb.useGravity = true;
		rb.AddForce(Vector3.up * 100f, ForceMode.VelocityChange);
		inControl = true;
		skitterMode = false;
		anim.SetBool("Jump", value: false);
		anim.SetBool("InControl", inControl);
	}

	private void MoveToTarget(Vector3 target, bool forceSkitter = false, bool clungMode = false)
	{
		if (clungMode)
		{
			if (debug)
			{
				Debug.Log("Starting clung movement");
			}
			clungMovementTarget = target;
			skitterMode = true;
		}
		else if (!inAction)
		{
			if (NavMesh.SamplePosition(target, out var hit, 15f, nma.areaMask))
			{
				target = hit.position;
			}
			nma.CalculatePath(target, nmp);
			skitterMode = forceSkitter || ((difficulty >= 3 || Random.Range(0f, 1f) > 0.5f) && Vector3.Distance(base.transform.position, target) > 15f);
			nma.path = nmp;
		}
	}

	public void OnDeath()
	{
		if ((bool)currentChargeProjectile)
		{
			Object.Destroy(currentChargeProjectile);
		}
		if (TryGetComponent<KeepInBounds>(out var component))
		{
			Object.Destroy(component);
		}
		skitterSound.Stop();
		sc.DamageStop();
		TrailRenderer[] array = trails;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].emitting = false;
		}
		mach.parryable = false;
		Object.Destroy(this);
	}

	private void StopTracking(int parryable = 0)
	{
		if (eid.target != null)
		{
			base.transform.LookAt(base.transform.position + (new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z) - base.transform.position));
		}
		trackTarget = false;
		if (parryable > 0)
		{
			mach.parryable = true;
			GameObject obj = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.parryableFlash, eid.weakPoint.transform.position + eid.weakPoint.transform.forward * -0.35f, Quaternion.identity);
			obj.transform.LookAt(MonoSingleton<CameraController>.Instance.GetDefaultPos());
			obj.transform.localScale *= 3f;
			obj.transform.SetParent(eid.weakPoint.transform, worldPositionStays: true);
		}
	}

	private void SwingStart(int limb = 0)
	{
		moveForward = true;
		rb.isKinematic = false;
		sc.DamageStart();
		if (limb < trails.Length)
		{
			trails[limb].emitting = true;
		}
	}

	private void SwingEnd(int parryEnd = 0)
	{
		moveForward = false;
		if (eid.gce.onGround)
		{
			rb.isKinematic = true;
		}
		else
		{
			rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
		}
		sc.DamageStop();
		TrailRenderer[] array = trails;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].emitting = false;
		}
		if (parryEnd > 0)
		{
			mach.parryable = false;
		}
	}

	private void ChargeProjectile()
	{
		if ((bool)currentChargeProjectile)
		{
			Object.Destroy(currentChargeProjectile);
		}
		if (chargingProjectile)
		{
			currentChargeProjectile = Object.Instantiate(chargeProjectile, shootPoint.position, shootPoint.rotation);
			currentChargeProjectile.transform.SetParent(shootPoint, worldPositionStays: true);
		}
	}

	private void ShootProjectile()
	{
		if ((bool)currentChargeProjectile)
		{
			Object.Destroy(currentChargeProjectile);
		}
		if (this.projectile == null || this.projectile.Equals(null))
		{
			trackTarget = false;
			chargingProjectile = false;
			return;
		}
		Projectile projectile = Object.Instantiate(this.projectile, shootPoint.position, (eid.target != null) ? Quaternion.LookRotation(eid.target.position - shootPoint.position) : shootPoint.rotation);
		projectile.target = eid.target;
		projectile.safeEnemyType = EnemyType.Mannequin;
		if (difficulty <= 2)
		{
			projectile.turningSpeedMultiplier = 0.75f;
		}
		trackTarget = false;
		chargingProjectile = false;
	}

	public void ChangeBehavior()
	{
		if (!dontChangeBehavior)
		{
			if (Random.Range(0f, 1f) < 0.35f)
			{
				meleeBehaviorCancel = 3.5f;
				behavior = MannequinBehavior.Melee;
			}
			else
			{
				behavior = (MannequinBehavior)Random.Range(2, 5);
			}
		}
		randomMovementTarget = base.transform.position;
	}

	public void ResetMovementTarget()
	{
		randomMovementTarget = base.transform.position;
	}

	private void StopAiming()
	{
		aiming = false;
	}

	public void Landing()
	{
		mach.parryable = false;
		if (difficulty >= 4)
		{
			inControl = true;
		}
		if (!inControl)
		{
			anim.Play("Landing");
			inAction = true;
			mostRecentAction = "Landing";
			inControl = true;
			nma.enabled = false;
			randomMovementTarget = base.transform.position;
		}
	}

	public void StopAction()
	{
		StopAction(changeBehavior: true);
	}

	public void StopAction(bool changeBehavior = true)
	{
		if (clinging && !stationary && !dontAutoDrop && attacksWhileClinging >= ((Random.Range(0f, 1f) > 0.5f) ? 2 : 4))
		{
			attacksWhileClinging = 0;
			inControl = true;
			anim.SetBool("InControl", value: true);
			Uncling();
		}
		if (clinging)
		{
			if (inAction && !jumping)
			{
				bool flag = Random.Range(0f, 1f) > 0.5f;
				RelocateWhileClinging((!flag) ? ClungMannequinMovementDirection.Vertical : ClungMannequinMovementDirection.Horizontal);
			}
		}
		else
		{
			clungMovementTarget = null;
			jumping = false;
			mach.overrideFalling = false;
		}
		trackTarget = clinging;
		aiming = false;
		inAction = false;
		mach.parryable = false;
		moveForward = false;
		chargingProjectile = false;
		if (changeBehavior)
		{
			ChangeBehavior();
		}
	}

	public void CancelActions(bool changeBehavior = true)
	{
		if (moveForward)
		{
			SwingEnd();
		}
		StopAction(changeBehavior);
		if ((bool)currentChargeProjectile)
		{
			Object.Destroy(currentChargeProjectile);
		}
	}

	public void SetMovementTarget(Vector3 direction, float distance = -1f)
	{
		direction.y = 0f;
		if (distance == -1f)
		{
			distance = Random.Range(5f, 25f);
		}
		RaycastHit hitInfo2;
		if (Physics.Raycast(eid.overrideCenter.position, direction, out var hitInfo, distance, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			if (NavMesh.SamplePosition(hitInfo.point, out var hit, 5f, nma.areaMask))
			{
				randomMovementTarget = hit.position;
			}
			else if (Physics.SphereCast(hitInfo.point, 1f, Vector3.down, out hitInfo, distance, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				randomMovementTarget = hitInfo.point;
			}
		}
		else if (Physics.Raycast(eid.overrideCenter.position + direction.normalized * distance, Vector3.down, out hitInfo2, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			randomMovementTarget = hitInfo2.point;
		}
		if ((bool)nma && nma.enabled && nma.isOnNavMesh && mach.gc.onGround)
		{
			MoveToTarget(randomMovementTarget);
		}
	}

	private void DelayedGroundCheckReenable()
	{
		mach.gc.StopForceOff();
		if (jumping)
		{
			jumping = false;
			mach.overrideFalling = false;
			inAction = false;
		}
	}

	private float GetRealDistance(NavMeshPath path)
	{
		if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length <= 1)
		{
			return Vector3.Distance(base.transform.position, GetTargetPosition());
		}
		float num = 0f;
		if (path.corners.Length > 1)
		{
			for (int i = 1; i < path.corners.Length; i++)
			{
				num += Vector3.Distance(path.corners[i - 1], path.corners[i]);
			}
		}
		return num;
	}

	private Vector3 GetTargetPosition()
	{
		if (((eid.target.isPlayer && !MonoSingleton<NewMovement>.Instance.gc.onGround) || (eid.target.isEnemy && (bool)eid.target.enemyIdentifier && (!eid.target.enemyIdentifier.gce || !eid.target.enemyIdentifier.gce.onGround))) && Physics.Raycast(eid.target.position, Vector3.down, out var hitInfo, 200f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
		{
			return hitInfo.point;
		}
		return eid.target.position;
	}
}

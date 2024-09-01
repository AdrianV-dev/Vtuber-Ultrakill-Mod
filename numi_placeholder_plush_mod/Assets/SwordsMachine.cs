using System.Collections.Generic;
using plog;
using Sandbox;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;

public class SwordsMachine : MonoBehaviour, IEnrage, IAlter, IAlterOptions<bool>, IEnemyRelationshipLogic
{
	private static readonly plog.Logger Log = new plog.Logger("SwordsMachine");

	public Transform targetZone;

	private NavMeshAgent nma;

	private Animator anim;

	private Rigidbody rb;

	private Machine mach;

	public float phaseChangeHealth;

	public bool firstPhase;

	public bool active = true;

	public Transform rightArm;

	[SerializeField]
	private Transform[] aimBones;

	private float aimLerp;

	public bool inAction;

	public bool inSemiAction;

	[HideInInspector]
	public bool moveAtTarget;

	private Vector3 moveTarget;

	private float moveSpeed;

	public TrailRenderer swordTrail;

	public TrailRenderer slapTrail;

	public SkinnedMeshRenderer swordMR;

	public Material enragedSword;

	public Material heatMat;

	private Material origMat;

	private AudioSource swordAud;

	public GameObject swingSound;

	public GameObject head;

	public AssetReference flash;

	public AssetReference gunFlash;

	private bool runningAttack = true;

	public float runningAttackCharge;

	public bool damaging;

	public int damage;

	public float runningAttackChance = 50f;

	private EnemyShotgun shotgun;

	private bool shotgunning;

	private bool gunDelay;

	public GameObject shotgunPickUp;

	public GameObject activateOnPhaseChange;

	private bool usingShotgun;

	public Transform secondPhasePosTarget;

	public CheckPoint cpToReset;

	public float swordThrowCharge = 3f;

	public int throwType;

	public GameObject[] thrownSword;

	private GameObject currentThrownSword;

	public Transform handTransform;

	public LayerMask swordThrowMask;

	private float swordThrowChance = 50f;

	private float spiralSwordChance = 50f;

	public float chaseThrowCharge;

	public GameObject bigPainSound;

	private Vector3 targetFuturePos;

	private int difficulty = -1;

	public bool enraged;

	private float rageLeft;

	public EnemySimplifier ensim;

	private float normalAnimSpeed;

	private float normalMovSpeed;

	public GameObject enrageEffect;

	public GameObject currentEnrageEffect;

	private AudioSource enrageAud;

	public Door[] doorsInPath;

	public bool eternalRage;

	public bool bothPhases;

	private bool knockedDown;

	public bool downed;

	[SerializeField]
	private SwingCheck2[] swordSwingCheck;

	[SerializeField]
	private SwingCheck2 slapSwingCheck;

	private GroundCheckEnemy gc;

	private bool bossVersion;

	private EnemyIdentifier eid;

	private BloodsplatterManager bsm;

	private float idleFailsafe = 1f;

	private bool idling;

	private bool inPhaseChange;

	private float moveSpeedMultiplier = 1f;

	private bool breakableInWay;

	private bool targetViewBlocked;

	private bool targetingStalker;

	public float spawnAttackDelay = 0.5f;

	private EnemyTarget target => eid.target;

	public bool isEnraged => enraged;

	public string alterKey => "swordsmachine";

	public string alterCategoryName => "swordsmachine";

	AlterOption<bool>[] IAlterOptions<bool>.options => new AlterOption<bool>[2]
	{
		new AlterOption<bool>
		{
			value = isEnraged,
			callback = delegate(bool value)
			{
				if (value)
				{
					Enrage();
				}
				else
				{
					UnEnrage();
				}
			},
			key = "enraged",
			name = "Enraged"
		},
		new AlterOption<bool>
		{
			value = eternalRage,
			callback = delegate(bool value)
			{
				eternalRage = value;
			},
			key = "eternal-rage",
			name = "Eternal Rage"
		}
	};

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		mach = GetComponent<Machine>();
		if (!eid)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		swordAud = swordTrail.GetComponent<AudioSource>();
		shotgun = GetComponentInChildren<EnemyShotgun>();
		gc = GetComponentInChildren<GroundCheckEnemy>();
		origMat = swordMR.sharedMaterial;
	}

	private void Start()
	{
		swordTrail.emitting = false;
		slapTrail.emitting = false;
		SetSpeed();
		gunDelay = true;
		BossHealthBar component = GetComponent<BossHealthBar>();
		if (component == null || !component.enabled)
		{
			bossVersion = false;
		}
		else
		{
			bossVersion = true;
		}
		Invoke("SlowUpdate", 0.5f);
		Invoke("NavigationUpdate", 0.1f);
	}

	private void UpdateBuff()
	{
		SetSpeed();
	}

	private void SetSpeed()
	{
		if (!nma)
		{
			nma = GetComponent<NavMeshAgent>();
		}
		if (!eid)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		if (!anim)
		{
			anim = GetComponentInChildren<Animator>();
		}
		if (!ensim)
		{
			ensim = GetComponentInChildren<EnemySimplifier>();
		}
		if (difficulty < 0)
		{
			if (eid.difficultyOverride >= 0)
			{
				difficulty = eid.difficultyOverride;
			}
			else
			{
				difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
			}
		}
		if (difficulty != 2)
		{
			if (difficulty >= 3)
			{
				nma.speed = (firstPhase ? 19 : 23);
				anim.speed = 1.2f;
				anim.SetFloat("ThrowSpeedMultiplier", 1.35f);
				anim.SetFloat("AttackSpeedMultiplier", 1f);
				moveSpeedMultiplier = ((difficulty == 3) ? 1.2f : 1.35f);
			}
			else if (difficulty <= 1)
			{
				nma.speed = (firstPhase ? 14 : 18);
				anim.speed = 0.85f;
				if (difficulty == 1)
				{
					anim.SetFloat("ThrowSpeedMultiplier", 0.825f);
					anim.SetFloat("AttackSpeedMultiplier", 0.825f);
					moveSpeedMultiplier = 0.8f;
				}
				else
				{
					anim.SetFloat("ThrowSpeedMultiplier", 0.75f);
					anim.SetFloat("AttackSpeedMultiplier", 0.75f);
					moveSpeedMultiplier = 0.65f;
				}
			}
		}
		else
		{
			nma.speed = (firstPhase ? 16 : 20);
			anim.speed = 1f;
			anim.SetFloat("ThrowSpeedMultiplier", 1f);
			anim.SetFloat("AttackSpeedMultiplier", 1f);
			moveSpeedMultiplier = 1f;
		}
		anim.SetFloat("RecoverySpeedMultiplier", (difficulty < 4) ? 1 : 2);
		nma.speed *= eid.totalSpeedModifier;
		anim.speed *= eid.totalSpeedModifier;
		moveSpeedMultiplier *= eid.totalSpeedModifier;
		normalAnimSpeed = anim.speed;
		normalMovSpeed = nma.speed;
		if (enraged)
		{
			anim.speed = normalAnimSpeed * 1.15f;
			nma.speed = normalMovSpeed * 1.25f;
			ensim.enraged = true;
			if (!eid.puppet)
			{
				swordMR.sharedMaterial = enragedSword;
			}
		}
		if ((bool)shotgun)
		{
			shotgun.UpdateBuffs(eid);
		}
	}

	private void OnDisable()
	{
		if (GetComponent<BossHealthBar>() != null)
		{
			GetComponent<BossHealthBar>().DisappearBar();
		}
		if (currentThrownSword != null)
		{
			Object.Destroy(currentThrownSword);
		}
	}

	private void OnEnable()
	{
		if ((bool)mach)
		{
			StopAction();
			CoolSword();
			StopMoving();
			DamageStop();
		}
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 0.5f);
		targetingStalker = false;
		if (!BlindEnemies.Blind && nma.isOnNavMesh && !eid.sandified)
		{
			List<EnemyIdentifier> enemiesOfType = MonoSingleton<EnemyTracker>.Instance.GetEnemiesOfType(EnemyType.Stalker);
			if (enemiesOfType.Count > 0)
			{
				float num = 100f;
				foreach (EnemyIdentifier item in enemiesOfType)
				{
					if (item.blessed)
					{
						continue;
					}
					NavMeshPath navMeshPath = new NavMeshPath();
					nma.CalculatePath(item.transform.position, navMeshPath);
					if (navMeshPath == null || navMeshPath.status != 0)
					{
						continue;
					}
					float num2 = 0f;
					for (int i = 1; i < navMeshPath.corners.Length; i++)
					{
						num2 += Vector3.Distance(navMeshPath.corners[i - 1], navMeshPath.corners[i]);
					}
					if (!(num2 < num))
					{
						continue;
					}
					eid.target = new EnemyTarget(item.transform);
					targetingStalker = true;
					num = num2;
					if (shotgunning)
					{
						anim.SetLayerWeight(1, 0f);
						shotgunning = false;
						if (!gunDelay)
						{
							gunDelay = true;
							Invoke("ShootDelay", (float)Random.Range(5, 10) / eid.totalSpeedModifier);
						}
					}
				}
			}
		}
		if (target == null)
		{
			return;
		}
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, target.position - base.transform.position, out var hitInfo, Vector3.Distance(base.transform.position + Vector3.up * 0.1f, target.position), LayerMaskDefaults.Get(LMD.Environment)))
		{
			targetViewBlocked = true;
			if (hitInfo.distance < 5f && hitInfo.transform.TryGetComponent<Breakable>(out var component) && !component.playerOnly)
			{
				breakableInWay = true;
			}
		}
		else
		{
			targetViewBlocked = false;
			if (target.position.y > base.transform.position.y + 2.5f && Vector2.Distance(new Vector2(base.transform.position.x, base.transform.position.z), new Vector3(target.position.x, target.position.z)) < 5f && Physics.Raycast(target.position + Vector3.up * 0.1f, Vector3.down, out hitInfo, Mathf.Clamp(target.position.y - base.transform.position.y, 0f, 5f), LayerMaskDefaults.Get(LMD.Environment)) && hitInfo.transform.TryGetComponent<Breakable>(out var component2) && !component2.playerOnly)
			{
				breakableInWay = true;
			}
		}
	}

	private void NavigationUpdate()
	{
		Invoke("NavigationUpdate", 0.1f);
		if (target != null && !inAction && nma.enabled && nma.isOnNavMesh)
		{
			nma.SetDestination(target.position);
		}
	}

	private void Update()
	{
		if (active && nma != null)
		{
			if (spawnAttackDelay > 0f)
			{
				spawnAttackDelay = Mathf.MoveTowards(spawnAttackDelay, 0f, Time.deltaTime * eid.totalSpeedModifier);
			}
			if (breakableInWay && !inAction)
			{
				breakableInWay = false;
				inAction = true;
				RunningSwing();
			}
			else if (target != null && spawnAttackDelay <= 0f)
			{
				if ((firstPhase || bothPhases) && (!enraged || difficulty >= 4) && !inAction && shotgun.gunReady && !gunDelay && !shotgunning && Vector3.Distance(target.position, base.transform.position) > 5f && !targetingStalker)
				{
					shotgunning = true;
					anim.SetLayerWeight(1, 1f);
					anim.SetTrigger("Shoot");
					aimLerp = 0f;
				}
				else if (!firstPhase && (!enraged || difficulty >= 4) && !inAction && !inSemiAction && !targetViewBlocked && ((swordThrowCharge == 0f && Vector3.Distance(target.position, base.transform.position) > 5f) || Vector3.Distance(target.position, base.transform.position) > 20f) && !targetingStalker)
				{
					swordThrowCharge = 3f;
					if ((float)Random.Range(0, 100) <= swordThrowChance || target.position.y > base.transform.position.y + 3f || Vector3.Distance(target.position, base.transform.position) > 16f)
					{
						inAction = true;
						throwType = 2;
						SwordThrow();
						if (swordThrowChance > 50f)
						{
							swordThrowChance = 25f;
						}
						else
						{
							swordThrowChance -= 25f;
						}
					}
					else if (swordThrowChance < 50f)
					{
						swordThrowChance = 75f;
					}
					else
					{
						swordThrowChance += 25f;
					}
				}
				if (runningAttack && !inAction && (!inSemiAction || difficulty >= 4) && Vector3.Distance(target.position, base.transform.position) <= 8f && Vector3.Distance(target.position, base.transform.position) >= 5f)
				{
					runningAttackCharge = 3f;
					if ((float)Random.Range(0, 100) <= runningAttackChance)
					{
						if (runningAttackChance > 50f)
						{
							runningAttackChance = 50f;
						}
						runningAttackChance -= 25f;
						inAction = true;
						RunningSwing();
						if (shotgunning)
						{
							anim.SetLayerWeight(1, 0f);
							shotgunning = false;
							if (!gunDelay)
							{
								gunDelay = true;
								Invoke("ShootDelay", Random.Range(5, 10));
							}
						}
					}
					else
					{
						if (runningAttackChance < 50f)
						{
							runningAttackChance = 50f;
						}
						runningAttackChance += 25f;
						runningAttack = false;
					}
				}
				else if (!inAction && (!inSemiAction || difficulty >= 4) && Vector3.Distance(target.position, base.transform.position) <= 5f)
				{
					inAction = true;
					if (shotgunning)
					{
						anim.SetLayerWeight(1, 0f);
						shotgunning = false;
						if (!gunDelay)
						{
							gunDelay = true;
							Invoke("ShootDelay", (float)Random.Range(5, 10) / eid.totalSpeedModifier);
						}
					}
					if (!firstPhase && !enraged && !targetingStalker)
					{
						if ((float)Random.Range(0, 100) <= spiralSwordChance && !inSemiAction)
						{
							SwordSpiral();
							if (spiralSwordChance > 50f)
							{
								spiralSwordChance = 25f;
							}
							else
							{
								spiralSwordChance -= 25f;
							}
						}
						else
						{
							Combo();
							if (spiralSwordChance < 50f)
							{
								spiralSwordChance = 50f;
							}
							spiralSwordChance += 25f;
						}
					}
					else
					{
						Combo();
					}
				}
				if (!runningAttack && runningAttackCharge > 0f)
				{
					runningAttackCharge -= Time.deltaTime * eid.totalSpeedModifier;
					if (runningAttackCharge <= 0f)
					{
						runningAttackCharge = 0f;
						runningAttack = true;
					}
				}
				if (!firstPhase)
				{
					if (swordThrowCharge > 0f && Vector3.Distance(target.position, base.transform.position) > 5f)
					{
						swordThrowCharge = Mathf.MoveTowards(swordThrowCharge, 0f, Time.deltaTime * eid.totalSpeedModifier);
					}
					else
					{
						swordThrowCharge = 0f;
					}
					if (chaseThrowCharge > 0f)
					{
						chaseThrowCharge = Mathf.MoveTowards(chaseThrowCharge, 0f, Time.deltaTime * eid.totalSpeedModifier);
					}
				}
			}
		}
		if (!inAction && (bool)nma && nma.enabled && nma.isOnNavMesh)
		{
			if (nma.velocity.magnitude > 0.1f)
			{
				anim.SetBool("Running", value: true);
			}
			else
			{
				anim.SetBool("Running", value: false);
			}
		}
		if (!eternalRage && rageLeft > 0f)
		{
			rageLeft = Mathf.MoveTowards(rageLeft, 0f, Time.deltaTime * eid.totalSpeedModifier);
			if (enrageAud != null && rageLeft < 3f)
			{
				enrageAud.pitch = rageLeft / 3f;
			}
			if (rageLeft <= 0f)
			{
				enraged = false;
				ensim.enraged = false;
				if (!eid.puppet)
				{
					swordMR.sharedMaterial = origMat;
				}
				nma.speed = normalMovSpeed;
				anim.speed = normalAnimSpeed;
				if (currentEnrageEffect != null)
				{
					Object.Destroy(currentEnrageEffect);
				}
			}
		}
		if (firstPhase && mach.health <= phaseChangeHealth)
		{
			firstPhase = false;
			phaseChangeHealth = 0f;
			if (bossVersion)
			{
				MonoSingleton<NewMovement>.Instance.ResetHardDamage();
				MonoSingleton<NewMovement>.Instance.GetHealth(999, silent: true);
			}
			EndFirstPhase();
		}
		if (((firstPhase && mach.health < 110f) || bothPhases) && !usingShotgun)
		{
			usingShotgun = true;
			gunDelay = false;
		}
		if (mach.health < 95f)
		{
			gunDelay = false;
		}
		if (idleFailsafe > 0f && (bool)anim && (inAction || !active || knockedDown || downed) && anim.GetCurrentAnimatorClipInfo(0).Length != 0 && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Idle")
		{
			idleFailsafe = Mathf.MoveTowards(idleFailsafe, 0f, Time.deltaTime);
			if (idleFailsafe == 0f)
			{
				StopAction();
				if (knockedDown || downed)
				{
					Disappear();
				}
			}
		}
		else
		{
			idleFailsafe = 1f;
		}
	}

	private void FixedUpdate()
	{
		if (moveAtTarget)
		{
			float y = Mathf.Min(0f, rb.velocity.y);
			if (enraged || Physics.Raycast(base.transform.position + Vector3.up + base.transform.forward, Vector3.down, out var _, Mathf.Max(22f, base.transform.position.y - MonoSingleton<PlayerTracker>.Instance.GetPlayer().position.y + 2.5f), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				rb.velocity = moveTarget * moveSpeed;
			}
			else
			{
				rb.velocity = Vector3.zero;
			}
			rb.velocity = new Vector3(rb.velocity.x, y, rb.velocity.z);
		}
		else
		{
			rb.velocity = new Vector3(0f, Mathf.Min(0f, rb.velocity.y), 0f);
		}
	}

	private void LateUpdate()
	{
		if (!firstPhase && !eternalRage && !bothPhases)
		{
			rightArm.localScale = Vector3.zero;
		}
		if (difficulty < 4 || !usingShotgun || eid.target == null)
		{
			return;
		}
		if (shotgunning)
		{
			aimLerp = Mathf.MoveTowards(aimLerp, 1f, Time.deltaTime * 2f);
		}
		else
		{
			aimLerp = Mathf.MoveTowards(aimLerp, 0f, Time.deltaTime * 8f);
		}
		if (!(aimLerp > 0f))
		{
			return;
		}
		Quaternion[] array = new Quaternion[aimBones.Length];
		for (int i = 0; i < aimBones.Length; i++)
		{
			array[i] = aimBones[i].localRotation;
			aimBones[i].LookAt(eid.target.position);
			if (i == 1)
			{
				aimBones[i].transform.Rotate(Vector3.right * 90f, Space.Self);
			}
			aimBones[i].localRotation = Quaternion.Lerp(array[i], aimBones[i].localRotation, aimLerp);
		}
	}

	public void RunningSwing()
	{
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		anim.SetTrigger("RunningSwing");
		rb.velocity = Vector3.zero;
		moveSpeed = 30f * moveSpeedMultiplier;
		damage = 40;
	}

	private void Combo()
	{
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		anim.SetTrigger("Combo");
		rb.velocity = Vector3.zero;
		moveSpeed = 60f * moveSpeedMultiplier;
		damage = 25;
	}

	private void SwordThrow()
	{
		anim.SetBool("Running", value: false);
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		anim.SetTrigger("SwordThrow");
		rb.velocity = Vector3.zero;
		damage = 0;
	}

	private void SwordSpiral()
	{
		throwType = 1;
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		anim.SetTrigger("SwordSpiral");
		rb.velocity = Vector3.zero;
		damage = 0;
	}

	public void StartMoving()
	{
		if (!knockedDown && !downed && target != null)
		{
			base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
			rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
			rb.isKinematic = false;
			moveTarget = base.transform.forward;
			moveAtTarget = true;
		}
	}

	public void StopMoving()
	{
		moveAtTarget = false;
		rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
		if (gc.onGround)
		{
			rb.isKinematic = true;
		}
	}

	public void LookAt()
	{
		if (target != null)
		{
			base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		}
	}

	public void StopAction()
	{
		mach.parryable = false;
		if (gc.onGround && (bool)nma)
		{
			nma.updatePosition = true;
			nma.updateRotation = true;
			nma.enabled = true;
		}
		StopMoving();
		inAction = false;
		runningAttack = true;
	}

	public void SemiStopAction()
	{
		mach.parryable = false;
		if (gc.onGround && (bool)nma)
		{
			nma.updatePosition = true;
			nma.updateRotation = true;
			nma.enabled = true;
		}
		inSemiAction = true;
		inAction = false;
		anim.SetTrigger("AnimationCancel");
	}

	public void HeatSword()
	{
		if (!inSemiAction)
		{
			swordTrail.emitting = true;
		}
		else
		{
			slapTrail.emitting = true;
		}
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = heatMat;
		}
		swordAud.pitch = 1.5f;
		Object.Instantiate(flash.ToAsset(), head.transform.position + Vector3.up + head.transform.forward, head.transform.rotation, head.transform);
		mach.ParryableCheck();
	}

	public void HeatSwordThrow()
	{
		if ((bool)swordTrail)
		{
			swordTrail.emitting = true;
		}
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = heatMat;
		}
		swordAud.pitch = 1.5f;
		Object.Instantiate(gunFlash.ToAsset(), head.transform);
		if (throwType == 2 && target != null)
		{
			if (target.isPlayer)
			{
				targetFuturePos = target.position + target.GetVelocity() * (Vector3.Distance(base.transform.position, target.position) / 80f) * Vector3.Distance(base.transform.position, target.position) * 0.08f / anim.speed;
			}
			else
			{
				targetFuturePos = target.position + target.GetVelocity();
			}
			base.transform.LookAt(new Vector3(targetFuturePos.x, base.transform.position.y, targetFuturePos.z));
		}
		mach.ParryableCheck();
	}

	public void CoolSword()
	{
		if ((bool)swordTrail)
		{
			swordTrail.emitting = false;
		}
		if ((bool)slapTrail)
		{
			slapTrail.emitting = false;
		}
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = (enraged ? enragedSword : origMat);
		}
		swordAud.pitch = 1f;
	}

	public void DamageStart()
	{
		damaging = true;
		if (!inSemiAction)
		{
			if ((bool)swordTrail)
			{
				Object.Instantiate(swingSound, swordTrail.transform);
			}
			SwingCheck2[] array = swordSwingCheck;
			foreach (SwingCheck2 obj in array)
			{
				obj.OverrideEnemyIdentifier(eid);
				obj.damage = damage;
				obj.DamageStart();
			}
		}
		else
		{
			slapSwingCheck.OverrideEnemyIdentifier(eid);
			slapSwingCheck.DamageStart();
		}
	}

	public void DamageStop()
	{
		damaging = false;
		mach.parryable = false;
		SwingCheck2[] array = swordSwingCheck;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DamageStop();
		}
		slapSwingCheck.DamageStop();
	}

	public void ShootGun()
	{
		if (!inAction)
		{
			shotgun.UpdateTarget(target);
			shotgun.Fire();
		}
	}

	public void StopShootAnimation()
	{
		mach.parryable = false;
		anim.SetLayerWeight(1, 0f);
		gunDelay = true;
		shotgunning = false;
		Invoke("ShootDelay", (float)Random.Range(5, 20) / eid.totalSpeedModifier);
	}

	private void ShootDelay()
	{
		gunDelay = false;
	}

	public void FlashGun()
	{
		Object.Instantiate(gunFlash.ToAsset(), head.transform.position + Vector3.up + head.transform.forward, head.transform.rotation, head.transform);
	}

	public void SwordSpawn()
	{
		mach.parryable = false;
		if (target == null)
		{
			return;
		}
		RaycastHit hitInfo;
		if (throwType != 2)
		{
			targetFuturePos = target.position;
		}
		else if (target.isPlayer)
		{
			targetFuturePos = new Vector3(targetFuturePos.x, target.position.y + MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity().y * Vector3.Distance(base.transform.position, target.position) * 0.01f, targetFuturePos.z);
			if (Physics.Raycast(target.position, targetFuturePos - target.position, out hitInfo, Vector3.Distance(target.position, targetFuturePos), swordThrowMask, QueryTriggerInteraction.Ignore))
			{
				targetFuturePos = hitInfo.point;
			}
		}
		else
		{
			targetFuturePos = target.position + target.GetVelocity() / eid.totalSpeedModifier;
		}
		base.transform.LookAt(new Vector3(targetFuturePos.x, base.transform.position.y, targetFuturePos.z));
		currentThrownSword = Object.Instantiate(thrownSword[throwType], new Vector3(base.transform.position.x, handTransform.position.y, base.transform.position.z), Quaternion.identity);
		ThrownSword componentInChildren = currentThrownSword.GetComponentInChildren<ThrownSword>();
		componentInChildren.thrownBy = eid;
		if (throwType != 1)
		{
			currentThrownSword.transform.rotation = base.transform.rotation;
		}
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = origMat;
		}
		swordMR.enabled = false;
		swordTrail.emitting = false;
		slapTrail.emitting = false;
		swordAud.pitch = 0f;
		if (Physics.Raycast(base.transform.position + Vector3.up * 2f, (targetFuturePos - base.transform.position).normalized, out hitInfo, float.PositiveInfinity, swordThrowMask))
		{
			componentInChildren.SetPoints(hitInfo.point, handTransform);
		}
		else
		{
			componentInChildren.thrownAtVoid = true;
			componentInChildren.SetPoints((targetFuturePos - base.transform.position) * 9999f, handTransform);
		}
		if (throwType == 2)
		{
			SemiStopAction();
		}
		Invoke("SwordCatch", 5f);
	}

	public void SwordCatch()
	{
		mach.parryable = false;
		if ((bool)currentThrownSword)
		{
			Object.Destroy(currentThrownSword);
		}
		if (!knockedDown && !downed)
		{
			inAction = true;
			anim.SetTrigger("SwordCatch");
		}
		inSemiAction = false;
		swordMR.enabled = true;
		swordAud.pitch = 1f;
		swordThrowCharge = 3f;
		CancelInvoke("SwordCatch");
	}

	private void EndFirstPhase()
	{
		DamageStop();
		knockedDown = true;
		inAction = true;
		inSemiAction = false;
		anim.SetLayerWeight(1, 0f);
		gunDelay = true;
		shotgunning = false;
		swordTrail.emitting = false;
		slapTrail.emitting = false;
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = origMat;
		}
		swordAud.pitch = 1f;
		nma.enabled = true;
		inPhaseChange = true;
		active = false;
		moveAtTarget = false;
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		if (target != null)
		{
			base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		}
		rb.velocity = Vector3.zero;
		if (gc.onGround)
		{
			rb.isKinematic = true;
		}
		else
		{
			rb.isKinematic = false;
		}
		if (!bsm)
		{
			bsm = MonoSingleton<BloodsplatterManager>.Instance;
		}
		GameObject gore = bsm.GetGore(GoreType.Limb, eid);
		if ((bool)gore)
		{
			gore.transform.position = rightArm.position;
		}
		if (bossVersion && shotgunPickUp != null)
		{
			shotgunPickUp.transform.SetPositionAndRotation(shotgun.transform.position, shotgun.transform.rotation);
			shotgunPickUp.SetActive(value: true);
		}
		CharacterJoint[] componentsInChildren = rightArm.GetComponentsInChildren<CharacterJoint>();
		GetComponentInParent<GoreZone>();
		if (componentsInChildren.Length != 0)
		{
			CharacterJoint[] array = componentsInChildren;
			foreach (CharacterJoint obj in array)
			{
				Object.Destroy(obj);
				obj.transform.localScale = Vector3.zero;
				obj.gameObject.SetActive(value: false);
			}
		}
		anim.Rebind();
		SetSpeed();
		anim.SetTrigger("Knockdown");
		if (bossVersion)
		{
			MonoSingleton<TimeController>.Instance.SlowDown(0.15f);
		}
		Object.Instantiate(bigPainSound, base.transform);
		if (secondPhasePosTarget != null)
		{
			MonoSingleton<MusicManager>.Instance.ArenaMusicEnd();
			MonoSingleton<MusicManager>.Instance.PlayCleanMusic();
		}
		normalMovSpeed = nma.speed;
		rageLeft = 0.01f;
	}

	public void Knockdown(bool light = false, bool fromExplosion = false)
	{
		DamageStop();
		knockedDown = true;
		inAction = true;
		inSemiAction = false;
		anim.SetLayerWeight(1, 0f);
		gunDelay = true;
		shotgunning = false;
		swordMR.enabled = true;
		swordTrail.emitting = false;
		slapTrail.emitting = false;
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = origMat;
		}
		swordAud.pitch = 1f;
		nma.enabled = true;
		SetSpeed();
		moveAtTarget = false;
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		rb.velocity = Vector3.zero;
		if (gc.onGround)
		{
			rb.isKinematic = true;
		}
		else
		{
			rb.isKinematic = false;
		}
		moveAtTarget = false;
		if (light)
		{
			anim.Play("LightKnockdown");
		}
		else
		{
			anim.Play("Knockdown");
		}
		if (mach == null)
		{
			mach = GetComponent<Machine>();
		}
		if (!light)
		{
			GetComponent<EnemyIdentifier>().hitter = "projectile";
			if (mach.health > 20f)
			{
				mach.GetHurt(GetComponentInChildren<EnemyIdentifierIdentifier>().gameObject, Vector3.zero, 20f, 0f);
			}
			else
			{
				mach.GetHurt(GetComponentInChildren<EnemyIdentifierIdentifier>().gameObject, Vector3.zero, mach.health - 0.1f, 0f);
			}
		}
		if (!bsm)
		{
			bsm = MonoSingleton<BloodsplatterManager>.Instance;
		}
		GameObject gore = bsm.GetGore(GoreType.Head, eid, fromExplosion);
		gore.transform.position = GetComponentInChildren<EnemyIdentifierIdentifier>().transform.position;
		gore.GetComponent<Bloodsplatter>()?.GetReady();
		gore.GetComponent<ParticleSystem>()?.Play();
		if (!light)
		{
			Object.Instantiate(bigPainSound, base.transform);
		}
		Enrage();
	}

	public void Down(bool fromExplosion = false)
	{
		downed = true;
		DamageStop();
		inAction = true;
		inSemiAction = false;
		anim.SetLayerWeight(1, 0f);
		gunDelay = true;
		shotgunning = false;
		swordMR.enabled = true;
		swordTrail.emitting = false;
		slapTrail.emitting = false;
		if (!eid.puppet)
		{
			swordMR.sharedMaterial = origMat;
		}
		swordAud.pitch = 1f;
		nma.enabled = true;
		SetSpeed();
		moveAtTarget = false;
		nma.updatePosition = false;
		nma.updateRotation = false;
		nma.enabled = false;
		base.transform.LookAt(new Vector3(target.position.x, base.transform.position.y, target.position.z));
		rb.velocity = Vector3.zero;
		if (gc.onGround)
		{
			rb.isKinematic = true;
		}
		else
		{
			rb.isKinematic = false;
		}
		moveAtTarget = false;
		anim.Play("Knockdown");
		Invoke("CheckLoop", 0.5f);
		if (mach == null)
		{
			mach = GetComponent<Machine>();
		}
		if (!bsm)
		{
			bsm = MonoSingleton<BloodsplatterManager>.Instance;
		}
		GameObject gore = bsm.GetGore(GoreType.Head, eid, fromExplosion);
		gore.transform.position = GetComponentInChildren<EnemyIdentifierIdentifier>().transform.position;
		gore.GetComponent<Bloodsplatter>()?.GetReady();
		gore.GetComponent<ParticleSystem>()?.Play();
		Object.Instantiate(bigPainSound, base.transform);
	}

	public void Disappear()
	{
		if (secondPhasePosTarget != null && !firstPhase)
		{
			BossHealthBar component = GetComponent<BossHealthBar>();
			component.DisappearBar();
			Object.Instantiate(position: new Vector3(base.transform.position.x, base.transform.position.y + 1.5f, base.transform.position.z), original: eid.spawnEffect, rotation: base.transform.rotation);
			base.gameObject.SetActive(value: false);
			SwordsMachine[] componentsInChildren = secondPhasePosTarget.GetComponentsInChildren<SwordsMachine>();
			if (componentsInChildren.Length != 0)
			{
				SwordsMachine[] array = componentsInChildren;
				foreach (SwordsMachine obj in array)
				{
					obj.gameObject.SetActive(value: false);
					Object.Destroy(obj.gameObject);
				}
			}
			nma.updatePosition = true;
			nma.updateRotation = true;
			nma.enabled = true;
			base.transform.position = secondPhasePosTarget.position;
			base.transform.parent = secondPhasePosTarget;
			eid.spawnIn = true;
			base.gameObject.SetActive(value: true);
			component.enabled = true;
		}
		knockedDown = false;
		moveAtTarget = false;
		if (gc.onGround)
		{
			rb.isKinematic = true;
		}
		inPhaseChange = false;
		active = true;
		if (gc.onGround)
		{
			nma.updatePosition = true;
			nma.updateRotation = true;
			nma.enabled = true;
		}
		inAction = false;
		inSemiAction = false;
		if (activateOnPhaseChange != null && !firstPhase)
		{
			activateOnPhaseChange.SetActive(value: true);
		}
		GetComponent<AudioSource>().volume = 0f;
		if (secondPhasePosTarget != null && !firstPhase)
		{
			secondPhasePosTarget = null;
			cpToReset.UpdateRooms();
		}
	}

	public void Enrage()
	{
		if (!enraged && !bothPhases)
		{
			enraged = true;
			rageLeft = 10f;
			anim.speed = normalAnimSpeed * 1.15f;
			nma.speed = normalMovSpeed * 1.25f;
			ensim.enraged = true;
			if (!eid.puppet)
			{
				swordMR.sharedMaterial = enragedSword;
			}
			Object.Instantiate(bigPainSound, base.transform).GetComponent<AudioSource>().pitch = 2f;
			if (currentEnrageEffect == null)
			{
				currentEnrageEffect = Object.Instantiate(enrageEffect, mach.chest.transform);
				enrageAud = currentEnrageEffect.GetComponent<AudioSource>();
			}
			enrageAud.pitch = 1f;
		}
	}

	public void UnEnrage()
	{
		if (enraged)
		{
			rageLeft = 0f;
			anim.speed = normalAnimSpeed;
			nma.speed = normalMovSpeed;
			ensim.enraged = false;
			if (!eid.puppet)
			{
				swordMR.sharedMaterial = origMat;
			}
			enraged = false;
			Object.Destroy(currentEnrageEffect);
		}
	}

	public void CheckLoop()
	{
		if (downed)
		{
			anim.Play("Knockdown", 0, 0.25f);
			Invoke("CheckLoop", 0.25f);
		}
	}

	public bool ShouldAttackEnemies()
	{
		return false;
	}

	public bool ShouldIgnorePlayer()
	{
		if (target != null && target.isEnemy)
		{
			return target.enemyIdentifier.enemyType == EnemyType.Stalker;
		}
		return false;
	}
}

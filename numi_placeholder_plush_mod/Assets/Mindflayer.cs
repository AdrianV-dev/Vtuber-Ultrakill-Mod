using Sandbox;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Mindflayer : MonoBehaviour, IEnrage, IAlter, IAlterOptions<bool>
{
	private Animator anim;

	private float defaultAnimSpeed = 1f;

	[HideInInspector]
	public bool active = true;

	public Transform model;

	public GameObject homingProjectile;

	public GameObject decorativeProjectile;

	public GameObject warningFlash;

	public GameObject warningFlashUnparriable;

	public GameObject decoy;

	public Transform[] tentacles;

	private SwingCheck2 sc;

	public float cooldown;

	private bool inAction;

	private bool overrideRotation;

	private Vector3 overrideTarget;

	private bool dontTeleport;

	private EnemyIdentifier eid;

	private Machine mach;

	private LayerMask environmentMask;

	private float decoyThreshold;

	private int teleportAttempts;

	private int teleportInterval = 6;

	public GameObject bigHurt;

	public GameObject windUp;

	public GameObject windUpSmall;

	public GameObject teleportSound;

	private bool goingLeft;

	private bool goForward;

	private Rigidbody rb;

	private bool beaming;

	private bool beamCooldown = true;

	private bool beamNext;

	public GameObject beam;

	[HideInInspector]
	public GameObject tempBeam;

	public Transform rightHand;

	private float beamDistance;

	private LineRenderer lr;

	private float outOfSightTime;

	public AssetReference deathExplosion;

	public ParticleSystem chargeParticle;

	private bool vibrate;

	private Vector3 origPos;

	private float timeSinceMelee;

	private float spawnAttackDelay = 1f;

	private int difficulty = -1;

	private float cooldownMultiplier;

	private bool enraged;

	public GameObject enrageEffect;

	private GameObject currentEnrageEffect;

	private EnemySimplifier[] ensims;

	public GameObject originalGlow;

	public GameObject enrageGlow;

	public Gradient originalTentacleGradient;

	[HideInInspector]
	public bool dying;

	private bool launched;

	private Collider[] ownColliders;

	private EnemyTarget target => eid.target;

	public bool isEnraged => enraged;

	public string alterKey => "mindflayer";

	public string alterCategoryName => "mindflayer";

	public AlterOption<bool>[] options => new AlterOption<bool>[1]
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
		}
	};

	private void Awake()
	{
		mach = GetComponent<Machine>();
		rb = GetComponent<Rigidbody>();
		anim = GetComponent<Animator>();
		eid = GetComponent<EnemyIdentifier>();
	}

	private void Start()
	{
		cooldown = 2f;
		decoyThreshold = mach.health - (float)teleportInterval;
		environmentMask = (int)environmentMask | 0x100;
		environmentMask = (int)environmentMask | 0x1000000;
		sc = GetComponentInChildren<SwingCheck2>();
		lr = GetComponent<LineRenderer>();
		lr.enabled = false;
		if (tempBeam != null)
		{
			Object.Destroy(tempBeam);
		}
		RandomizeDirection();
		SetSpeed();
		if (dying)
		{
			Death();
		}
	}

	private void UpdateBuff()
	{
		SetSpeed();
	}

	private void SetSpeed()
	{
		if (!anim)
		{
			anim = GetComponent<Animator>();
		}
		if (!eid)
		{
			eid = GetComponent<EnemyIdentifier>();
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
		eid.immuneToFriendlyFire = difficulty >= 4;
		if (difficulty >= 4)
		{
			cooldownMultiplier = 2.5f;
			anim.speed = 1.5f;
		}
		else if (difficulty == 3)
		{
			cooldownMultiplier = 1.5f;
			anim.speed = 1.35f;
		}
		else if (difficulty < 2)
		{
			cooldownMultiplier = 0.75f;
			if (difficulty == 1)
			{
				anim.speed = 0.75f;
			}
			else if (difficulty == 0)
			{
				anim.speed = 0.5f;
			}
		}
		else
		{
			cooldownMultiplier = 1f;
			anim.speed = 1f;
		}
		cooldownMultiplier *= eid.totalSpeedModifier;
		anim.speed *= eid.totalSpeedModifier;
		defaultAnimSpeed = anim.speed;
	}

	private void OnDisable()
	{
		StopAction();
		if ((bool)sc)
		{
			DamageEnd();
		}
		if (tempBeam != null)
		{
			Object.Destroy(tempBeam);
		}
		chargeParticle.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmitting);
		overrideRotation = false;
	}

	private void UpdateRigidbodySettings()
	{
		if (target == null)
		{
			rb.drag = 3f;
			rb.angularDrag = 3f;
		}
		else
		{
			rb.drag = 0f;
			rb.angularDrag = 0f;
		}
	}

	private void Update()
	{
		UpdateRigidbodySettings();
		if (vibrate)
		{
			model.localPosition = new Vector3(origPos.x + Random.Range(-0.2f, 0.2f), origPos.y + Random.Range(-0.2f, 0.2f), origPos.z + Random.Range(-0.2f, 0.2f));
		}
		if (launched)
		{
			model.Rotate(Vector3.right, -1200f * Time.deltaTime, Space.Self);
		}
		if (target == null)
		{
			if (beaming)
			{
				StopBeam();
			}
			if (inAction)
			{
				StopAction();
			}
		}
		else
		{
			if (!active)
			{
				return;
			}
			bool flag = Vector3.Distance(base.transform.position, target.position) > 25f || base.transform.position.y > target.position.y + 15f || Physics.Raycast(base.transform.position, target.position - base.transform.position, Vector3.Distance(base.transform.position, target.position), environmentMask);
			if (spawnAttackDelay > 0f)
			{
				spawnAttackDelay = Mathf.MoveTowards(spawnAttackDelay, 0f, Time.deltaTime * eid.totalSpeedModifier);
			}
			else if (Vector3.Distance(target.position, base.transform.position) < 5f && !inAction)
			{
				MeleeAttack();
			}
			timeSinceMelee += Time.deltaTime * eid.totalSpeedModifier;
			if (((difficulty > 2 && timeSinceMelee > 10f) || (difficulty == 2 && timeSinceMelee > 15f)) && !inAction)
			{
				Teleport(closeRange: true);
				timeSinceMelee = 5f;
				if (Vector3.Distance(target.position, base.transform.position) < 8f)
				{
					MeleeAttack();
				}
			}
			if (cooldown > 0f)
			{
				cooldown = Mathf.MoveTowards(cooldown, 0f, Time.deltaTime * cooldownMultiplier);
			}
			else if (!inAction && !flag)
			{
				if (beamCooldown || (Random.Range(0f, 1f) < 0.25f && !beamNext))
				{
					if (!beamCooldown)
					{
						beamNext = true;
					}
					beamCooldown = false;
					HomingAttack();
				}
				else
				{
					BeamAttack();
				}
			}
			if (flag)
			{
				outOfSightTime = Mathf.MoveTowards(outOfSightTime, 3f, Time.deltaTime * eid.totalSpeedModifier);
				if (outOfSightTime >= 3f && !inAction)
				{
					Teleport();
				}
			}
			else
			{
				outOfSightTime = Mathf.MoveTowards(outOfSightTime, 0f, Time.deltaTime * 2f * eid.totalSpeedModifier);
			}
			if (!overrideRotation)
			{
				Quaternion quaternion = Quaternion.LookRotation(target.position - base.transform.position, Vector3.up);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, Time.deltaTime * (10f * Quaternion.Angle(quaternion, base.transform.rotation) + 2f) * eid.totalSpeedModifier);
			}
			else
			{
				Quaternion quaternion2 = Quaternion.LookRotation(overrideTarget - base.transform.position, Vector3.up);
				if (!beaming)
				{
					base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion2, Time.deltaTime * (100f * Quaternion.Angle(quaternion2, base.transform.rotation) + 10f) * eid.totalSpeedModifier);
				}
				else
				{
					float num = 1f;
					if (difficulty == 1)
					{
						num = 0.85f;
					}
					else if (difficulty == 0)
					{
						num = 0.65f;
					}
					base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion2, Time.deltaTime * beamDistance * num * eid.totalSpeedModifier);
					if (Quaternion.Angle(base.transform.rotation, quaternion2) < 1f)
					{
						StopBeam();
					}
				}
			}
			if (decoyThreshold > mach.health && decoyThreshold > 0f && !dontTeleport)
			{
				Object.Instantiate(bigHurt, base.transform.position, Quaternion.identity);
				while (decoyThreshold > mach.health)
				{
					decoyThreshold -= teleportInterval;
				}
				Teleport();
			}
			if (difficulty > 2 && mach.health < 15f && !enraged)
			{
				Enrage();
			}
		}
	}

	private void FixedUpdate()
	{
		if (launched)
		{
			RaycastHit[] array = Physics.SphereCastAll(base.transform.position, 1f, base.transform.forward * -1f, 50f * Time.fixedDeltaTime, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment));
			bool flag = false;
			bool flag2 = false;
			RaycastHit[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit raycastHit = array2[i];
				if (raycastHit.collider.gameObject.layer == 8 || raycastHit.collider.gameObject.layer == 24)
				{
					flag = true;
					break;
				}
				Collider[] array3 = ownColliders;
				foreach (Collider collider in array3)
				{
					if (raycastHit.collider == collider)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = true;
					if (raycastHit.collider.TryGetComponent<EnemyIdentifierIdentifier>(out var component) && (bool)component.eid && !component.eid.dead)
					{
						MonoSingleton<StyleHUD>.Instance.AddPoints(100, "ultrakill.strike", null, eid);
						if (component.eid.enemyType == EnemyType.Gutterman && component.eid.TryGetComponent<Gutterman>(out var component2) && component2.hasShield)
						{
							component2.ShieldBreak(player: true, flash: false);
						}
					}
					break;
				}
				flag2 = false;
			}
			if (flag)
			{
				CancelInvoke("DeathExplosion");
				DeathExplosion();
			}
			else
			{
				rb.MovePosition(base.transform.position - base.transform.forward * 50f * Time.fixedDeltaTime);
			}
		}
		if (target == null)
		{
			return;
		}
		if (!inAction)
		{
			if (goingLeft)
			{
				if (!Physics.Raycast(base.transform.position, base.transform.right * -1f, 1f, environmentMask))
				{
					rb.MovePosition(base.transform.position + base.transform.right * -5f * Time.fixedDeltaTime * anim.speed);
				}
				else
				{
					goingLeft = false;
				}
			}
			else if (!Physics.Raycast(base.transform.position, base.transform.right, 1f, environmentMask))
			{
				rb.MovePosition(base.transform.position + base.transform.right * 5f * Time.fixedDeltaTime * anim.speed);
			}
			else
			{
				goingLeft = true;
			}
		}
		else if (goForward && !Physics.Raycast(base.transform.position, base.transform.forward, 1f, environmentMask))
		{
			rb.MovePosition(base.transform.position + base.transform.forward * 75f * Time.fixedDeltaTime * anim.speed);
		}
	}

	private void RandomizeDirection()
	{
		if (Random.Range(0f, 1f) > 0.5f)
		{
			goingLeft = true;
		}
		else
		{
			goingLeft = false;
		}
	}

	public void Teleport(bool closeRange = false)
	{
		outOfSightTime = 0f;
		if ((bool)eid && eid.drillers.Count > 0)
		{
			return;
		}
		if (teleportAttempts == 0)
		{
			GameObject gameObject = Object.Instantiate(decoy, base.transform.GetChild(0).position, base.transform.GetChild(0).rotation);
			Animator componentInChildren = gameObject.GetComponentInChildren<Animator>();
			AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(0);
			componentInChildren.Play(currentAnimatorStateInfo.shortNameHash, 0, currentAnimatorStateInfo.normalizedTime);
			componentInChildren.speed = 0f;
			if (enraged)
			{
				gameObject.GetComponent<MindflayerDecoy>().enraged = true;
			}
		}
		Vector3 normalized = Random.onUnitSphere.normalized;
		if (normalized.y < 0f)
		{
			normalized.y *= -1f;
		}
		float num = Random.Range(8, 15);
		if (closeRange)
		{
			num = Random.Range(5, 8);
		}
		Vector3 vector = target.position + Vector3.up;
		vector = ((!Physics.Raycast(target.position + Vector3.up, normalized, out var hitInfo, num, environmentMask, QueryTriggerInteraction.Ignore)) ? (target.position + Vector3.up + normalized * num) : (hitInfo.point - normalized * 3f));
		bool flag = false;
		bool flag2 = false;
		if (Physics.Raycast(vector, Vector3.up, out var hitInfo2, 5f, environmentMask, QueryTriggerInteraction.Ignore))
		{
			flag = true;
		}
		if (Physics.Raycast(vector, Vector3.down, out var hitInfo3, 5f, environmentMask, QueryTriggerInteraction.Ignore))
		{
			flag2 = true;
		}
		bool flag3 = false;
		Vector3 position = base.transform.position;
		if (flag && flag2)
		{
			if (Vector3.Distance(hitInfo2.point, hitInfo3.point) > 7f)
			{
				position = new Vector3(vector.x, (hitInfo3.point.y + hitInfo2.point.y) / 2f, vector.z);
				flag3 = true;
			}
			else
			{
				teleportAttempts++;
				if (teleportAttempts <= 10)
				{
					Teleport();
				}
			}
		}
		else
		{
			flag3 = true;
			position = (flag ? (hitInfo2.point + Vector3.down * Random.Range(5, 10)) : ((!flag2) ? vector : (hitInfo3.point + Vector3.up * Random.Range(5, 10))));
		}
		if (flag3)
		{
			if (Physics.CheckSphere(position, 0.1f, environmentMask, QueryTriggerInteraction.Ignore))
			{
				Teleport();
				return;
			}
			if (eid.hooked)
			{
				MonoSingleton<HookArm>.Instance.StopThrow(1f, sparks: true);
			}
			base.transform.position = position;
			teleportAttempts = 0;
			Object.Instantiate(teleportSound, base.transform.position, Quaternion.identity);
		}
		if (goingLeft)
		{
			goingLeft = false;
		}
		else
		{
			goingLeft = true;
		}
	}

	public void Death()
	{
		active = false;
		inAction = true;
		chargeParticle.Play();
		anim.SetTrigger("Death");
		Invoke("DeathExplosion", 2f / eid.totalSpeedModifier);
		origPos = model.localPosition;
		vibrate = true;
		dying = true;
		if ((bool)currentEnrageEffect)
		{
			Object.Destroy(currentEnrageEffect);
		}
		for (int i = 0; i < tentacles.Length; i++)
		{
			TrailRenderer component = tentacles[i].GetComponent<TrailRenderer>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}
		if (tempBeam != null)
		{
			Object.Destroy(tempBeam);
		}
		if (lr.enabled)
		{
			lr.enabled = false;
		}
	}

	private void DeathExplosion()
	{
		Object.Instantiate(deathExplosion.ToAsset(), base.transform.position, Quaternion.identity);
		if (eid.drillers.Count > 0)
		{
			for (int num = eid.drillers.Count - 1; num >= 0; num--)
			{
				Object.Destroy(eid.drillers[num].gameObject);
			}
		}
		Object.Destroy(base.gameObject);
	}

	public void DeadLaunch(Vector3 direction)
	{
		launched = true;
		base.transform.LookAt(base.transform.position - direction);
		anim.Play("BeamHold", 0, 0f);
		ownColliders = GetComponentsInChildren<Collider>();
		CancelInvoke("DeathExplosion");
		Invoke("DeathExplosion", 2f / eid.totalSpeedModifier);
	}

	private void HomingAttack()
	{
		inAction = true;
		dontTeleport = true;
		chargeParticle.Play();
		anim.SetTrigger("HomingAttack");
		Object.Instantiate(windUp, base.transform);
	}

	private void BeamAttack()
	{
		inAction = true;
		chargeParticle.Play();
		dontTeleport = true;
		beamCooldown = true;
		beamNext = false;
		anim.SetTrigger("BeamAttack");
		Object.Instantiate(windUp, base.transform).GetComponent<AudioSource>().pitch = 1.5f;
	}

	private void MeleeAttack()
	{
		timeSinceMelee = 0f;
		inAction = true;
		anim.SetTrigger("MeleeAttack");
		Object.Instantiate(windUpSmall, base.transform);
	}

	public void SwingStart()
	{
		Object.Instantiate(warningFlash, eid.weakPoint.transform).transform.localScale *= 8f;
		mach.ParryableCheck();
	}

	public void DamageStart()
	{
		sc.DamageStart();
		goForward = true;
	}

	public void DamageEnd()
	{
		sc.DamageStop();
		mach.parryable = false;
		goForward = false;
	}

	public void LockTarget()
	{
		if (target == null)
		{
			return;
		}
		if (difficulty > 2 && enraged && Random.Range(0f, 1f) > 0.5f)
		{
			Teleport();
		}
		Rigidbody componentInParent = target.targetTransform.GetComponentInParent<Rigidbody>();
		if ((bool)componentInParent)
		{
			if (difficulty < 4)
			{
				float y = -1.5f;
				if (componentInParent.velocity.y < 0f)
				{
					y = componentInParent.velocity.y * 3f - 1.5f;
				}
				Vector3 vector = new Vector3(componentInParent.velocity.x * 2.5f, y, componentInParent.velocity.z * 2.5f);
				overrideTarget = target.position + vector;
				if (componentInParent.velocity.y < 0f && Physics.Raycast(maxDistance: Vector3.Distance(new Vector3(target.position.x + vector.x, target.position.y, target.position.z + vector.z), overrideTarget), origin: overrideTarget, direction: Vector3.down, hitInfo: out var hitInfo, layerMask: environmentMask))
				{
					overrideTarget = hitInfo.point + Vector3.up;
				}
			}
			else
			{
				overrideTarget = MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(0.5f);
			}
		}
		else
		{
			overrideTarget = target.position;
		}
		Object.Instantiate(warningFlashUnparriable, eid.weakPoint.transform).transform.localScale *= 8f;
		lr.SetPosition(0, base.transform.position);
		lr.SetPosition(1, overrideTarget);
		lr.enabled = true;
		overrideRotation = true;
	}

	public void StartBeam()
	{
		if (target == null || beaming)
		{
			return;
		}
		lr.enabled = false;
		beaming = true;
		tempBeam = Object.Instantiate(beam, rightHand.transform.position, base.transform.rotation);
		tempBeam.transform.SetParent(launched ? model : rightHand, worldPositionStays: true);
		if (tempBeam.TryGetComponent<ContinuousBeam>(out var component))
		{
			component.damage *= eid.totalDamageModifier;
			component.target = target;
			if (launched)
			{
				component.canHitPlayer = false;
			}
		}
		Vector3 vector = ((difficulty >= 4 && eid.target.isPlayer) ? MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(0.5f) : target.position);
		overrideTarget += (vector - overrideTarget) * 2f;
		Quaternion b = Quaternion.LookRotation(overrideTarget - base.transform.position, Vector3.up);
		beamDistance = Quaternion.Angle(base.transform.rotation, b);
	}

	private void StopBeam()
	{
		if (tempBeam != null)
		{
			Object.Destroy(tempBeam);
		}
		chargeParticle.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmitting);
		overrideRotation = false;
		anim.SetTrigger("StopBeam");
	}

	public void ShootProjectiles()
	{
		if (target == null)
		{
			return;
		}
		GameObject gameObject = new GameObject();
		gameObject.transform.position = base.transform.position;
		ProjectileSpread projectileSpread = gameObject.AddComponent<ProjectileSpread>();
		projectileSpread.dontSpawn = true;
		projectileSpread.timeUntilDestroy = 10f;
		for (int i = 0; i < tentacles.Length; i++)
		{
			GameObject gameObject2 = Object.Instantiate(homingProjectile, base.transform.position, Quaternion.LookRotation(target.position - base.transform.position));
			if (!Physics.Raycast(base.transform.position, tentacles[i].position - base.transform.position, Vector3.Distance(tentacles[i].position, base.transform.position), environmentMask))
			{
				gameObject2.transform.position = tentacles[i].position;
			}
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: true);
			Projectile component = gameObject2.GetComponent<Projectile>();
			component.target = target;
			component.speed = 10f;
			component.damage *= eid.totalDamageModifier;
		}
		chargeParticle.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmittingAndClear);
		cooldown = Random.Range(4, 5);
	}

	public void HighDifficultyTeleport()
	{
		if (enraged && !dontTeleport)
		{
			Teleport();
			anim.speed = 0f;
			Invoke("ResetAnimSpeed", 0.25f / eid.totalSpeedModifier);
			if (Random.Range(0f, 1f) < 0.1f || (difficulty > 3 && Random.Range(0f, 1f) < 0.33f))
			{
				Invoke("Teleport", 0.2f / eid.totalSpeedModifier);
			}
		}
	}

	public void MeleeTeleport()
	{
		if (enraged)
		{
			Teleport(closeRange: true);
			anim.speed = 0f;
			CancelInvoke("ResetAnimSpeed");
			Invoke("ResetAnimSpeed", 0.25f / eid.totalSpeedModifier);
		}
	}

	public void ResetAnimSpeed()
	{
		anim.speed = defaultAnimSpeed;
	}

	public void StopAction()
	{
		beaming = false;
		inAction = false;
		dontTeleport = false;
		RandomizeDirection();
	}

	public void Enrage()
	{
		if (enraged)
		{
			return;
		}
		enraged = true;
		if (ensims == null || ensims.Length == 0)
		{
			ensims = GetComponentsInChildren<EnemySimplifier>();
		}
		EnemySimplifier[] array = ensims;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enraged = true;
		}
		Gradient gradient = new Gradient();
		GradientColorKey[] array2 = new GradientColorKey[2];
		array2[0].color = Color.red;
		array2[0].time = 0f;
		array2[1].color = Color.red;
		array2[1].time = 1f;
		GradientAlphaKey[] array3 = new GradientAlphaKey[2];
		array3[0].alpha = 1f;
		array3[0].time = 0f;
		array3[1].alpha = 0f;
		array3[1].time = 1f;
		gradient.SetKeys(array2, array3);
		for (int j = 0; j < tentacles.Length; j++)
		{
			TrailRenderer component = tentacles[j].GetComponent<TrailRenderer>();
			if ((bool)component)
			{
				component.colorGradient = gradient;
			}
		}
		currentEnrageEffect = Object.Instantiate(enrageEffect, base.transform.position, base.transform.rotation);
		currentEnrageEffect.transform.SetParent(base.transform, worldPositionStays: true);
		originalGlow.SetActive(value: false);
		enrageGlow.SetActive(value: true);
	}

	public void UnEnrage()
	{
		if (!enraged)
		{
			return;
		}
		enraged = false;
		if (ensims == null || ensims.Length == 0)
		{
			ensims = GetComponentsInChildren<EnemySimplifier>();
		}
		EnemySimplifier[] array = ensims;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enraged = false;
		}
		for (int j = 0; j < tentacles.Length; j++)
		{
			TrailRenderer component = tentacles[j].GetComponent<TrailRenderer>();
			if ((bool)component)
			{
				component.colorGradient = originalTentacleGradient;
			}
		}
		Object.Destroy(currentEnrageEffect);
		originalGlow.SetActive(value: true);
		enrageGlow.SetActive(value: false);
	}
}

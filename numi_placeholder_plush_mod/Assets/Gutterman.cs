using Sandbox;
using UnityEngine;
using UnityEngine.AI;

public class Gutterman : MonoBehaviour, IEnrage, IAlter, IAlterOptions<bool>
{
	private bool gotValues;

	private EnemyIdentifier eid;

	private NavMeshAgent nma;

	private Machine mach;

	private Rigidbody rb;

	private Animator anim;

	private int difficulty;

	private float defaultMovementSpeed;

	[HideInInspector]
	public bool dead;

	[HideInInspector]
	public bool fallen;

	[HideInInspector]
	public bool exploded;

	public bool hasShield = true;

	[SerializeField]
	private GameObject[] shield;

	public Transform torsoAimBone;

	public Transform gunAimBone;

	private Quaternion torsoDefaultRotation;

	[SerializeField]
	private SwingCheck2 sc;

	[SerializeField]
	private SwingCheck2 shieldlessSwingcheck;

	private bool inAction;

	private bool attacking;

	private bool moveForward;

	private bool trackInAction;

	public Transform shootPoint;

	public GameObject beam;

	private float windup;

	private float windupSpeed;

	[SerializeField]
	private AudioSource windupAud;

	[SerializeField]
	private Transform windupBarrel;

	private Quaternion barrelRotation;

	private bool slowMode;

	private float slowModeLerp;

	private bool firing;

	private float bulletCooldown;

	private float lineOfSightTimer;

	private float trackingSpeed;

	private float trackingSpeedMultiplier;

	private float defaultTrackingSpeed = 1f;

	private Vector3 trackingPosition;

	private Vector3 lastKnownPosition;

	private TimeSince lastParried;

	[SerializeField]
	private GameObject playerUnstucker;

	[SerializeField]
	private GameObject fallingKillTrigger;

	[SerializeField]
	private GameObject fallEffect;

	[SerializeField]
	private GameObject corpseExplosion;

	[SerializeField]
	private GameObject shieldBreakEffect;

	[SerializeField]
	private AudioSource bonkSound;

	[SerializeField]
	private AudioSource releaseSound;

	[SerializeField]
	private AudioSource deathSound;

	private bool enraged;

	public bool eternalRage;

	[SerializeField]
	private AudioSource enrageEffect;

	private AudioSource currentEnrageEffect;

	private float rageLeft;

	private EnemySimplifier[] ensims;

	public bool isEnraged => enraged;

	public string alterKey => "Gutterman";

	public string alterCategoryName => "Gutterman";

	public AlterOption<bool>[] options => new AlterOption<bool>[2]
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

	private void Start()
	{
		GetValues();
	}

	private void GetValues()
	{
		if (gotValues)
		{
			return;
		}
		gotValues = true;
		eid = GetComponent<EnemyIdentifier>();
		nma = GetComponent<NavMeshAgent>();
		mach = GetComponent<Machine>();
		rb = GetComponent<Rigidbody>();
		anim = GetComponent<Animator>();
		ensims = GetComponentsInChildren<EnemySimplifier>();
		if (dead)
		{
			CheckIfInstaCorpse();
			return;
		}
		if (eid.difficultyOverride >= 0)
		{
			difficulty = eid.difficultyOverride;
		}
		else
		{
			difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		}
		if (!hasShield)
		{
			GameObject[] array = shield;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
		}
		anim.SetBool("Shield", hasShield);
		torsoDefaultRotation = Quaternion.Inverse(base.transform.rotation) * torsoAimBone.rotation;
		lastParried = 5f;
		barrelRotation = windupBarrel.localRotation;
		if (windupAud.pitch != 0f)
		{
			windupAud.Play();
		}
		SetSpeed();
		SlowUpdate();
	}

	private void OnEnable()
	{
		CheckIfInstaCorpse();
	}

	private void OnDisable()
	{
		inAction = false;
		CancelInvoke("DoneDying");
	}

	private void CheckIfInstaCorpse()
	{
		if (dead)
		{
			anim.Play("Death", 0, 1f);
			fallen = true;
			Invoke("DoneDying", 0.5f);
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
			anim.speed = 0.8f;
			defaultMovementSpeed = 8f;
			windupSpeed = 0.5f;
		}
		else if (difficulty == 1)
		{
			anim.speed = 0.9f;
			defaultMovementSpeed = 9f;
			windupSpeed = 0.75f;
		}
		else
		{
			anim.speed = 1f;
			defaultMovementSpeed = 10f;
			windupSpeed = 1f;
		}
		anim.speed *= eid.totalSpeedModifier;
		defaultMovementSpeed *= eid.totalSpeedModifier;
		nma.speed = (slowMode ? (defaultMovementSpeed / 2f) : defaultMovementSpeed);
		windupSpeed *= eid.totalSpeedModifier;
		if (difficulty > 2)
		{
			trackingSpeedMultiplier = 1f;
		}
		else if (difficulty == 2)
		{
			trackingSpeedMultiplier = 0.8f;
		}
		else if (difficulty == 1)
		{
			trackingSpeedMultiplier = 0.5f;
		}
		else
		{
			trackingSpeedMultiplier = 0.35f;
		}
		defaultTrackingSpeed = 1f;
		if (trackingSpeed < defaultTrackingSpeed)
		{
			trackingSpeed = defaultTrackingSpeed;
		}
	}

	private void Update()
	{
		if (dead)
		{
			return;
		}
		if (lineOfSightTimer >= 0.9f || (slowMode && lineOfSightTimer > 0f))
		{
			windup = Mathf.MoveTowards(windup, 1f, Time.deltaTime * windupSpeed);
		}
		else
		{
			windup = Mathf.MoveTowards(windup, 0f, Time.deltaTime * windupSpeed);
		}
		windupAud.pitch = windup * 3f;
		if (windupAud.pitch == 0f)
		{
			windupAud.Stop();
		}
		else if (!windupAud.isPlaying)
		{
			windupAud.Play();
		}
		if (inAction)
		{
			firing = false;
			if (nma.enabled)
			{
				nma.updateRotation = false;
			}
			if (difficulty <= 1)
			{
				windup = 0f;
			}
			if (eid.target != null)
			{
				trackingPosition = base.transform.position + base.transform.forward * Mathf.Max(5f, Vector3.Distance(base.transform.position, new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z))) + Vector3.up * (eid.target.position.y - base.transform.position.y);
				if (trackInAction || moveForward)
				{
					base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z) - base.transform.position), (float)(trackInAction ? 360 : 90) * Time.deltaTime);
				}
			}
		}
		else
		{
			RaycastHit hitInfo;
			bool flag = eid.target != null && !Physics.Raycast(base.transform.position + Vector3.up, eid.target.headPosition - (base.transform.position + Vector3.up), out hitInfo, Vector3.Distance(eid.target.position, base.transform.position + Vector3.up), LayerMaskDefaults.Get(LMD.Environment));
			lineOfSightTimer = Mathf.MoveTowards(lineOfSightTimer, flag ? 1 : 0, Time.deltaTime * 2f);
			if (windup >= 0.5f)
			{
				if (!slowMode)
				{
					trackingPosition = base.transform.position + base.transform.forward * Mathf.Max(30f, Vector3.Distance(base.transform.position, eid.target.headPosition));
				}
				slowMode = true;
			}
			else if (slowMode && windup <= 0f)
			{
				slowMode = false;
				Object.Instantiate(releaseSound, base.transform.position, Quaternion.identity);
			}
			if (firing && !mach.gc.onGround)
			{
				firing = false;
			}
			if (slowMode && firing)
			{
				trackingSpeed += Time.deltaTime * (float)(hasShield ? 2 : 5) * trackingSpeedMultiplier * eid.totalSpeedModifier;
			}
			else if (!slowMode)
			{
				trackingSpeed = defaultTrackingSpeed;
			}
			if (slowMode)
			{
				if (nma.enabled)
				{
					nma.updateRotation = false;
				}
				if (eid.target != null)
				{
					if (lineOfSightTimer > 0f)
					{
						lastKnownPosition = eid.target.headPosition;
					}
					trackingPosition = Vector3.MoveTowards(trackingPosition, lastKnownPosition, (Vector3.Distance(trackingPosition, eid.target.headPosition) + trackingSpeed) * Time.deltaTime);
				}
				base.transform.rotation = Quaternion.LookRotation(new Vector3(trackingPosition.x, base.transform.position.y, trackingPosition.z) - base.transform.position);
			}
			else if (nma.enabled)
			{
				nma.updateRotation = true;
			}
			nma.speed = (slowMode ? (defaultMovementSpeed / 2f) : defaultMovementSpeed);
			if (eid.target != null && lineOfSightTimer >= 0.5f && (float)lastParried > 5f && mach.gc.onGround && Vector3.Distance(base.transform.position, eid.target.position) < 12f)
			{
				ShieldBash();
			}
		}
		slowModeLerp = Mathf.MoveTowards(slowModeLerp, slowMode ? 1 : 0, Time.deltaTime * 2.5f);
		anim.SetFloat("WalkSpeed", slowMode ? 0.5f : 1f);
		anim.SetBool("Walking", nma.velocity.magnitude > 2.5f);
		anim.SetLayerWeight(1, firing ? 1 : 0);
		if (eternalRage || !(rageLeft > 0f))
		{
			return;
		}
		rageLeft = Mathf.MoveTowards(rageLeft, 0f, Time.deltaTime * eid.totalSpeedModifier);
		if (currentEnrageEffect != null && rageLeft < 3f)
		{
			currentEnrageEffect.pitch = rageLeft / 3f;
		}
		if (!(rageLeft <= 0f))
		{
			return;
		}
		enraged = false;
		EnemySimplifier[] array = ensims;
		foreach (EnemySimplifier enemySimplifier in array)
		{
			if ((bool)enemySimplifier)
			{
				enemySimplifier.enraged = false;
			}
		}
		if (currentEnrageEffect != null)
		{
			Object.Destroy(currentEnrageEffect.gameObject);
		}
	}

	private void LateUpdate()
	{
		if (!dead)
		{
			if (inAction)
			{
				Quaternion quaternion = Quaternion.RotateTowards(torsoAimBone.rotation, Quaternion.LookRotation(torsoAimBone.position - trackingPosition, Vector3.up), 60f);
				Quaternion quaternion2 = Quaternion.Inverse(base.transform.rotation * torsoDefaultRotation) * torsoAimBone.rotation;
				torsoAimBone.rotation = quaternion * quaternion2;
				sc.knockBackDirection = trackingPosition - torsoAimBone.position;
			}
			else if (slowModeLerp > 0f)
			{
				torsoAimBone.rotation = Quaternion.Lerp(torsoAimBone.rotation, Quaternion.LookRotation(torsoAimBone.position - trackingPosition), slowModeLerp);
				Quaternion rotation = gunAimBone.rotation;
				gunAimBone.rotation = Quaternion.LookRotation(gunAimBone.position - trackingPosition);
				gunAimBone.Rotate(Vector3.left, 90f, Space.Self);
				gunAimBone.Rotate(Vector3.up, 180f, Space.Self);
				gunAimBone.rotation = Quaternion.Lerp(rotation, gunAimBone.rotation, slowModeLerp);
			}
			windupBarrel.localRotation = barrelRotation;
			if (windup > 0f)
			{
				windupBarrel.Rotate(Vector3.up * -3600f * windup * Time.deltaTime);
				barrelRotation = windupBarrel.localRotation;
			}
		}
	}

	private void FixedUpdate()
	{
		if (dead)
		{
			return;
		}
		if (inAction)
		{
			rb.isKinematic = !moveForward;
			if (moveForward)
			{
				if (Physics.Raycast(base.transform.position + Vector3.up + base.transform.forward, Vector3.down, out var _, (eid.target == null) ? 22f : Mathf.Max(22f, base.transform.position.y - eid.target.position.y + 2.5f), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
				{
					rb.velocity = base.transform.forward * (hasShield ? 25 : 45) * anim.speed * eid.totalSpeedModifier;
				}
				else
				{
					rb.velocity = Vector3.zero;
				}
			}
		}
		if (!firing)
		{
			return;
		}
		if (bulletCooldown == 0f)
		{
			Vector3 position = shootPoint.position + shootPoint.right * Random.Range(-0.2f, 0.2f) + shootPoint.up * Random.Range(-0.2f, 0.2f);
			if (Physics.Raycast(shootPoint.position - shootPoint.forward * 4f, shootPoint.forward, 4f, LayerMaskDefaults.Get(LMD.EnvironmentAndPlayer)))
			{
				position = shootPoint.position - shootPoint.forward * 4f;
			}
			Object.Instantiate(beam, position, shootPoint.rotation).transform.Rotate(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));
			bulletCooldown = 0.05f / windup;
		}
		else
		{
			bulletCooldown = Mathf.MoveTowards(bulletCooldown, 0f, Time.fixedDeltaTime);
		}
	}

	private void SlowUpdate()
	{
		if (dead)
		{
			return;
		}
		Invoke("SlowUpdate", 0.1f);
		if (!inAction && eid.target != null && nma.enabled && nma.isOnNavMesh)
		{
			nma.SetDestination(eid.target.position);
		}
		if (inAction)
		{
			return;
		}
		if (slowMode && windup >= 0.5f && eid.target != null && (firing || windup >= 1f) && mach.gc.onGround)
		{
			firing = true;
			RaycastHit[] array = Physics.RaycastAll(base.transform.position + Vector3.up + base.transform.forward * 3f, eid.target.headPosition - (base.transform.position + Vector3.up), Vector3.Distance(eid.target.position, base.transform.position + Vector3.up), LayerMaskDefaults.Get(LMD.Enemies));
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit = array[i];
				if (!(raycastHit.transform == eid.target.targetTransform) && !(raycastHit.transform == shield[0].transform) && (!raycastHit.transform.TryGetComponent<EnemyIdentifierIdentifier>(out var component) || !component.eid || (!(component.eid == eid) && !component.eid.dead && !(component.eid == eid.target.enemyIdentifier))))
				{
					firing = false;
					break;
				}
			}
		}
		else
		{
			firing = false;
		}
	}

	private void Death()
	{
		Object.Instantiate(deathSound, base.transform);
		ShieldBashStop();
		dead = true;
		windupAud.Stop();
		anim.SetBool("Dead", value: true);
		anim.SetLayerWeight(1, 0f);
		anim.Play("Death", 0, 0f);
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
		if (currentEnrageEffect != null)
		{
			Object.Destroy(currentEnrageEffect.gameObject);
		}
	}

	public void ShieldBreak(bool player = true, bool flash = true)
	{
		anim.Play("ShieldBreak", 0, 0f);
		anim.SetBool("Shield", value: false);
		if (player)
		{
			if (flash)
			{
				MonoSingleton<NewMovement>.Instance.Parry(null, "GUARD BREAK");
			}
			else
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100, "<color=green>GUARD BREAK</color>");
			}
			if (difficulty >= 4)
			{
				Enrage();
			}
		}
		GameObject[] array = shield;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		hasShield = false;
		Object.Instantiate(bonkSound, base.transform.position, Quaternion.identity);
		Object.Instantiate(shieldBreakEffect, shield[0].transform.position, Quaternion.identity);
		if (inAction)
		{
			ShieldBashStop();
			StopAction();
		}
		sc = shieldlessSwingcheck;
		inAction = true;
		attacking = false;
		trackInAction = false;
		nma.enabled = false;
		moveForward = false;
		firing = false;
		slowMode = false;
		windup = 0f;
	}

	private void ShieldBash()
	{
		if (difficulty <= 2 && hasShield)
		{
			lastParried = 3f;
		}
		anim.Play(hasShield ? "ShieldBash" : "Smack", 0, 0f);
		Object.Instantiate((hasShield || enraged) ? MonoSingleton<DefaultReferenceManager>.Instance.unparryableFlash : MonoSingleton<DefaultReferenceManager>.Instance.parryableFlash, shield[0].transform.position + base.transform.forward, base.transform.rotation).transform.localScale *= 15f;
		inAction = true;
		nma.enabled = false;
		firing = false;
		attacking = true;
		trackInAction = true;
		if (!hasShield && !enraged)
		{
			mach.parryable = true;
		}
	}

	private void ShieldBashActive()
	{
		if (attacking)
		{
			sc.DamageStart();
			sc.knockBackDirectionOverride = true;
			sc.knockBackDirection = base.transform.forward;
			moveForward = true;
			trackInAction = false;
		}
	}

	private void ShieldBashStop()
	{
		sc.DamageStop();
		moveForward = false;
		mach.parryable = false;
		attacking = false;
	}

	private void StopAction()
	{
		if (!dead)
		{
			inAction = false;
			if (mach.gc.onGround)
			{
				rb.isKinematic = true;
				nma.enabled = true;
			}
			else
			{
				rb.isKinematic = false;
			}
		}
	}

	public void GotParried()
	{
		anim.Play("ShieldBreak", 0, 0f);
		ShieldBashStop();
		StopAction();
		inAction = true;
		trackInAction = false;
		attacking = false;
		nma.enabled = false;
		moveForward = false;
		if (difficulty >= 4)
		{
			Enrage();
		}
		else
		{
			lastParried = 0f;
		}
		windup = 0f;
		trackingSpeed = defaultTrackingSpeed;
		Object.Instantiate(bonkSound, base.transform.position, Quaternion.identity);
	}

	private void FallStart()
	{
		fallingKillTrigger.SetActive(value: true);
	}

	private void FallOver()
	{
		if (!fallEffect)
		{
			return;
		}
		if ((bool)MonoSingleton<EndlessGrid>.Instance)
		{
			Explode();
			return;
		}
		if (mach.gc.onGround)
		{
			for (int i = 0; i < mach.gc.cols.Count; i++)
			{
				if (mach.gc.cols[i].gameObject.CompareTag("Moving"))
				{
					Explode();
					return;
				}
			}
		}
		fallEffect.transform.position = new Vector3(mach.chest.transform.position.x, base.transform.position.y, mach.chest.transform.position.z);
		fallEffect.SetActive(value: true);
		fallingKillTrigger.SetActive(value: false);
		playerUnstucker.SetActive(value: true);
		fallen = true;
		Invoke("DoneDying", 1f);
	}

	public void Explode()
	{
		if (exploded)
		{
			return;
		}
		exploded = true;
		Object.Instantiate(corpseExplosion, torsoAimBone.position, Quaternion.identity);
		if ((bool)mach)
		{
			EnemyIdentifierIdentifier[] componentsInChildren = GetComponentsInChildren<EnemyIdentifierIdentifier>();
			foreach (EnemyIdentifierIdentifier enemyIdentifierIdentifier in componentsInChildren)
			{
				if (!(enemyIdentifierIdentifier == null))
				{
					mach.GetHurt(enemyIdentifierIdentifier.gameObject, (base.transform.position - enemyIdentifierIdentifier.transform.position).normalized * 1000f, 999f, 1f);
				}
			}
		}
		Object.Destroy(base.gameObject);
	}

	private void DoneDying()
	{
		playerUnstucker.SetActive(value: false);
		anim.enabled = false;
		base.enabled = false;
	}

	public void Enrage()
	{
		if (enraged)
		{
			return;
		}
		enraged = true;
		rageLeft = 10f;
		EnemySimplifier[] array = ensims;
		foreach (EnemySimplifier enemySimplifier in array)
		{
			if ((bool)enemySimplifier)
			{
				enemySimplifier.enraged = true;
			}
		}
		if (currentEnrageEffect == null)
		{
			currentEnrageEffect = Object.Instantiate(enrageEffect, mach.chest.transform);
			currentEnrageEffect.pitch = 1f;
			currentEnrageEffect.transform.localScale *= 0.01f;
		}
	}

	public void UnEnrage()
	{
		enraged = false;
		rageLeft = 0f;
		EnemySimplifier[] array = ensims;
		foreach (EnemySimplifier enemySimplifier in array)
		{
			if ((bool)enemySimplifier)
			{
				enemySimplifier.enraged = false;
			}
		}
		if (currentEnrageEffect != null)
		{
			Object.Destroy(currentEnrageEffect.gameObject);
		}
	}
}

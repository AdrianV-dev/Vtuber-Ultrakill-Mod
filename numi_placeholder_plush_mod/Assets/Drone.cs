using System;
using System.Collections.Generic;
using Sandbox;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Drone : MonoBehaviour, IEnrage, IAlter, IAlterOptions<bool>
{
	public bool dontStartAware;

	public bool stationary;

	public float health;

	public bool crashing;

	private Vector3 crashTarget;

	private Rigidbody rb;

	private bool canInterruptCrash;

	private Transform modelTransform;

	public bool targetSpotted;

	public bool toLastKnownPos;

	private Vector3 lastKnownPos;

	private Vector3 nextRandomPos;

	public float checkCooldown;

	public float blockCooldown;

	public float preferredDistanceToTarget = 15f;

	private BloodsplatterManager bsm;

	public AssetReference explosion;

	public AssetReference gib;

	private StyleCalculator scalc;

	private EnemyIdentifier eid;

	private EnemyType type;

	private AudioSource aud;

	public AudioClip hurtSound;

	public AudioClip deathSound;

	public AudioClip windUpSound;

	public AudioClip spotSound;

	public AudioClip loseSound;

	private float dodgeCooldown;

	private float attackCooldown;

	public AssetReference projectile;

	private Material origMaterial;

	public Material shootMaterial;

	private EnemySimplifier[] ensims;

	public ParticleSystem chargeParticle;

	private bool killedByPlayer;

	private bool parried;

	private bool exploded;

	private bool parryable;

	private Vector3 viewTarget;

	[HideInInspector]
	public bool musicRequested;

	private GoreZone gz;

	private int difficulty;

	private Animator anim;

	public bool enraged;

	public GameObject enrageEffect;

	private int usedAttacks;

	[HideInInspector]
	public List<VirtueInsignia> childVi = new List<VirtueInsignia>();

	private EnemyCooldowns vc;

	private KeepInBounds kib;

	private bool checkingForCrash;

	private bool canHurtOtherDrones;

	[HideInInspector]
	public bool lockRotation;

	[HideInInspector]
	public bool lockPosition;

	private bool hooked;

	private bool homeRunnable;

	public bool cantInstaExplode;

	private GameObject currentEnrageEffect;

	[HideInInspector]
	public bool fleshDrone;

	private int parryFramesLeft;

	public GameObject ghost;

	private EnemyTarget target => eid.target;

	private int relevantSightBlockMask => LayerMaskDefaults.Get((!target.isPlayer) ? LMD.Environment : LMD.EnvironmentAndBigEnemies);

	public bool isEnraged { get; private set; }

	public string alterKey => "drone";

	public string alterCategoryName => "drone";

	public AlterOption<bool>[] options
	{
		get
		{
			if (type != EnemyType.Drone)
			{
				return new AlterOption<bool>[1]
				{
					new AlterOption<bool>
					{
						key = "enraged",
						name = "Enraged",
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
						}
					}
				};
			}
			return null;
		}
	}

	private void Awake()
	{
		eid = GetComponent<EnemyIdentifier>();
		rb = GetComponent<Rigidbody>();
		kib = GetComponent<KeepInBounds>();
		type = eid.enemyType;
		if (type == EnemyType.Virtue)
		{
			vc = MonoSingleton<EnemyCooldowns>.Instance;
		}
	}

	private void Start()
	{
		bsm = MonoSingleton<BloodsplatterManager>.Instance;
		if (!chargeParticle)
		{
			chargeParticle = GetComponentInChildren<ParticleSystem>();
		}
		if (type == EnemyType.Virtue)
		{
			anim = GetComponent<Animator>();
		}
		dodgeCooldown = UnityEngine.Random.Range(0.5f, 3f);
		if (type == EnemyType.Drone)
		{
			attackCooldown = UnityEngine.Random.Range(1f, 3f);
		}
		else
		{
			attackCooldown = 1.5f;
		}
		if (!dontStartAware)
		{
			targetSpotted = true;
		}
		if (type == EnemyType.Drone)
		{
			modelTransform = base.transform.Find("drone");
			if ((bool)modelTransform)
			{
				ensims = modelTransform.GetComponentsInChildren<EnemySimplifier>();
				origMaterial = ensims[0].GetComponent<Renderer>().material;
			}
			rb.solverIterations *= 3;
			rb.solverVelocityIterations *= 3;
		}
		SlowUpdate();
		if (!musicRequested && !eid.dead)
		{
			musicRequested = true;
			MonoSingleton<MusicManager>.Instance.PlayBattleMusic();
		}
		gz = GoreZone.ResolveGoreZone(base.transform);
		if (enraged)
		{
			Enrage();
		}
		if (eid.difficultyOverride >= 0)
		{
			difficulty = eid.difficultyOverride;
		}
		else
		{
			difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		}
	}

	private void UpdateBuff()
	{
		if ((bool)anim)
		{
			anim.speed = eid.totalSpeedModifier;
		}
	}

	private void OnDisable()
	{
		if (type == EnemyType.Virtue && (bool)vc)
		{
			vc.RemoveVirtue(this);
		}
		if (musicRequested)
		{
			musicRequested = false;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if ((bool)instance)
			{
				instance.PlayCleanMusic();
			}
		}
		if ((bool)MonoSingleton<EnemyTracker>.Instance && type == EnemyType.Drone && MonoSingleton<EnemyTracker>.Instance.drones.Contains(this))
		{
			MonoSingleton<EnemyTracker>.Instance.drones.Remove(this);
		}
	}

	private void OnEnable()
	{
		if (type == EnemyType.Virtue && (bool)vc)
		{
			vc.AddVirtue(this);
		}
		if (!musicRequested && !eid.dead)
		{
			musicRequested = true;
			MonoSingleton<MusicManager>.Instance.PlayBattleMusic();
		}
		if (type == EnemyType.Drone && !MonoSingleton<EnemyTracker>.Instance.drones.Contains(this))
		{
			MonoSingleton<EnemyTracker>.Instance.drones.Add(this);
		}
	}

	private void UpdateRigidbodySettings()
	{
		if (target == null && !crashing)
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
		if (!crashing)
		{
			UpdateRigidbodySettings();
			if (target == null)
			{
				return;
			}
			if (targetSpotted)
			{
				viewTarget = target.position;
				float num = difficulty / 2;
				if (num == 0f)
				{
					num = 0.25f;
				}
				num *= eid.totalSpeedModifier;
				if (dodgeCooldown > 0f)
				{
					dodgeCooldown = Mathf.MoveTowards(dodgeCooldown, 0f, Time.deltaTime * num);
				}
				else if (!stationary && !lockPosition)
				{
					dodgeCooldown = UnityEngine.Random.Range(1f, 3f);
					RandomDodge();
				}
			}
			if ((type == EnemyType.Virtue && (!target.isPlayer || !MonoSingleton<NewMovement>.Instance.levelOver) && (Vector3.Distance(base.transform.position, target.position) < 150f || stationary)) || targetSpotted)
			{
				float num2 = difficulty / 2;
				if (type == EnemyType.Virtue && difficulty >= 4)
				{
					num2 = 1.2f;
				}
				else if (difficulty == 1)
				{
					num2 = 0.75f;
				}
				else if (difficulty == 0)
				{
					num2 = 0.5f;
				}
				num2 *= eid.totalSpeedModifier;
				if (attackCooldown > 0f)
				{
					attackCooldown = Mathf.MoveTowards(attackCooldown, 0f, Time.deltaTime * num2);
				}
				else if (projectile != null && (!vc || vc.virtueCooldown == 0f))
				{
					if ((bool)vc)
					{
						vc.virtueCooldown = 1f / eid.totalSpeedModifier;
					}
					parryable = true;
					PlaySound(windUpSound);
					if (chargeParticle != null)
					{
						chargeParticle.Play();
					}
					if (shootMaterial != null && ensims != null && ensims.Length != 0)
					{
						EnemySimplifier[] array = ensims;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].ChangeMaterialNew(EnemySimplifier.MaterialState.normal, shootMaterial);
						}
					}
					if (type == EnemyType.Drone)
					{
						attackCooldown = UnityEngine.Random.Range(2f, 4f);
						Invoke("Shoot", 0.75f / eid.totalSpeedModifier);
					}
					else
					{
						attackCooldown = UnityEngine.Random.Range(4f, 6f);
						if (anim != null)
						{
							anim.SetTrigger("Attack");
						}
					}
					if (parryFramesLeft > 0)
					{
						eid.hitter = "punch";
						eid.DeliverDamage(base.gameObject, MonoSingleton<CameraController>.Instance.transform.forward * 25000f, base.transform.position, 1f, tryForExplode: false);
						parryFramesLeft = 0;
					}
				}
			}
		}
		if ((bool)eid && eid.hooked && !hooked)
		{
			Hooked();
		}
		else if ((bool)eid && !eid.hooked && hooked)
		{
			Unhooked();
		}
	}

	private void SlowUpdate()
	{
		if (!crashing && target != null)
		{
			if (targetSpotted)
			{
				if (Physics.Raycast(base.transform.position, target.position - base.transform.position, Vector3.Distance(base.transform.position, target.position) - 1f, relevantSightBlockMask))
				{
					targetSpotted = false;
					PlaySound(loseSound);
					lastKnownPos = target.position;
					blockCooldown = 0f;
					checkCooldown = 0f;
					toLastKnownPos = true;
				}
			}
			else if (!Physics.Raycast(base.transform.position, target.position - base.transform.position, Vector3.Distance(base.transform.position, target.position) - 1f, relevantSightBlockMask))
			{
				PlaySound(spotSound);
				targetSpotted = true;
			}
			if (difficulty >= 4 && MonoSingleton<EnemyTracker>.Instance.drones.Count > 1)
			{
				Vector3 zero = Vector3.zero;
				foreach (Drone drone in MonoSingleton<EnemyTracker>.Instance.drones)
				{
					if (!(drone == this) && Vector3.Distance(drone.transform.position, base.transform.position) < 10f)
					{
						zero += base.transform.position - drone.transform.position;
					}
				}
				if (zero.magnitude > 0f)
				{
					Dodge(zero);
				}
			}
		}
		Invoke("SlowUpdate", 0.25f);
	}

	private void FixedUpdate()
	{
		if (parryFramesLeft > 0)
		{
			parryFramesLeft--;
		}
		if (rb.velocity.magnitude < 1f && rb.collisionDetectionMode != 0)
		{
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		}
		if (crashing)
		{
			if (type == EnemyType.Virtue)
			{
				if (parried)
				{
					rb.useGravity = false;
					rb.velocity = base.transform.forward * 120f * eid.totalSpeedModifier;
				}
			}
			else if (!parried)
			{
				float num = 50f;
				if (difficulty == 1)
				{
					num = 40f;
				}
				else if (difficulty == 0)
				{
					num = 25f;
				}
				num *= eid.totalSpeedModifier;
				rb.AddForce(base.transform.forward * num, ForceMode.Acceleration);
				modelTransform?.Rotate(0f, 0f, 10f, Space.Self);
			}
			else
			{
				rb.velocity = base.transform.forward * 50f;
				modelTransform?.Rotate(0f, 0f, 50f, Space.Self);
			}
		}
		else if (targetSpotted && target != null)
		{
			if (type == EnemyType.Drone)
			{
				rb.velocity *= 0.95f;
				if (!stationary && !lockPosition)
				{
					float num2 = 50f;
					if (difficulty >= 4)
					{
						num2 = 250f;
					}
					if (Vector3.Distance(base.transform.position, target.position) > preferredDistanceToTarget)
					{
						rb.AddForce(base.transform.forward * num2 * eid.totalSpeedModifier, ForceMode.Acceleration);
					}
					else if (Vector3.Distance(base.transform.position, target.position) < 5f)
					{
						if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
						{
							rb.AddForce(base.transform.forward * -0.1f * eid.totalSpeedModifier, ForceMode.Impulse);
						}
						else
						{
							rb.AddForce(base.transform.forward * (0f - num2) * eid.totalSpeedModifier, ForceMode.Impulse);
						}
					}
				}
			}
			else
			{
				rb.velocity *= 0.975f;
				if (!stationary && Vector3.Distance(base.transform.position, target.position) > 15f)
				{
					rb.AddForce(base.transform.forward * 10f * eid.totalSpeedModifier, ForceMode.Acceleration);
				}
			}
		}
		else if (toLastKnownPos && !stationary && !lockPosition && target != null)
		{
			if (blockCooldown == 0f)
			{
				viewTarget = lastKnownPos;
			}
			else
			{
				blockCooldown = Mathf.MoveTowards(blockCooldown, 0f, 0.01f);
			}
			rb.AddForce(base.transform.forward * 10f * eid.totalSpeedModifier, ForceMode.Acceleration);
			if (checkCooldown == 0f && Vector3.Distance(base.transform.position, lastKnownPos) > 5f)
			{
				checkCooldown = 0.1f;
				if (Physics.BoxCast(base.transform.position - (viewTarget - base.transform.position).normalized, Vector3.one, viewTarget - base.transform.position, base.transform.rotation, 4f, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)))
				{
					blockCooldown = UnityEngine.Random.Range(1.5f, 3f);
					Vector3 vector = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
					viewTarget = base.transform.position + vector * 100f;
				}
			}
			else if (Vector3.Distance(base.transform.position, lastKnownPos) <= 3f)
			{
				Physics.Raycast(base.transform.position, UnityEngine.Random.onUnitSphere, out var hitInfo, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies));
				lastKnownPos = hitInfo.point;
			}
			if (checkCooldown != 0f)
			{
				checkCooldown = Mathf.MoveTowards(checkCooldown, 0f, 0.01f);
			}
		}
		if (!crashing)
		{
			if (!lockRotation && target != null)
			{
				Quaternion b = Quaternion.LookRotation(viewTarget - base.transform.position);
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, 0.075f + 0.00025f * Quaternion.Angle(base.transform.rotation, b) * eid.totalSpeedModifier);
			}
			rb.velocity = Vector3.ClampMagnitude(rb.velocity, 50f * eid.totalSpeedModifier);
			if ((bool)kib)
			{
				kib.ValidateMove();
			}
		}
	}

	public void RandomDodge()
	{
		if ((difficulty != 1 || !(UnityEngine.Random.Range(0f, 1f) > 0.75f)) && difficulty != 0)
		{
			Dodge(base.transform.up * UnityEngine.Random.Range(-5f, 5f) + base.transform.right * UnityEngine.Random.Range(-5f, 5f));
		}
	}

	public void Dodge(Vector3 direction)
	{
		float num = 50f;
		if (type == EnemyType.Virtue)
		{
			num = 150f;
		}
		num *= eid.totalSpeedModifier;
		rb.AddForce(direction.normalized * num, ForceMode.Impulse);
	}

	public void GetHurt(Vector3 force, float multiplier, GameObject sourceWeapon = null, bool fromExplosion = false)
	{
		bool flag = false;
		if (!crashing)
		{
			if ((eid.hitter == "shotgunzone" || eid.hitter == "hammerzone") && !parryable && health - multiplier > 0f)
			{
				return;
			}
			if (((eid.hitter == "shotgunzone" || eid.hitter == "hammerzone") && parryable) || eid.hitter == "punch")
			{
				if (parryable)
				{
					if (!InvincibleEnemies.Enabled && !eid.blessed)
					{
						multiplier = ((parryFramesLeft > 0) ? 3 : 4);
					}
					MonoSingleton<FistControl>.Instance.currentPunch.Parry(hook: false, eid);
					parryable = false;
				}
				else
				{
					parryFramesLeft = MonoSingleton<FistControl>.Instance.currentPunch.activeFrames;
				}
			}
			if (!eid.blessed && !InvincibleEnemies.Enabled)
			{
				health -= 1f * multiplier;
			}
			else
			{
				multiplier = 0f;
			}
			health = (float)Math.Round(health, 4);
			if ((double)health <= 0.001)
			{
				health = 0f;
			}
			if (eid == null)
			{
				eid = GetComponent<EnemyIdentifier>();
			}
			if (health <= 0f)
			{
				flag = true;
			}
			if (homeRunnable && !fleshDrone && !eid.puppet && flag && (eid.hitter == "punch" || eid.hitter == "heavypunch" || eid.hitter == "hammer"))
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100, "ultrakill.homerun", sourceWeapon, eid);
				MonoSingleton<StyleCalculator>.Instance.AddToMultiKill();
			}
			else if (eid.hitter != "enemy" && !eid.puppet && multiplier != 0f)
			{
				if (scalc == null)
				{
					scalc = MonoSingleton<StyleCalculator>.Instance;
				}
				if ((bool)scalc)
				{
					scalc.HitCalculator(eid.hitter, "drone", "", flag, eid, sourceWeapon);
				}
			}
			if (health <= 0f && !crashing)
			{
				parryable = false;
				Death(fromExplosion);
				if (eid.hitter != "punch" && eid.hitter != "heavypunch" && eid.hitter != "hammer")
				{
					if (target != null)
					{
						crashTarget = target.position;
					}
				}
				else
				{
					canHurtOtherDrones = true;
					base.transform.position += force.normalized;
					crashTarget = base.transform.position + force;
					rb.velocity = force.normalized * 40f;
				}
				base.transform.LookAt(crashTarget);
				if (aud == null)
				{
					aud = GetComponent<AudioSource>();
				}
				if (type == EnemyType.Drone)
				{
					aud.clip = deathSound;
					aud.volume = 0.75f;
					aud.pitch = UnityEngine.Random.Range(0.85f, 1.35f);
					aud.priority = 11;
					aud.Play();
				}
				else
				{
					PlaySound(deathSound);
				}
				Invoke("CanInterruptCrash", 0.5f);
				Invoke("Explode", 5f);
			}
			else if (eid.hitter != "fire")
			{
				PlaySound(hurtSound);
				GameObject gameObject = null;
				Bloodsplatter bloodsplatter = null;
				if (multiplier != 0f)
				{
					gameObject = bsm.GetGore(GoreType.Body, eid, fromExplosion);
					gameObject.transform.position = base.transform.position;
					gameObject.SetActive(value: true);
					gameObject.transform.SetParent(gz.goreZone, worldPositionStays: true);
					if (eid.hitter == "drill")
					{
						gameObject.transform.localScale *= 2f;
					}
					bloodsplatter = gameObject.GetComponent<Bloodsplatter>();
				}
				if (health > 0f)
				{
					if ((bool)bloodsplatter)
					{
						bloodsplatter.GetReady();
					}
					if (!eid.blessed)
					{
						rb.velocity /= 10f;
						rb.AddForce(force.normalized * (force.magnitude / 100f), ForceMode.Impulse);
						rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
						if (rb.velocity.magnitude > 50f)
						{
							rb.velocity = Vector3.ClampMagnitude(rb.velocity, 50f);
						}
					}
				}
				if (multiplier >= 1f)
				{
					if ((bool)bloodsplatter)
					{
						bloodsplatter.hpAmount = 30;
					}
					if (gib != null)
					{
						for (int i = 0; (float)i <= multiplier; i++)
						{
							UnityEngine.Object.Instantiate(gib.ToAsset(), base.transform.position, UnityEngine.Random.rotation).transform.SetParent(gz.gibZone, worldPositionStays: true);
						}
					}
				}
				if (MonoSingleton<BloodsplatterManager>.Instance.goreOn && (bool)gameObject && gameObject.TryGetComponent<ParticleSystem>(out var component))
				{
					component.Play();
				}
			}
			else
			{
				PlaySound(hurtSound);
			}
		}
		else if ((eid.hitter == "punch" || eid.hitter == "hammer") && !parried)
		{
			parried = true;
			rb.velocity = Vector3.zero;
			base.transform.rotation = MonoSingleton<CameraController>.Instance.transform.rotation;
			Punch currentPunch = MonoSingleton<FistControl>.Instance.currentPunch;
			if (eid.hitter == "punch")
			{
				currentPunch.GetComponent<Animator>().Play("Hook", -1, 0.065f);
				currentPunch.Parry(hook: false, eid);
			}
			if (type == EnemyType.Virtue && TryGetComponent<Collider>(out var component2))
			{
				component2.isTrigger = true;
			}
		}
		else if (multiplier >= 1f || canInterruptCrash)
		{
			Explode();
		}
	}

	public void PlaySound(AudioClip clippe)
	{
		if ((bool)clippe)
		{
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = clippe;
			if (type == EnemyType.Drone)
			{
				aud.volume = 0.5f;
				aud.pitch = UnityEngine.Random.Range(0.85f, 1.35f);
			}
			aud.priority = 12;
			aud.Play();
		}
	}

	public void Explode()
	{
		if (exploded || !base.gameObject.activeInHierarchy || (cantInstaExplode && !canInterruptCrash))
		{
			return;
		}
		exploded = true;
		GameObject obj = UnityEngine.Object.Instantiate(this.explosion.ToAsset(), base.transform.position, Quaternion.identity);
		obj.transform.SetParent(gz.transform, worldPositionStays: true);
		Explosion[] componentsInChildren = obj.GetComponentsInChildren<Explosion>();
		foreach (Explosion explosion in componentsInChildren)
		{
			if (eid.totalDamageModifier != 1f)
			{
				explosion.damage = Mathf.RoundToInt((float)explosion.damage * eid.totalDamageModifier);
				explosion.maxSize *= eid.totalDamageModifier;
				explosion.speed *= eid.totalDamageModifier;
			}
			if (difficulty >= 4 && type == EnemyType.Drone && !parried && !canHurtOtherDrones)
			{
				explosion.toIgnore.Add(EnemyType.Drone);
			}
			if (killedByPlayer)
			{
				explosion.friendlyFire = true;
			}
		}
		DoubleRender componentInChildren = GetComponentInChildren<DoubleRender>();
		if ((bool)componentInChildren)
		{
			componentInChildren.RemoveEffect();
		}
		if (!crashing)
		{
			Death(fromExplosion: true);
		}
		else if (eid.drillers.Count > 0)
		{
			for (int num = eid.drillers.Count - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(eid.drillers[num].gameObject);
			}
		}
		if (GhostDroneMode.Enabled && ghost != null)
		{
			UnityEngine.Object.Instantiate(ghost, base.transform.position, base.transform.rotation);
		}
		UnityEngine.Object.Destroy(base.gameObject);
		if (musicRequested)
		{
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if ((bool)instance)
			{
				instance.PlayCleanMusic();
			}
		}
	}

	private void Death(bool fromExplosion = false)
	{
		if (crashing)
		{
			return;
		}
		crashing = true;
		UpdateRigidbodySettings();
		if (rb.isKinematic)
		{
			rb.isKinematic = false;
		}
		if (type == EnemyType.Virtue)
		{
			rb.velocity = Vector3.zero;
			rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
			rb.useGravity = true;
			if (childVi.Count > 0)
			{
				for (int i = 0; i < childVi.Count; i++)
				{
					if (childVi[i] != null && (bool)childVi[i].gameObject)
					{
						UnityEngine.Object.Destroy(childVi[i].gameObject);
					}
				}
			}
		}
		if (eid.hitter != "enemy")
		{
			killedByPlayer = true;
		}
		if (!eid.dontCountAsKills)
		{
			if (gz != null && gz.checkpoint != null)
			{
				gz.AddDeath();
				gz.checkpoint.sm.kills++;
			}
			else
			{
				MonoSingleton<StatsManager>.Instance.kills++;
			}
		}
		if (eid.hitter != "fire")
		{
			GameObject gore = bsm.GetGore(GoreType.Head, eid, fromExplosion);
			if ((bool)gore)
			{
				gore.transform.position = base.transform.position;
				if (MonoSingleton<BloodsplatterManager>.Instance.goreOn && gore.TryGetComponent<ParticleSystem>(out var component))
				{
					component.Play();
				}
				gore.transform.SetParent(gz.goreZone, worldPositionStays: true);
				if (eid.hitter == "drill")
				{
					gore.transform.localScale *= 2f;
				}
				if (gore.TryGetComponent<Bloodsplatter>(out var component2))
				{
					component2.GetReady();
				}
			}
		}
		if (!eid.dontCountAsKills)
		{
			ActivateNextWave componentInParent = GetComponentInParent<ActivateNextWave>();
			if (componentInParent != null)
			{
				componentInParent.AddDeadEnemy();
			}
		}
	}

	public void Shoot()
	{
		parryable = false;
		if (crashing || !projectile.RuntimeKeyIsValid())
		{
			return;
		}
		EnemySimplifier[] array = ensims;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ChangeMaterialNew(EnemySimplifier.MaterialState.normal, origMaterial);
		}
		if (base.gameObject.activeInHierarchy)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(projectile.ToAsset(), base.transform.position + base.transform.forward, base.transform.rotation);
			gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x, gameObject.transform.rotation.eulerAngles.y, UnityEngine.Random.Range(0, 360));
			gameObject.transform.localScale *= 0.5f;
			SetProjectileSettings(gameObject.GetComponent<Projectile>());
			GameObject gameObject2 = UnityEngine.Object.Instantiate(projectile.ToAsset(), gameObject.transform.position + gameObject.transform.up, gameObject.transform.rotation);
			if (difficulty > 2)
			{
				gameObject2.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x + 10f, gameObject.transform.rotation.eulerAngles.y, gameObject.transform.rotation.eulerAngles.z);
			}
			gameObject2.transform.localScale *= 0.5f;
			SetProjectileSettings(gameObject2.GetComponent<Projectile>());
			gameObject2 = UnityEngine.Object.Instantiate(projectile.ToAsset(), gameObject.transform.position - gameObject.transform.up, gameObject.transform.rotation);
			if (difficulty > 2)
			{
				gameObject2.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x - 10f, gameObject.transform.rotation.eulerAngles.y, gameObject.transform.rotation.eulerAngles.z);
			}
			gameObject2.transform.localScale *= 0.5f;
			SetProjectileSettings(gameObject2.GetComponent<Projectile>());
		}
	}

	private void SetProjectileSettings(Projectile proj)
	{
		float speed = 35f;
		if (difficulty >= 3)
		{
			speed = 45f;
		}
		else if (difficulty == 1)
		{
			speed = 25f;
		}
		else if (difficulty == 0)
		{
			speed = 15f;
		}
		proj.damage *= eid.totalDamageModifier;
		proj.target = target;
		proj.safeEnemyType = EnemyType.Drone;
		proj.speed = speed;
	}

	public void SpawnInsignia()
	{
		if (target != null && !crashing)
		{
			parryable = false;
			GameObject gameObject = UnityEngine.Object.Instantiate(projectile.ToAsset(), target.position, Quaternion.identity);
			VirtueInsignia component = gameObject.GetComponent<VirtueInsignia>();
			component.target = target;
			component.parentDrone = this;
			component.hadParent = true;
			chargeParticle.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmittingAndClear);
			if (enraged)
			{
				component.predictive = true;
			}
			if (difficulty == 1)
			{
				component.windUpSpeedMultiplier = 0.875f;
			}
			else if (difficulty == 0)
			{
				component.windUpSpeedMultiplier = 0.75f;
			}
			if (difficulty >= 4)
			{
				component.explosionLength = ((difficulty == 5) ? 5f : 3.5f);
			}
			if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
			{
				gameObject.transform.localScale *= 0.75f;
				component.windUpSpeedMultiplier *= 0.875f;
			}
			component.windUpSpeedMultiplier *= eid.totalSpeedModifier;
			component.damage = Mathf.RoundToInt((float)component.damage * eid.totalDamageModifier);
			usedAttacks++;
			if (((difficulty > 2 && usedAttacks > 2) || (difficulty == 2 && usedAttacks > 4 && !eid.blessed)) && !enraged && vc.currentVirtues.Count < 3)
			{
				Invoke("Enrage", 3f / eid.totalSpeedModifier);
			}
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (crashing && (collision.gameObject.layer == 0 || collision.gameObject.layer == 8 || collision.gameObject.layer == 24 || collision.gameObject.CompareTag("Player") || collision.gameObject.layer == 10 || collision.gameObject.layer == 11 || collision.gameObject.layer == 12 || collision.gameObject.layer == 26))
		{
			Explode();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!crashing)
		{
			return;
		}
		if ((type == EnemyType.Drone && (other.gameObject.layer == 10 || other.gameObject.layer == 11 || other.gameObject.layer == 12)) || (!other.isTrigger && (other.gameObject.layer == 0 || other.gameObject.layer == 8 || other.gameObject.layer == 24 || other.gameObject.layer == 26 || other.gameObject.CompareTag("Player"))))
		{
			Explode();
		}
		else
		{
			if (type == EnemyType.Drone || (other.gameObject.layer != 10 && other.gameObject.layer != 11 && other.gameObject.layer != 12) || checkingForCrash)
			{
				return;
			}
			checkingForCrash = true;
			EnemyIdentifierIdentifier component = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
			EnemyIdentifier enemyIdentifier = ((!component || !component.eid) ? other.gameObject.GetComponent<EnemyIdentifier>() : component.eid);
			if ((bool)enemyIdentifier)
			{
				bool flag = true;
				if (!enemyIdentifier.dead)
				{
					flag = false;
				}
				enemyIdentifier.hitter = "cannonball";
				enemyIdentifier.DeliverDamage(other.gameObject, (other.transform.position - base.transform.position).normalized * 100f, base.transform.position, 5f * enemyIdentifier.totalDamageModifier, tryForExplode: true);
				if (!enemyIdentifier || enemyIdentifier.dead)
				{
					if (!flag)
					{
						MonoSingleton<StyleHUD>.Instance.AddPoints(50, "ultrakill.cannonballed", null, enemyIdentifier);
					}
					if ((bool)enemyIdentifier)
					{
						enemyIdentifier.Explode();
					}
					checkingForCrash = false;
				}
				else
				{
					Explode();
				}
			}
			else
			{
				checkingForCrash = false;
			}
		}
	}

	private void CanInterruptCrash()
	{
		canInterruptCrash = true;
	}

	public void Hooked()
	{
		hooked = true;
		lockPosition = true;
		homeRunnable = true;
		CancelInvoke("DelayedUnhooked");
	}

	public void Unhooked()
	{
		hooked = false;
		Invoke("DelayedUnhooked", 0.25f);
	}

	private void DelayedUnhooked()
	{
		if (!crashing)
		{
			Invoke("NoMoreHomeRun", 0.5f);
		}
		lockPosition = false;
	}

	private void NoMoreHomeRun()
	{
		if (!crashing)
		{
			homeRunnable = false;
		}
	}

	public void Enrage()
	{
		if (!isEnraged && type != EnemyType.Drone)
		{
			isEnraged = true;
			currentEnrageEffect = UnityEngine.Object.Instantiate(enrageEffect, base.transform.position, base.transform.rotation);
			currentEnrageEffect.transform.SetParent(base.transform, worldPositionStays: true);
			enraged = true;
			EnemySimplifier[] componentsInChildren = GetComponentsInChildren<EnemySimplifier>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enraged = true;
			}
		}
	}

	public void UnEnrage()
	{
		if (isEnraged)
		{
			isEnraged = false;
			EnemySimplifier[] componentsInChildren = GetComponentsInChildren<EnemySimplifier>();
			if (componentsInChildren == null || componentsInChildren.Length == 0)
			{
				componentsInChildren = GetComponentsInChildren<EnemySimplifier>();
			}
			UnityEngine.Object.Destroy(currentEnrageEffect);
			EnemySimplifier[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enraged = false;
			}
		}
	}
}

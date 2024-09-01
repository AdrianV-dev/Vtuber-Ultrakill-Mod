using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Machine : MonoBehaviour
{
	public float health;

	private BloodsplatterManager bsm;

	public bool limp;

	private EnemyIdentifier eid;

	public GameObject chest;

	private float chestHP = 3f;

	private AudioSource aud;

	public AudioClip[] hurtSounds;

	[HideInInspector]
	public StyleCalculator scalc;

	private GoreZone gz;

	public Material deadMaterial;

	private Material originalMaterial;

	public SkinnedMeshRenderer smr;

	private NavMeshAgent nma;

	private Rigidbody rb;

	private Rigidbody[] rbs;

	private Animator anim;

	public AudioClip deathSound;

	public AudioClip scream;

	private bool noheal;

	public bool bigKill;

	public bool thickLimbs;

	public bool parryable;

	public bool partiallyParryable;

	[HideInInspector]
	public List<Transform> parryables = new List<Transform>();

	private SwordsMachine sm;

	private Streetcleaner sc;

	private V2 v2;

	private Mindflayer mf;

	private Sisyphus sisy;

	private Turret tur;

	private Ferryman fm;

	private Mannequin man;

	private Minotaur min;

	private Gutterman gm;

	public GameObject[] destroyOnDeath;

	public Machine symbiote;

	private bool symbiotic;

	private bool healing;

	public bool grounded;

	[HideInInspector]
	public GroundCheckEnemy gc;

	public bool knockedBack;

	public bool overrideFalling;

	private float knockBackCharge;

	public float brakes;

	public float juggleWeight;

	public bool falling;

	private LayerMask lmask;

	private LayerMask lmaskWater;

	private float fallSpeed;

	private float fallTime;

	private float reduceFallTime;

	public bool noFallDamage;

	public bool dontDie;

	public bool dismemberment;

	public bool specialDeath;

	public bool simpleDeath;

	[HideInInspector]
	public bool musicRequested;

	public UnityEvent onDeath;

	private int parryFramesLeft;

	private bool parryFramesOnPartial;

	public Transform hitJiggleRoot;

	private Vector3 jiggleRootPosition;

	private void Awake()
	{
		nma = GetComponent<NavMeshAgent>();
		bsm = MonoSingleton<BloodsplatterManager>.Instance;
		rbs = GetComponentsInChildren<Rigidbody>();
		anim = GetComponentInChildren<Animator>();
		eid = GetComponent<EnemyIdentifier>();
		gc = GetComponentInChildren<GroundCheckEnemy>();
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		if (smr != null)
		{
			originalMaterial = smr.material;
		}
		switch (eid.enemyType)
		{
		case EnemyType.Swordsmachine:
			sm = GetComponent<SwordsMachine>();
			break;
		case EnemyType.Streetcleaner:
			sc = GetComponent<Streetcleaner>();
			break;
		case EnemyType.V2:
			v2 = GetComponent<V2>();
			break;
		case EnemyType.Mindflayer:
			mf = GetComponent<Mindflayer>();
			break;
		case EnemyType.Sisyphus:
			sisy = GetComponent<Sisyphus>();
			break;
		case EnemyType.Turret:
			tur = GetComponent<Turret>();
			break;
		case EnemyType.Ferryman:
			fm = GetComponent<Ferryman>();
			break;
		case EnemyType.Mannequin:
			man = GetComponent<Mannequin>();
			break;
		case EnemyType.Minotaur:
			min = GetComponent<Minotaur>();
			break;
		case EnemyType.Gutterman:
			gm = GetComponent<Gutterman>();
			break;
		}
		if (symbiote != null)
		{
			symbiotic = true;
		}
		if (!gz)
		{
			gz = GoreZone.ResolveGoreZone(base.transform);
		}
		if ((bool)hitJiggleRoot)
		{
			jiggleRootPosition = hitJiggleRoot.localPosition;
		}
		if (!musicRequested && !eid.dead && (sm == null || !eid.IgnorePlayer))
		{
			musicRequested = true;
			MonoSingleton<MusicManager>.Instance.PlayBattleMusic();
		}
		if (limp && !mf)
		{
			noheal = true;
		}
		lmask = (int)lmask | 0x100;
		lmask = (int)lmask | 0x1000000;
		lmaskWater = lmask;
		lmaskWater = (int)lmaskWater | 0x10;
	}

	private void OnEnable()
	{
		parryable = false;
		partiallyParryable = false;
	}

	private void Update()
	{
		if (knockBackCharge > 0f)
		{
			knockBackCharge = Mathf.MoveTowards(knockBackCharge, 0f, Time.deltaTime);
		}
		if (healing && !limp && (bool)symbiote)
		{
			health = Mathf.MoveTowards(health, symbiote.health, Time.deltaTime * 10f);
			eid.health = health;
			if (health >= symbiote.health)
			{
				healing = false;
				if ((bool)sm)
				{
					sm.downed = false;
				}
				if ((bool)sisy)
				{
					sisy.downed = false;
				}
			}
		}
		if (falling && rb != null && !overrideFalling && (!nma || !nma.isOnOffMeshLink))
		{
			fallTime += Time.deltaTime;
			if ((bool)man)
			{
				noFallDamage = man.inControl;
				if (fallTime > 0.2f && !man.inControl)
				{
					parryable = true;
				}
			}
			if (gc.onGround && falling && nma != null)
			{
				if (fallSpeed <= -60f && !noFallDamage && !InvincibleEnemies.Enabled && !eid.blessed && (!gc.fallSuppressed || eid.unbounceable))
				{
					if (eid == null)
					{
						eid = GetComponent<EnemyIdentifier>();
					}
					eid.Splatter();
					return;
				}
				fallSpeed = 0f;
				nma.updatePosition = true;
				nma.updateRotation = true;
				if (!sm || !sm.moveAtTarget)
				{
					rb.isKinematic = true;
				}
				if (aud == null)
				{
					aud = GetComponent<AudioSource>();
				}
				if ((bool)aud && aud.clip == scream && aud.isPlaying)
				{
					aud.Stop();
				}
				rb.useGravity = false;
				nma.enabled = true;
				nma.Warp(base.transform.position);
				falling = false;
				anim.SetBool("Falling", value: false);
				if ((bool)man)
				{
					if (fallTime > 0.2f)
					{
						man.Landing();
					}
					else
					{
						man.inControl = true;
					}
					man.ResetMovementTarget();
				}
			}
			else if (eid.underwater && (bool)aud && aud.clip == scream && aud.isPlaying)
			{
				aud.Stop();
			}
			else if (fallTime > 0.05f && rb.velocity.y < fallSpeed)
			{
				fallSpeed = rb.velocity.y;
				reduceFallTime = 0.5f;
				if (aud == null)
				{
					aud = GetComponent<AudioSource>();
				}
				if ((bool)aud && !aud.isPlaying && !limp && !noFallDamage && !eid.underwater && (!Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, float.PositiveInfinity, lmaskWater, QueryTriggerInteraction.Collide) || ((hitInfo.distance > 42f || rb.velocity.y < -60f) && hitInfo.transform.gameObject.layer != 4)))
				{
					aud.clip = scream;
					aud.volume = 1f;
					aud.priority = 78;
					aud.pitch = Random.Range(0.8f, 1.2f);
					aud.Play();
				}
			}
			else if (fallTime > 0.05f && rb.velocity.y > fallSpeed)
			{
				reduceFallTime = Mathf.MoveTowards(reduceFallTime, 0f, Time.deltaTime);
				if (reduceFallTime <= 0f)
				{
					fallSpeed = rb.velocity.y;
				}
			}
			else if (rb.velocity.y > 0f)
			{
				fallSpeed = 0f;
			}
		}
		else if (fallTime > 0f)
		{
			fallTime = 0f;
		}
	}

	private void FixedUpdate()
	{
		if (parryFramesLeft > 0)
		{
			parryFramesLeft--;
		}
		if (!limp && gc != null && !overrideFalling)
		{
			if (knockedBack && knockBackCharge <= 0f && (rb.velocity.magnitude < 1f || v2 != null) && gc.onGround)
			{
				StopKnockBack();
			}
			else if (knockedBack)
			{
				if (eid.useBrakes || gc.onGround)
				{
					if (knockBackCharge <= 0f && gc.onGround)
					{
						brakes = Mathf.MoveTowards(brakes, 0f, 0.0005f * brakes);
					}
					rb.velocity = new Vector3(rb.velocity.x * 0.95f * brakes, rb.velocity.y - juggleWeight, rb.velocity.z * 0.95f * brakes);
				}
				else if (!eid.useBrakes)
				{
					brakes = 1f;
				}
				if (nma != null)
				{
					nma.updatePosition = false;
					nma.updateRotation = false;
					nma.enabled = false;
					rb.isKinematic = false;
					rb.useGravity = true;
				}
			}
			if (!grounded && gc.onGround)
			{
				grounded = true;
			}
			else if (grounded && !gc.onGround)
			{
				grounded = false;
			}
			if (!gc.onGround && !falling && nma != null && (!nma.enabled || !nma.isOnOffMeshLink))
			{
				rb.isKinematic = false;
				rb.useGravity = true;
				nma.enabled = false;
				falling = true;
				anim.SetBool("Falling", value: true);
				if (sc != null)
				{
					sc.StopFire();
				}
				if (tur != null)
				{
					tur.CancelAim(instant: true);
				}
				if ((bool)man && man.inAction && !man.jumping && !man.inControl)
				{
					man.CancelActions();
				}
			}
		}
		if (hitJiggleRoot != null && hitJiggleRoot.localPosition != jiggleRootPosition)
		{
			hitJiggleRoot.localPosition = Vector3.MoveTowards(hitJiggleRoot.localPosition, jiggleRootPosition, (Vector3.Distance(hitJiggleRoot.localPosition, jiggleRootPosition) + 1f) * 100f * Time.fixedDeltaTime);
		}
	}

	public void KnockBack(Vector3 force)
	{
		if ((!(sc == null) && sc.dodging) || (!(sm == null) && sm.inAction) || (!(tur == null) && tur.lodged) || eid.poise)
		{
			return;
		}
		if (nma != null)
		{
			nma.enabled = false;
			rb.isKinematic = false;
			rb.useGravity = true;
		}
		if ((bool)man)
		{
			man.inControl = false;
			if (man.clinging)
			{
				man.Uncling();
			}
		}
		if ((bool)gc && !overrideFalling)
		{
			if (!knockedBack || (!gc.onGround && rb.velocity.y < 0f))
			{
				rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
			}
			if (!gc.onGround)
			{
				rb.AddForce(Vector3.up, ForceMode.VelocityChange);
			}
		}
		if (hitJiggleRoot != null)
		{
			Vector3 vector = new Vector3(force.x, 0f, force.z);
			hitJiggleRoot.localPosition = jiggleRootPosition + vector.normalized * -0.01f;
			if (Vector3.Distance(hitJiggleRoot.localPosition, jiggleRootPosition) > 0.1f)
			{
				hitJiggleRoot.localPosition = jiggleRootPosition + (hitJiggleRoot.localPosition - jiggleRootPosition).normalized * 0.1f;
			}
		}
		rb.AddForce(force / 10f, ForceMode.VelocityChange);
		knockedBack = true;
		knockBackCharge = Mathf.Min(knockBackCharge + force.magnitude / 1500f, 0.35f);
		brakes = 1f;
	}

	public void StopKnockBack()
	{
		knockBackCharge = 0f;
		if (nma != null)
		{
			if (gc.onGround && Physics.Raycast(base.transform.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, float.PositiveInfinity, lmask))
			{
				_ = Vector3.zero;
				if (NavMesh.SamplePosition(hitInfo.point, out var hit, 4f, nma.areaMask))
				{
					knockedBack = false;
					nma.updatePosition = true;
					nma.updateRotation = true;
					nma.enabled = true;
					if ((!sm || !sm.moveAtTarget) && (!man || !man.jumping))
					{
						rb.isKinematic = true;
					}
					if ((bool)man)
					{
						man.inControl = true;
					}
					juggleWeight = 0f;
					nma.Warp(hit.position);
				}
				else
				{
					knockBackCharge = 0.5f;
				}
			}
			else
			{
				knockBackCharge = 0.5f;
			}
		}
		else if (v2 != null)
		{
			knockedBack = false;
			juggleWeight = 0f;
		}
	}

	public void GetHurt(GameObject target, Vector3 force, float multiplier, float critMultiplier, GameObject sourceWeapon = null, bool fromExplosion = false)
	{
		string hitLimb = "";
		bool dead = false;
		bool flag = false;
		float num = multiplier;
		GameObject gameObject = null;
		if (eid == null)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		if (force != Vector3.zero && !limp && sm == null && (v2 == null || !v2.inIntro) && (tur == null || !tur.lodged || eid.hitter == "heavypunch" || eid.hitter == "railcannon" || eid.hitter == "cannonball" || eid.hitter == "hammer"))
		{
			if ((bool)tur && tur.lodged)
			{
				tur.CancelAim(instant: true);
				tur.Unlodge();
			}
			KnockBack(force / 100f);
			if (eid.hitter == "heavypunch" || ((bool)gc && !gc.onGround && eid.hitter == "cannonball"))
			{
				eid.useBrakes = false;
			}
			else
			{
				eid.useBrakes = true;
			}
		}
		if (v2 != null && v2.secondEncounter && eid.hitter == "heavypunch")
		{
			v2.InstaEnrage();
		}
		if (sc != null && target.gameObject == sc.canister && !sc.canisterHit && eid.hitter == "revolver")
		{
			if (!InvincibleEnemies.Enabled && !eid.blessed)
			{
				sc.canisterHit = true;
			}
			if (!eid.dead && !InvincibleEnemies.Enabled && !eid.blessed)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(200, "ultrakill.instakill", sourceWeapon, eid);
			}
			MonoSingleton<TimeController>.Instance.ParryFlash();
			Invoke("CanisterExplosion", 0.1f);
			return;
		}
		if (tur != null && tur.aiming && (eid.hitter == "revolver" || eid.hitter == "coin") && tur.interruptables.Contains(target.transform))
		{
			tur.Interrupt();
		}
		if ((bool)gm)
		{
			if (gm.hasShield && !eid.dead && (eid.hitter == "heavypunch" || eid.hitter == "hammer"))
			{
				gm.ShieldBreak();
			}
			if (gm.hasShield)
			{
				multiplier /= 1.5f;
			}
			if (gm.fallen && !gm.exploded && eid.hitter == "ground slam")
			{
				gm.Explode();
				MonoSingleton<NewMovement>.Instance.Launch(Vector3.up * 750f);
			}
		}
		if ((bool)mf && mf.dying && eid.hitter == "heavypunch")
		{
			mf.DeadLaunch(force);
		}
		if (eid.hitter == "punch")
		{
			bool flag2 = parryables != null && parryables.Count > 0 && parryables.Contains(target.transform);
			if (parryable || (partiallyParryable && (flag2 || (parryFramesLeft > 0 && parryFramesOnPartial))))
			{
				parryable = false;
				partiallyParryable = false;
				parryables.Clear();
				if (!InvincibleEnemies.Enabled && !eid.blessed)
				{
					health -= ((parryFramesLeft > 0) ? 4 : 5);
				}
				MonoSingleton<FistControl>.Instance.currentPunch.Parry(hook: false, eid);
				if (sm != null && health > 0f)
				{
					if (!sm.enraged)
					{
						sm.Knockdown(light: true, fromExplosion);
					}
					else
					{
						sm.Enrage();
					}
				}
				else
				{
					SendMessage("GotParried", SendMessageOptions.DontRequireReceiver);
				}
			}
			else
			{
				parryFramesOnPartial = flag2;
				parryFramesLeft = MonoSingleton<FistControl>.Instance.currentPunch.activeFrames;
			}
		}
		else if ((bool)min && min.ramTimer > 0f && eid.hitter == "ground slam")
		{
			min.GotSlammed();
		}
		if ((bool)sisy && num > 0f)
		{
			if (eid.burners.Count > 0)
			{
				if (eid.hitter != "fire")
				{
					if (num <= 0.5f)
					{
						gameObject = bsm.GetGore(GoreType.Limb, eid, fromExplosion);
						sisy.PlayHurtSound(1);
					}
					else
					{
						gameObject = bsm.GetGore(GoreType.Head, eid, fromExplosion);
						sisy.PlayHurtSound(2);
					}
				}
				else
				{
					sisy.PlayHurtSound();
				}
			}
			else if (eid.hitter != "fire")
			{
				gameObject = bsm.GetGore(GoreType.Smallest, eid, fromExplosion);
			}
		}
		float num2 = 0f;
		if (target.gameObject.CompareTag("Head"))
		{
			num2 = 1f;
		}
		else if (target.gameObject.CompareTag("Limb") || target.gameObject.CompareTag("EndLimb"))
		{
			num2 = 0.5f;
		}
		num = multiplier + num2 * multiplier * critMultiplier;
		if (num2 == 0f && (eid.hitter == "shotgunzone" || eid.hitter == "hammerzone"))
		{
			if (!parryable && (target.gameObject != chest || health - num > 0f))
			{
				num = 0f;
			}
			else if ((parryable && (target.gameObject == chest || MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity().magnitude > 18f)) || (partiallyParryable && parryables != null && parryables.Contains(target.transform)))
			{
				num *= 1.5f;
				parryable = false;
				partiallyParryable = false;
				parryables.Clear();
				MonoSingleton<NewMovement>.Instance.Parry(eid);
				if (sm != null && health - num > 0f)
				{
					if (!sm.enraged)
					{
						sm.Knockdown(light: true, fromExplosion);
					}
					else
					{
						sm.Enrage();
					}
				}
				else
				{
					SendMessage("GotParried", SendMessageOptions.DontRequireReceiver);
				}
			}
		}
		if ((bool)sisy && !limp && eid.hitter == "fire" && health > 0f && health - num < 0.01f && !eid.isGasolined)
		{
			num = health - 0.01f;
		}
		if (!eid.blessed && !InvincibleEnemies.Enabled)
		{
			health -= num;
		}
		if (!gameObject && eid.hitter != "fire" && num > 0f)
		{
			if ((num2 == 1f && (num >= 1f || health <= 0f)) || eid.hitter == "hammer")
			{
				gameObject = bsm.GetGore(GoreType.Head, eid, fromExplosion);
			}
			else if (((num >= 1f || health <= 0f) && eid.hitter != "explosion") || (eid.hitter == "explosion" && target.gameObject.CompareTag("EndLimb")))
			{
				gameObject = ((!target.gameObject.CompareTag("Body")) ? bsm.GetGore(GoreType.Limb, eid, fromExplosion) : bsm.GetGore(GoreType.Body, eid, fromExplosion));
			}
			else if (eid.hitter != "explosion")
			{
				gameObject = bsm.GetGore(GoreType.Small, eid, fromExplosion);
			}
		}
		if (!limp)
		{
			flag = true;
			string text = target.gameObject.tag.ToLower();
			if (text == "endlimb")
			{
				text = "limb";
			}
			hitLimb = text;
		}
		if (health <= 0f)
		{
			if (symbiotic)
			{
				if (sm != null && !sm.downed && symbiote.health > 0f)
				{
					sm.downed = true;
					sm.Down(fromExplosion);
					Invoke("StartHealing", 3f);
				}
				else if (sisy != null && !sisy.downed && symbiote.health > 0f)
				{
					sisy.downed = true;
					sisy.Knockdown(base.transform.position + base.transform.forward);
					Invoke("StartHealing", 3f);
				}
				else if (symbiote.health <= 0f)
				{
					symbiotic = false;
					if (!limp)
					{
						GoLimp(fromExplosion);
					}
				}
			}
			else
			{
				if (!limp)
				{
					GoLimp(fromExplosion);
				}
				if (MonoSingleton<BloodsplatterManager>.Instance.goreOn && !target.gameObject.CompareTag("EndLimb"))
				{
					float num3 = 1f;
					if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone" || eid.hitter == "explosion")
					{
						num3 = 0.5f;
					}
					string text2 = target.gameObject.tag;
					if (!(text2 == "Head"))
					{
						if (text2 == "Limb")
						{
							for (int i = 0; (float)i < 4f * num3; i++)
							{
								GameObject gib = bsm.GetGib(BSType.gib);
								if ((bool)gib && (bool)gz && (bool)gz.gibZone)
								{
									ReadyGib(gib, target);
								}
							}
							if (target.transform.childCount > 0 && dismemberment)
							{
								Transform child = target.transform.GetChild(0);
								CharacterJoint[] componentsInChildren = target.GetComponentsInChildren<CharacterJoint>();
								if (componentsInChildren.Length != 0)
								{
									CharacterJoint[] array = componentsInChildren;
									foreach (CharacterJoint characterJoint in array)
									{
										if (StockMapInfo.Instance.removeGibsWithoutAbsorbers && characterJoint.TryGetComponent<EnemyIdentifierIdentifier>(out var component))
										{
											component.Invoke("DestroyLimbIfNotTouchedBloodAbsorber", StockMapInfo.Instance.gibRemoveTime);
										}
										Object.Destroy(characterJoint);
									}
								}
								CharacterJoint component2 = target.GetComponent<CharacterJoint>();
								if (component2 != null)
								{
									component2.connectedBody = null;
									Object.Destroy(component2);
								}
								target.transform.position = child.position;
								target.transform.SetParent(child);
								child.SetParent(gz.gibZone);
								Object.Destroy(target.GetComponent<Rigidbody>());
							}
						}
					}
					else
					{
						for (int k = 0; (float)k < 6f * num3; k++)
						{
							GameObject gib = bsm.GetGib(BSType.skullChunk);
							if ((bool)gib && (bool)gz && (bool)gz.gibZone)
							{
								ReadyGib(gib, target);
							}
						}
						for (int l = 0; (float)l < 4f * num3; l++)
						{
							GameObject gib = bsm.GetGib(BSType.brainChunk);
							if ((bool)gib && (bool)gz && (bool)gz.gibZone)
							{
								ReadyGib(gib, target);
							}
						}
						for (int m = 0; (float)m < 2f * num3; m++)
						{
							GameObject gib = bsm.GetGib(BSType.eyeball);
							if ((bool)gib && (bool)gz && (bool)gz.gibZone)
							{
								ReadyGib(gib, target);
							}
							gib = bsm.GetGib(BSType.jawChunk);
							if ((bool)gib && (bool)gz && (bool)gz.gibZone)
							{
								ReadyGib(gib, target);
							}
						}
					}
				}
				if (dismemberment)
				{
					if (!target.gameObject.CompareTag("Body"))
					{
						if (target.TryGetComponent<Collider>(out var component3))
						{
							Object.Destroy(component3);
						}
						target.transform.localScale = Vector3.zero;
					}
					else if (target.gameObject == chest && v2 == null && sc == null)
					{
						chestHP -= num;
						if (chestHP <= 0f || eid.hitter == "shotgunzone" || eid.hitter == "hammerzone")
						{
							CharacterJoint[] componentsInChildren2 = target.GetComponentsInChildren<CharacterJoint>();
							if (componentsInChildren2.Length != 0)
							{
								CharacterJoint[] array = componentsInChildren2;
								foreach (CharacterJoint characterJoint2 in array)
								{
									if (characterJoint2.transform.parent.parent == chest.transform)
									{
										if (StockMapInfo.Instance.removeGibsWithoutAbsorbers && characterJoint2.TryGetComponent<EnemyIdentifierIdentifier>(out var component4))
										{
											component4.Invoke("DestroyLimbIfNotTouchedBloodAbsorber", StockMapInfo.Instance.gibRemoveTime);
										}
										Object.Destroy(characterJoint2);
										characterJoint2.transform.parent = null;
									}
								}
							}
							if (MonoSingleton<BloodsplatterManager>.Instance.goreOn)
							{
								for (int n = 0; n < 2; n++)
								{
									GameObject gib2 = bsm.GetGib(BSType.gib);
									if ((bool)gib2 && (bool)gz && (bool)gz.gibZone)
									{
										ReadyGib(gib2, target);
									}
								}
							}
							GameObject gore = bsm.GetGore(GoreType.Head, eid, fromExplosion);
							gore.transform.position = target.transform.position;
							gore.transform.SetParent(gz.goreZone, worldPositionStays: true);
							target.transform.localScale = Vector3.zero;
						}
					}
				}
			}
			if (limp)
			{
				Rigidbody componentInParent = target.GetComponentInParent<Rigidbody>();
				if (componentInParent != null)
				{
					componentInParent.AddForce(force);
				}
			}
		}
		if (gameObject != null)
		{
			if (!gz)
			{
				gz = GoreZone.ResolveGoreZone(base.transform);
			}
			if (thickLimbs && target.TryGetComponent<Collider>(out var component5))
			{
				gameObject.transform.position = component5.ClosestPoint(MonoSingleton<NewMovement>.Instance.transform.position);
			}
			else
			{
				gameObject.transform.position = target.transform.position;
			}
			if (eid.hitter == "drill")
			{
				gameObject.transform.localScale *= 2f;
			}
			if (gz != null && gz.goreZone != null)
			{
				gameObject.transform.SetParent(gz.goreZone, worldPositionStays: true);
			}
			Bloodsplatter component6 = gameObject.GetComponent<Bloodsplatter>();
			if ((bool)component6)
			{
				ParticleSystem.CollisionModule collision = component6.GetComponent<ParticleSystem>().collision;
				if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone" || eid.hitter == "explosion")
				{
					if (Random.Range(0f, 1f) > 0.5f)
					{
						collision.enabled = false;
					}
					component6.hpAmount = 3;
				}
				else if (eid.hitter == "nail")
				{
					component6.hpAmount = 1;
					component6.GetComponent<AudioSource>().volume *= 0.8f;
				}
				if (!noheal)
				{
					component6.GetReady();
				}
			}
		}
		if ((health > 0f || symbiotic) && hurtSounds.Length != 0 && !eid.blessed)
		{
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
			if ((bool)tur)
			{
				aud.volume = 0.85f;
			}
			else if ((bool)min)
			{
				aud.volume = 1f;
			}
			else
			{
				aud.volume = 0.5f;
			}
			if (sm != null)
			{
				aud.pitch = Random.Range(0.85f, 1.35f);
			}
			else
			{
				aud.pitch = Random.Range(0.9f, 1.1f);
			}
			aud.priority = 12;
			aud.Play();
		}
		if (num == 0f || eid.puppet)
		{
			flag = false;
		}
		if (!flag || !(eid.hitter != "enemy"))
		{
			return;
		}
		if (scalc == null)
		{
			scalc = MonoSingleton<StyleCalculator>.Instance;
		}
		if (health <= 0f && !symbiotic && (v2 == null || !v2.dontDie) && (!eid.flying || (bool)mf))
		{
			dead = true;
			if ((bool)gc && !gc.onGround)
			{
				if (eid.hitter == "explosion" || eid.hitter == "ffexplosion" || eid.hitter == "railcannon")
				{
					scalc.shud.AddPoints(120, "ultrakill.fireworks", sourceWeapon, eid);
				}
				else if (eid.hitter == "ground slam")
				{
					scalc.shud.AddPoints(160, "ultrakill.airslam", sourceWeapon, eid);
				}
				else if (eid.hitter != "deathzone")
				{
					scalc.shud.AddPoints(50, "ultrakill.airshot", sourceWeapon, eid);
				}
			}
		}
		else if (health > 0f && (bool)gc && !gc.onGround && (eid.hitter == "explosion" || eid.hitter == "ffexplosion" || eid.hitter == "railcannon"))
		{
			scalc.shud.AddPoints(20, "ultrakill.fireworksweak", sourceWeapon, eid);
		}
		if (eid.hitter != "secret")
		{
			if (bigKill)
			{
				scalc.HitCalculator(eid.hitter, "spider", hitLimb, dead, eid, sourceWeapon);
			}
			else
			{
				scalc.HitCalculator(eid.hitter, "machine", hitLimb, dead, eid, sourceWeapon);
			}
		}
	}

	public void GoLimp()
	{
		GoLimp(fromExplosion: false);
	}

	public void GoLimp(bool fromExplosion = false)
	{
		if (limp)
		{
			return;
		}
		if (!gz)
		{
			gz = GoreZone.ResolveGoreZone(base.transform);
		}
		onDeath?.Invoke();
		if (smr != null)
		{
			smr.updateWhenOffscreen = true;
		}
		if (health > 0f)
		{
			health = 0f;
		}
		if (!mf)
		{
			Invoke("StopHealing", 1f);
		}
		if ((bool)v2)
		{
			v2.active = false;
			v2.Die();
		}
		if ((bool)mf)
		{
			mf.active = false;
		}
		if ((bool)tur)
		{
			tur.OnDeath();
		}
		if ((bool)fm)
		{
			fm.OnDeath();
		}
		if ((bool)man)
		{
			man.OnDeath();
		}
		SwingCheck2[] componentsInChildren = GetComponentsInChildren<SwingCheck2>();
		if (sm != null)
		{
			anim.StopPlayback();
			SwingCheck2[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				Object.Destroy(array[i]);
			}
			sm.CoolSword();
			if (sm.currentEnrageEffect != null)
			{
				Object.Destroy(sm.currentEnrageEffect);
			}
			Object.Destroy(sm);
		}
		if (sc != null)
		{
			if (anim != null)
			{
				anim.StopPlayback();
			}
			BulletCheck componentInChildren = GetComponentInChildren<BulletCheck>();
			if (componentInChildren != null)
			{
				Object.Destroy(componentInChildren.gameObject);
			}
			sc.hose.SetParent(sc.hoseTarget, worldPositionStays: true);
			sc.hose.transform.localPosition = Vector3.zero;
			sc.hose.transform.localScale = Vector3.zero;
			sc.StopFire();
			sc.dead = true;
			sc.damaging = false;
			FireZone componentInChildren2 = GetComponentInChildren<FireZone>();
			if ((bool)componentInChildren2)
			{
				Object.Destroy(componentInChildren2.gameObject);
			}
			if (sc.canister != null)
			{
				sc.canister.GetComponentInChildren<ParticleSystem>().Stop();
				AudioSource componentInChildren3 = sc.canister.GetComponentInChildren<AudioSource>();
				if (componentInChildren3 != null)
				{
					if (componentInChildren3.TryGetComponent<AudioLowPassFilter>(out var component))
					{
						Object.Destroy(component);
					}
					Object.Destroy(componentInChildren3);
				}
			}
		}
		if (destroyOnDeath.Length != 0)
		{
			GameObject[] array2 = destroyOnDeath;
			foreach (GameObject gameObject in array2)
			{
				if (gameObject.activeInHierarchy)
				{
					Transform transform = gameObject.GetComponentInParent<Rigidbody>().transform;
					if ((bool)transform)
					{
						gameObject.transform.SetParent(transform);
						gameObject.transform.position = transform.position;
						gameObject.transform.localScale = Vector3.zero;
					}
				}
			}
		}
		if (!dontDie && !eid.dontCountAsKills && !limp)
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
			ActivateNextWave componentInParent = GetComponentInParent<ActivateNextWave>();
			if (componentInParent != null)
			{
				componentInParent.AddDeadEnemy();
			}
		}
		EnemySimplifier[] componentsInChildren2 = GetComponentsInChildren<EnemySimplifier>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].Begone();
		}
		if (deadMaterial != null)
		{
			smr.sharedMaterial = deadMaterial;
		}
		else if (smr != null && !mf)
		{
			smr.sharedMaterial = originalMaterial;
		}
		if (nma != null)
		{
			Object.Destroy(nma);
			nma = null;
		}
		if (!v2 && !specialDeath)
		{
			Object.Destroy(anim);
			Object.Destroy(base.gameObject.GetComponent<Collider>());
			if (rb == null)
			{
				rb = GetComponent<Rigidbody>();
			}
			Object.Destroy(rb);
		}
		if (aud == null)
		{
			aud = GetComponent<AudioSource>();
		}
		if (deathSound != null)
		{
			aud.clip = deathSound;
			aud.pitch = Random.Range(0.85f, 1.35f);
			aud.priority = 11;
			aud.Play();
			if ((bool)tur)
			{
				aud.volume = 1f;
			}
		}
		if (!limp)
		{
			SendMessage("Death", SendMessageOptions.DontRequireReceiver);
			if (eid.hitter != "spin")
			{
				if (simpleDeath)
				{
					Explosion[] componentsInChildren3 = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.explosion, base.transform.position, base.transform.rotation).GetComponentsInChildren<Explosion>();
					for (int i = 0; i < componentsInChildren3.Length; i++)
					{
						componentsInChildren3[i].canHit = AffectedSubjects.EnemiesOnly;
					}
					Object.Destroy(base.gameObject);
				}
				else if (!specialDeath && !v2 && !mf)
				{
					rbs = GetComponentsInChildren<Rigidbody>();
					Rigidbody[] array3 = rbs;
					foreach (Rigidbody rigidbody in array3)
					{
						if (rigidbody != null)
						{
							rigidbody.isKinematic = false;
							rigidbody.useGravity = true;
							if (StockMapInfo.Instance.removeGibsWithoutAbsorbers && rigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var component2))
							{
								component2.Invoke("DestroyLimbIfNotTouchedBloodAbsorber", StockMapInfo.Instance.gibRemoveTime);
							}
							if ((bool)man)
							{
								rigidbody.AddForce((rigidbody.position - eid.overrideCenter.transform.position).normalized * Random.Range(20f, 30f), ForceMode.VelocityChange);
								rigidbody.AddTorque(Random.onUnitSphere * 360f, ForceMode.VelocityChange);
								Object.Instantiate(man.bloodSpray, rigidbody.transform.position, Quaternion.LookRotation(rigidbody.transform.parent.position - rigidbody.transform.position)).transform.SetParent(rigidbody.transform, worldPositionStays: true);
								rigidbody.transform.SetParent(gz.goreZone, worldPositionStays: true);
							}
						}
					}
				}
			}
			if ((bool)man)
			{
				GameObject gore = MonoSingleton<BloodsplatterManager>.Instance.GetGore(GoreType.Head, eid, fromExplosion);
				gore.transform.position = chest.transform.position;
				gore.transform.SetParent(gz.goreZone, worldPositionStays: true);
				gore.SetActive(value: true);
			}
			if (musicRequested)
			{
				MonoSingleton<MusicManager>.Instance.PlayCleanMusic();
			}
		}
		parryable = false;
		partiallyParryable = false;
		limp = true;
	}

	private void StartHealing()
	{
		if (symbiotic && symbiote != null)
		{
			healing = true;
		}
	}

	private void StopHealing()
	{
		noheal = true;
	}

	public void CanisterExplosion()
	{
		if (InvincibleEnemies.Enabled || eid.blessed)
		{
			if ((bool)sc && sc.canisterHit)
			{
				sc.canisterHit = false;
			}
			return;
		}
		eid.Explode(fromExplosion: true);
		Explosion[] componentsInChildren = Object.Instantiate(sc.explosion.ToAsset(), sc.canister.transform.position, Quaternion.identity).GetComponentsInChildren<Explosion>();
		foreach (Explosion obj in componentsInChildren)
		{
			obj.maxSize *= 1.75f;
			obj.damage = 50;
			obj.friendlyFire = true;
		}
		CharacterJoint[] componentsInChildren2 = chest.GetComponentsInChildren<CharacterJoint>();
		if (componentsInChildren2.Length != 0)
		{
			CharacterJoint[] array = componentsInChildren2;
			foreach (CharacterJoint characterJoint in array)
			{
				if (characterJoint.transform.parent.parent == chest.transform)
				{
					if (StockMapInfo.Instance.removeGibsWithoutAbsorbers && characterJoint.TryGetComponent<EnemyIdentifierIdentifier>(out var component))
					{
						component.Invoke("DestroyLimbIfNotTouchedBloodAbsorber", StockMapInfo.Instance.gibRemoveTime);
					}
					Object.Destroy(characterJoint);
					characterJoint.transform.parent = null;
				}
			}
		}
		if (MonoSingleton<BloodsplatterManager>.Instance.goreOn)
		{
			for (int j = 0; j < 2; j++)
			{
				GameObject gib = bsm.GetGib(BSType.gib);
				if ((bool)gib && (bool)gz && (bool)gz.gibZone)
				{
					ReadyGib(gib, sc.canister);
				}
			}
		}
		GameObject gore = bsm.GetGore(GoreType.Head, eid, fromExplosion: true);
		gore.transform.position = sc.canister.transform.position;
		gore.transform.SetParent(gz.goreZone, worldPositionStays: true);
		chest.transform.localScale = Vector3.zero;
		if (sc.canister.TryGetComponent<Collider>(out var component2))
		{
			Object.Destroy(component2);
		}
		sc.canister.transform.localScale = Vector3.zero;
		sc.canister.transform.parent = gz.transform;
		sc.canister.transform.position = Vector3.zero;
	}

	public void ReadyGib(GameObject tempGib, GameObject target)
	{
		tempGib.transform.SetPositionAndRotation(target.transform.position, Random.rotation);
		gz.SetGoreZone(tempGib);
		if (!OptionsMenuToManager.bloodEnabled)
		{
			tempGib.SetActive(value: false);
		}
	}

	public void ParryableCheck(bool partial = false)
	{
		if (partial)
		{
			partiallyParryable = true;
		}
		else
		{
			parryable = true;
		}
		if (parryFramesLeft > 0 && (!partial || parryFramesOnPartial))
		{
			eid.hitter = "punch";
			eid.DeliverDamage(base.gameObject, MonoSingleton<CameraController>.Instance.transform.forward * 25000f, base.transform.position, 1f, tryForExplode: false);
			parryFramesLeft = 0;
		}
	}
}

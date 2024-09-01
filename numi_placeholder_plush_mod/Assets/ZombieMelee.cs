using UnityEngine;
using UnityEngine.AI;

public class ZombieMelee : MonoBehaviour, IHitTargetCallback
{
	public bool harmless;

	public bool damaging;

	public TrailRenderer biteTrail;

	public TrailRenderer diveTrail;

	public bool track;

	public float coolDown;

	public Zombie zmb;

	private NavMeshAgent nma;

	private Animator anim;

	private EnemyIdentifier eid;

	private bool customStart;

	private bool musicRequested;

	private int difficulty = -1;

	private float defaultCoolDown = 0.5f;

	public GameObject swingSound;

	public LayerMask lmask;

	private Rigidbody rb;

	[HideInInspector]
	public SwingCheck2 swingCheck;

	[HideInInspector]
	public SwingCheck2 diveSwingCheck;

	[HideInInspector]
	public bool diving;

	private bool inAction;

	[SerializeField]
	private Transform modelTransform;

	private TimeSince randomJumpChanceCooldown;

	private bool aboutToDive;

	[SerializeField]
	private GameObject hitGroundParticle;

	[SerializeField]
	private GameObject pullOutParticle;

	private EnemySimplifier ensim;

	public Material originalMaterial;

	public Material biteMaterial;

	private void Awake()
	{
		zmb = GetComponent<Zombie>();
		eid = GetComponent<EnemyIdentifier>();
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		if (eid.difficultyOverride >= 0)
		{
			difficulty = eid.difficultyOverride;
		}
		else
		{
			difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		}
		if (difficulty != 2)
		{
			if (difficulty >= 3)
			{
				defaultCoolDown = 0.25f;
			}
			else if (difficulty == 1)
			{
				defaultCoolDown = 0.75f;
			}
			else if (difficulty == 0)
			{
				defaultCoolDown = 1f;
			}
		}
		if (!musicRequested && !eid.IgnorePlayer)
		{
			musicRequested = true;
			zmb.musicRequested = true;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if ((bool)instance)
			{
				instance.PlayBattleMusic();
			}
		}
		ensim = GetComponentInChildren<EnemySimplifier>();
		nma = zmb.nma;
		anim = zmb.anim;
		TrackTick();
	}

	private void Update()
	{
		if (diving)
		{
			modelTransform.LookAt(base.transform.position + base.transform.forward + Vector3.up * rb.velocity.normalized.y * 5f);
			modelTransform.Rotate(Vector3.right * 90f, Space.Self);
		}
		else
		{
			modelTransform.localRotation = Quaternion.identity;
		}
		if (!diving && damaging)
		{
			rb.isKinematic = false;
			float num = 1f;
			if (difficulty >= 4)
			{
				num = 1.25f;
			}
			rb.velocity = base.transform.forward * 40f * num * anim.speed;
		}
		if (track && eid.target != null)
		{
			if (difficulty > 1)
			{
				base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
			}
			else
			{
				float num2 = 720f;
				if (difficulty == 0)
				{
					num2 = 360f;
				}
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z) - base.transform.position), Time.deltaTime * num2 * eid.totalSpeedModifier);
			}
		}
		float num3 = 3f;
		if (eid.target != null && !eid.target.isPlayer)
		{
			num3 = 4f;
		}
		if (coolDown != 0f)
		{
			if (coolDown - Time.deltaTime > 0f)
			{
				coolDown -= Time.deltaTime / 2.5f * eid.totalSpeedModifier;
			}
			else
			{
				coolDown = 0f;
			}
		}
		else
		{
			if (eid.target == null || !zmb.grounded || nma.isOnOffMeshLink || aboutToDive || inAction)
			{
				return;
			}
			if (difficulty >= 4)
			{
				float num4 = Vector3.Distance(eid.target.position, base.transform.position);
				if (eid.target.position.y > base.transform.position.y + 5f && num4 < 20f && !Physics.Raycast(base.transform.position + Vector3.up, eid.target.position - (base.transform.position + Vector3.up), Vector3.Distance(eid.target.position, base.transform.position + Vector3.up), LayerMaskDefaults.Get(LMD.Environment)))
				{
					aboutToDive = true;
					Invoke("JumpAttack", Random.Range(0f, 0.5f));
				}
				else if (num4 < num3 && !damaging)
				{
					Swing();
				}
				else if (num4 < 20f && num4 > 10f && (float)randomJumpChanceCooldown > 1f)
				{
					if (Random.Range(0f, 1f) > 0.8f && !Physics.Raycast(base.transform.position + Vector3.up, eid.target.position - (base.transform.position + Vector3.up), Vector3.Distance(eid.target.position, base.transform.position + Vector3.up), LayerMaskDefaults.Get(LMD.Environment)))
					{
						JumpAttack();
					}
					randomJumpChanceCooldown = 0f;
				}
			}
			else if (Vector3.Distance(eid.target.position, base.transform.position) < num3 && !damaging)
			{
				Swing();
			}
		}
	}

	private void OnEnable()
	{
		if (zmb == null)
		{
			zmb = GetComponent<Zombie>();
		}
		if (!musicRequested && !eid.IgnorePlayer)
		{
			musicRequested = true;
			zmb.musicRequested = true;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if ((bool)instance)
			{
				instance.PlayBattleMusic();
			}
		}
		CancelAttack();
		if (zmb.grounded && (bool)rb)
		{
			rb.velocity = Vector3.zero;
			rb.isKinematic = true;
		}
	}

	private void OnDisable()
	{
		if (musicRequested && !eid.IgnorePlayer && !zmb.limp)
		{
			musicRequested = false;
			zmb.musicRequested = false;
			MusicManager instance = MonoSingleton<MusicManager>.Instance;
			if ((bool)instance)
			{
				instance.PlayCleanMusic();
			}
		}
	}

	private void FixedUpdate()
	{
		if (zmb.grounded && nma != null && nma.enabled && nma.isOnNavMesh)
		{
			if (nma.isStopped || nma.velocity == Vector3.zero)
			{
				anim.SetBool("Running", value: false);
			}
			else
			{
				anim.SetBool("Running", value: true);
			}
		}
	}

	public void JumpAttack()
	{
		aboutToDive = false;
		if (!nma.isOnOffMeshLink)
		{
			anim.Play("JumpStart");
			coolDown = defaultCoolDown;
			inAction = true;
			zmb.stopped = true;
			nma.enabled = false;
		}
	}

	public void JumpStart()
	{
		Vector3 vector = eid.target.position;
		if (eid.target.isPlayer)
		{
			vector = MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(0.5f);
		}
		base.transform.LookAt(new Vector3(vector.x, base.transform.position.y, vector.z));
		zmb.Jump(Vector3.up * 25f + Vector3.ClampMagnitude(new Vector3((vector.x - base.transform.position.x) * 2f, 0f, (vector.z - base.transform.position.z) * 2f), 25f));
		Object.Instantiate(swingSound, base.transform);
		diving = true;
		DamageStart();
		zmb.ParryableCheck();
		Invoke("CheckThatJumpStarted", 1f);
	}

	private void CheckThatJumpStarted()
	{
		if (diving && !zmb.falling)
		{
			JumpEnd();
		}
	}

	public void JumpEnd()
	{
		CancelInvoke("CheckThatJumpStarted");
		anim.Play("JumpEnd");
		DamageEnd();
		diving = false;
		zmb.attacking = false;
		Object.Instantiate(hitGroundParticle, base.transform.position, Quaternion.identity);
	}

	public void PullOut()
	{
		Object.Instantiate(pullOutParticle, base.transform.position, Quaternion.identity);
	}

	public void JumpEndEnd()
	{
		inAction = false;
	}

	public void Swing()
	{
		if (!damaging && !harmless && eid.target != null)
		{
			GetComponentInChildren<SwingCheck2>().OverrideEnemyIdentifier(eid);
			zmb.stopped = true;
			track = true;
			coolDown = defaultCoolDown;
			nma.enabled = false;
			anim.SetTrigger("Swing");
			Object.Instantiate(swingSound, base.transform);
		}
	}

	public void SwingEnd()
	{
		if (zmb.grounded)
		{
			nma.enabled = true;
		}
		zmb.stopped = false;
	}

	public void DamageStart()
	{
		if (!harmless)
		{
			damaging = true;
			if (diving)
			{
				diveTrail.emitting = true;
				diveSwingCheck.DamageStart();
			}
			else
			{
				biteTrail.enabled = true;
				swingCheck.DamageStart();
				MouthClose();
			}
		}
	}

	public void TargetBeenHit()
	{
		MouthClose();
	}

	public void DamageEnd()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		damaging = false;
		zmb.attacking = false;
		rb.velocity = Vector3.zero;
		rb.isKinematic = true;
		biteTrail.enabled = false;
		diveTrail.emitting = false;
		diving = false;
		swingCheck.DamageStop();
		diveSwingCheck.DamageStop();
	}

	public void StopTracking()
	{
		track = false;
		if (difficulty >= 4 && eid.target.isPlayer)
		{
			Vector3 vector = MonoSingleton<PlayerTracker>.Instance.PredictPlayerPosition(0.2f);
			base.transform.LookAt(new Vector3(vector.x, base.transform.position.y, vector.z));
		}
		zmb.ParryableCheck();
	}

	public void CancelAttack()
	{
		damaging = false;
		zmb.attacking = false;
		inAction = false;
		biteTrail.enabled = false;
		diveTrail.emitting = false;
		diving = false;
		zmb.stopped = false;
		track = false;
		coolDown = defaultCoolDown;
		swingCheck.DamageStop();
	}

	public void TrackTick()
	{
		if (base.gameObject.activeInHierarchy)
		{
			if (nma == null)
			{
				nma = zmb.nma;
			}
			if (zmb.grounded && !inAction && nma != null && nma.enabled && nma.isOnNavMesh && eid.target != null)
			{
				if (Physics.Raycast(eid.target.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, float.PositiveInfinity, lmask))
				{
					nma.SetDestination(hitInfo.point);
				}
				else
				{
					nma.SetDestination(eid.target.position);
				}
			}
		}
		Invoke("TrackTick", 0.1f);
	}

	public void MouthClose()
	{
		if (!eid.puppet)
		{
			if ((bool)ensim)
			{
				ensim.ChangeMaterialNew(EnemySimplifier.MaterialState.normal, biteMaterial);
			}
			CancelInvoke("MouthOpen");
			Invoke("MouthOpen", 0.75f);
		}
	}

	private void MouthOpen()
	{
		if (!eid.puppet && (bool)ensim)
		{
			ensim.ChangeMaterialNew(EnemySimplifier.MaterialState.normal, originalMaterial);
		}
	}
}

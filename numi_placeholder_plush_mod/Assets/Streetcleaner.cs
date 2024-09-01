using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;

public class Streetcleaner : MonoBehaviour
{
	private Animator anim;

	private NavMeshAgent nma;

	private Rigidbody rb;

	public bool dead;

	private TrailRenderer handTrail;

	public LayerMask enviroMask;

	public bool dodging;

	private float dodgeSpeed;

	private float dodgeCooldown;

	public GameObject dodgeSound;

	public Transform hose;

	public Transform hoseTarget;

	public GameObject canister;

	public AssetReference explosion;

	public bool canisterHit;

	public GameObject firePoint;

	private Transform warningFlame;

	private ParticleSystem firePart;

	private Light fireLight;

	private AudioSource fireAud;

	public GameObject fireStopSound;

	public bool damaging;

	private bool attacking;

	public GameObject warningFlash;

	[SerializeField]
	private Transform aimBone;

	private Quaternion torsoDefaultRotation;

	[SerializeField]
	private Transform flameThrowerBone;

	private int difficulty;

	private float cooldown;

	private RaycastHit rhit;

	private GroundCheck playergc;

	private bool playerInAir;

	private GroundCheckEnemy gc;

	private Machine mach;

	[HideInInspector]
	public EnemyIdentifier eid;

	private EnemyTarget target => eid.target;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		eid = GetComponent<EnemyIdentifier>();
		anim = GetComponentInChildren<Animator>();
		nma = GetComponent<NavMeshAgent>();
	}

	private void Start()
	{
		if (!dead)
		{
			playergc = MonoSingleton<NewMovement>.Instance.gc;
			handTrail = GetComponentInChildren<TrailRenderer>();
			handTrail.emitting = false;
			warningFlame = firePoint.GetComponentInChildren<SpriteRenderer>().transform;
			warningFlame.localScale = Vector3.zero;
			firePart = firePoint.GetComponentInChildren<ParticleSystem>();
			fireLight = firePoint.GetComponentInChildren<Light>();
			fireLight.enabled = false;
			fireAud = firePoint.GetComponent<AudioSource>();
			torsoDefaultRotation = Quaternion.Inverse(base.transform.rotation) * aimBone.rotation;
			if (eid.difficultyOverride >= 0)
			{
				difficulty = eid.difficultyOverride;
			}
			else
			{
				difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
			}
			gc = GetComponentInChildren<GroundCheckEnemy>();
			mach = GetComponent<Machine>();
			SlowUpdate();
		}
	}

	private void OnDisable()
	{
		if (dodging)
		{
			StopMoving();
		}
	}

	private void SlowUpdate()
	{
		if (dead)
		{
			return;
		}
		if (playerInAir && nma != null && nma.enabled && nma.isOnNavMesh && target != null)
		{
			if (Physics.Raycast(target.position + Vector3.up * 0.1f, Vector3.down, out rhit, 20f, enviroMask))
			{
				nma.SetDestination(rhit.point);
			}
			else
			{
				nma.SetDestination(target.position);
			}
		}
		Invoke("SlowUpdate", 0.25f);
	}

	private void Update()
	{
		if (dodgeCooldown != 0f)
		{
			dodgeCooldown = Mathf.MoveTowards(dodgeCooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (difficulty <= 2 && cooldown > 0f)
		{
			cooldown = Mathf.MoveTowards(cooldown, 0f, Time.deltaTime * eid.totalSpeedModifier);
		}
		if (target == null)
		{
			if (!dead)
			{
				if ((bool)nma && nma.isOnNavMesh)
				{
					nma.isStopped = true;
					nma.ResetPath();
				}
				if ((bool)anim)
				{
					anim.SetBool("Running", value: false);
					anim.SetBool("Walking", value: false);
				}
			}
		}
		else
		{
			if (dead)
			{
				return;
			}
			if (nma.enabled && nma.isOnNavMesh)
			{
				if (target.isPlayer)
				{
					if (playergc != null && !playergc.onGround)
					{
						playerInAir = true;
					}
					else
					{
						playerInAir = false;
						nma.SetDestination(target.position);
					}
				}
				else
				{
					playerInAir = false;
					nma.SetDestination(target.position);
					if (nma.isStopped)
					{
						nma.isStopped = false;
					}
				}
				if ((!attacking && GetDistanceToTarget() > 6f) || (attacking && GetDistanceToTarget() > 16f))
				{
					if (difficulty >= 4)
					{
						anim.SetFloat("RunSpeed", 1.5f);
					}
					else
					{
						anim.SetFloat("RunSpeed", 1f);
					}
					if (difficulty >= 4)
					{
						nma.speed = 24f;
					}
					else if (difficulty >= 2)
					{
						nma.speed = 16f;
					}
					else if (difficulty == 1)
					{
						nma.speed = 14f;
					}
					else if (difficulty == 0)
					{
						nma.speed = 10f;
					}
					nma.speed *= eid.totalSpeedModifier;
					if (attacking)
					{
						StopFire();
					}
				}
				else if (GetDistanceToTarget() < 6f)
				{
					if (difficulty >= 4)
					{
						anim.SetFloat("RunSpeed", 1.25f);
					}
					else
					{
						anim.SetFloat("RunSpeed", 1f);
					}
					if (difficulty >= 4)
					{
						nma.speed = 20f;
					}
					else if (difficulty >= 2)
					{
						nma.speed = 16f;
					}
					else if (difficulty == 1)
					{
						nma.speed = 7f;
					}
					else if (difficulty == 0)
					{
						nma.speed = 1f;
					}
					nma.speed *= eid.totalSpeedModifier;
					if (!attacking && (difficulty > 2 || cooldown <= 0f))
					{
						attacking = true;
						GameObject obj = Object.Instantiate(warningFlash, firePoint.transform.position, firePoint.transform.rotation);
						obj.transform.localScale = Vector3.one * 8f;
						obj.transform.SetParent(firePoint.transform, worldPositionStays: true);
						if (difficulty >= 2)
						{
							Invoke("StartFire", 0.5f / eid.totalSpeedModifier);
						}
						else
						{
							Invoke("StartFire", 1f / eid.totalSpeedModifier);
						}
					}
				}
				float num = 12f;
				if (difficulty == 1)
				{
					num = 10f;
				}
				else if (difficulty == 0)
				{
					num = 4f;
				}
				if (nma.velocity.magnitude > num && !attacking)
				{
					anim.SetBool("Running", value: true);
					anim.SetBool("Walking", value: true);
				}
				else if (nma.velocity.magnitude > 2f)
				{
					anim.SetBool("Running", value: false);
					anim.SetBool("Walking", value: true);
				}
				else
				{
					anim.SetBool("Running", value: false);
					anim.SetBool("Walking", value: false);
				}
			}
			if (damaging)
			{
				TryIgniteStains();
			}
		}
	}

	private void FixedUpdate()
	{
		if (target != null && !dead && dodging)
		{
			rb.velocity = base.transform.forward * -1f * dodgeSpeed * eid.totalSpeedModifier;
			dodgeSpeed = dodgeSpeed * 0.95f / eid.totalSpeedModifier;
		}
	}

	private void LateUpdate()
	{
		if (difficulty >= 4 && attacking && target != null)
		{
			float maxDegreesDelta = ((difficulty == 5) ? 90 : 35);
			Quaternion rotation = aimBone.rotation;
			Quaternion quaternion = Quaternion.RotateTowards(aimBone.rotation, Quaternion.LookRotation(eid.target.headPosition - aimBone.position, Vector3.up), maxDegreesDelta);
			Quaternion quaternion2 = Quaternion.Inverse(base.transform.rotation * torsoDefaultRotation) * aimBone.rotation;
			if (Vector3.Dot(Vector3.up, quaternion * Vector3.forward) > 0f)
			{
				aimBone.rotation = quaternion * quaternion2;
			}
			Quaternion quaternion3 = Quaternion.Inverse(rotation) * aimBone.rotation;
			quaternion3 = Quaternion.Euler(0f - quaternion3.eulerAngles.y, quaternion3.eulerAngles.z, 0f - quaternion3.eulerAngles.x);
			flameThrowerBone.rotation *= quaternion3;
		}
	}

	public void StartFire()
	{
		fireAud.Play();
		firePart.Play();
		fireLight.enabled = true;
		Invoke("StartDamaging", 0.15f / eid.totalSpeedModifier);
		if (difficulty == 0)
		{
			Invoke("StopFire", 0.5f / eid.totalSpeedModifier);
		}
		else if (difficulty <= 2)
		{
			Invoke("StopFire", 1f / eid.totalSpeedModifier);
		}
	}

	public void StartDamaging()
	{
		damaging = true;
	}

	public void StopFire()
	{
		if ((bool)fireAud && fireAud.isPlaying)
		{
			fireAud.Stop();
			Object.Instantiate(fireStopSound, firePoint.transform.position, Quaternion.identity);
		}
		attacking = false;
		CancelInvoke("StartFire");
		CancelInvoke("StartDamaging");
		firePart.Stop();
		fireLight.enabled = false;
		warningFlame.localScale = Vector3.zero;
		damaging = false;
		if (difficulty <= 2)
		{
			if (difficulty == 2)
			{
				cooldown = 1f;
			}
			else if (difficulty == 1)
			{
				cooldown = 1.5f;
			}
			else if (difficulty == 0)
			{
				cooldown = 2f;
			}
			CancelInvoke("StopFire");
		}
	}

	public void Dodge()
	{
		if (!dead && dodgeCooldown == 0f)
		{
			dodgeCooldown = Random.Range(2f, 4f);
			dodgeSpeed = 60f;
			nma.enabled = false;
			rb.isKinematic = false;
			eid.hookIgnore = true;
			StopFire();
			if ((Random.Range(0f, 1f) > 0.5f && !Physics.Raycast(base.transform.position + Vector3.up, base.transform.right * -1f, 5f, enviroMask, QueryTriggerInteraction.Ignore)) || Physics.Raycast(base.transform.position + Vector3.up, base.transform.right, 5f, enviroMask, QueryTriggerInteraction.Ignore))
			{
				base.transform.LookAt(base.transform.position + base.transform.right);
			}
			else
			{
				base.transform.LookAt(base.transform.position + base.transform.right * -1f);
			}
			Object.Instantiate(dodgeSound, base.transform.position, Quaternion.identity);
			anim.SetTrigger("Dodge");
			dodging = true;
		}
	}

	public void StopMoving()
	{
		if (dead)
		{
			return;
		}
		dodging = false;
		nma.enabled = false;
		eid.hookIgnore = false;
		if (gc.onGround)
		{
			rb.isKinematic = true;
			if (NavMesh.SamplePosition(gc.transform.position, out var hit, 4f, 1))
			{
				nma.Warp(hit.position);
				nma.enabled = true;
			}
		}
		rb.velocity = Vector3.zero;
	}

	public void DodgeEnd()
	{
	}

	public void DeflectShot()
	{
		if (!dead)
		{
			anim.SetLayerWeight(1, 1f);
			anim.SetTrigger("Deflect");
			handTrail.emitting = true;
		}
	}

	public void SlapOver()
	{
		if (!dead)
		{
			handTrail.emitting = false;
		}
	}

	public void OverrideOver()
	{
		if (!dead)
		{
			anim.SetLayerWeight(1, 0f);
		}
	}

	private float GetDistanceToTarget()
	{
		float num = Vector3.Distance(base.transform.position, target.position);
		if (target != null && target.isEnemy && target.enemyIdentifier != null)
		{
			num *= target.enemyIdentifier.GetReachDistanceMultiplier();
		}
		return num;
	}

	private void TryIgniteStains()
	{
		Vector3 position = firePoint.transform.position;
		Vector3 forward = firePoint.transform.forward;
		Vector3 worldPosition = position + forward * 3.75f;
		Vector3 worldPosition2 = position + forward * 7.5f;
		Vector3 worldPosition3 = position + forward * 15f;
		StainVoxelManager instance = MonoSingleton<StainVoxelManager>.Instance;
		if (!(instance == null))
		{
			instance.TryIgniteAt(position, 1);
			instance.TryIgniteAt(worldPosition, 1);
			instance.TryIgniteAt(worldPosition2, 1);
			instance.TryIgniteAt(worldPosition3, 1);
		}
	}
}

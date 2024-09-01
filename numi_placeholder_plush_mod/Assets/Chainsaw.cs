using System;
using System.Collections.Generic;
using UnityEngine;

public class Chainsaw : MonoBehaviour
{
	[HideInInspector]
	public Rigidbody rb;

	public float damage;

	public Transform attachedTransform;

	[HideInInspector]
	public Transform lineStartTransform;

	[SerializeField]
	private AudioSource ropeSnapSound;

	private AudioSource aud;

	public AudioSource stoppedAud;

	[SerializeField]
	private GameObject ricochetEffect;

	[SerializeField]
	private AudioClip enemyHitSound;

	[SerializeField]
	private GameObject enemyHitParticle;

	private LineRenderer lr;

	[HideInInspector]
	public bool stopped;

	public bool heated;

	public int hitAmount = 1;

	private int currentHitAmount;

	private Transform hitTarget;

	private List<Transform> hitLimbs = new List<Transform>();

	private EnemyIdentifier currentHitEnemy;

	private float multiHitCooldown;

	private float sameEnemyHitCooldown;

	[HideInInspector]
	public Vector3 originalVelocity;

	[HideInInspector]
	public bool beingPunched;

	private bool beenPunched;

	private bool inPlayer;

	private float playerHitTimer;

	private TimeSince ignorePlayerTimer;

	private float raycastBlockedTimer;

	[HideInInspector]
	public string weaponType;

	[HideInInspector]
	public GameObject sourceWeapon;

	public Nail sawbladeVersion;

	[SerializeField]
	private Renderer model;

	[SerializeField]
	private Transform sprite;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		aud = GetComponent<AudioSource>();
		lr = GetComponent<LineRenderer>();
		if (lineStartTransform == null)
		{
			lineStartTransform = attachedTransform;
		}
		ignorePlayerTimer = 0f;
		Invoke("SlowUpdate", 2f);
	}

	private void OnEnable()
	{
		MonoSingleton<WeaponCharges>.Instance.shoSawAmount++;
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<WeaponCharges>.Instance)
		{
			MonoSingleton<WeaponCharges>.Instance.shoSawAmount--;
		}
	}

	private void SlowUpdate()
	{
		if (Vector3.Distance(base.transform.position, attachedTransform.position) > 1000f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			Invoke("SlowUpdate", 2f);
		}
	}

	private void Update()
	{
		lr.SetPosition(0, lineStartTransform.position);
		lr.SetPosition(1, base.transform.position);
		if ((bool)rb)
		{
			if (inPlayer)
			{
				base.transform.forward = MonoSingleton<CameraController>.Instance.transform.forward * -1f;
			}
			else
			{
				base.transform.LookAt(base.transform.position + (base.transform.position - attachedTransform.position));
			}
		}
		if (sameEnemyHitCooldown > 0f && !stopped)
		{
			sameEnemyHitCooldown = Mathf.MoveTowards(sameEnemyHitCooldown, 0f, Time.deltaTime);
			if (sameEnemyHitCooldown <= 0f)
			{
				currentHitEnemy = null;
			}
		}
		if (inPlayer)
		{
			base.transform.position = attachedTransform.position;
			playerHitTimer = Mathf.MoveTowards(playerHitTimer, 0.25f, Time.deltaTime);
			stoppedAud.pitch = 0.5f;
			stoppedAud.volume = 0.75f;
			if (playerHitTimer >= 0.25f && !beingPunched)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
		else
		{
			if (hitAmount <= 1)
			{
				return;
			}
			if (multiHitCooldown > 0f)
			{
				multiHitCooldown = Mathf.MoveTowards(multiHitCooldown, 0f, Time.deltaTime);
			}
			else if (stopped)
			{
				if (!currentHitEnemy.dead && currentHitAmount > 0)
				{
					currentHitAmount--;
					DamageEnemy(hitTarget, currentHitEnemy);
				}
				if (currentHitEnemy.dead || currentHitAmount <= 0)
				{
					stopped = false;
					rb.velocity = originalVelocity.normalized * Mathf.Max(originalVelocity.magnitude, 35f);
					return;
				}
				multiHitCooldown = 0.05f;
			}
			if ((bool)stoppedAud)
			{
				if (stopped)
				{
					stoppedAud.pitch = 1.1f;
					stoppedAud.volume = 0.75f;
				}
				else
				{
					stoppedAud.pitch = 0.85f;
					stoppedAud.volume = 0.5f;
				}
			}
		}
	}

	private void FixedUpdate()
	{
		if (stopped)
		{
			rb.velocity = Vector3.zero;
			return;
		}
		if (Vector3.Dot(rb.velocity.normalized, (attachedTransform.position - base.transform.position).normalized) < 0.5f)
		{
			rb.velocity = Vector3.MoveTowards(rb.velocity, (attachedTransform.position - base.transform.position).normalized * 100f, Time.fixedDeltaTime * Vector3.Distance(attachedTransform.position, base.transform.position) * 10f);
		}
		else
		{
			rb.velocity = (attachedTransform.position - base.transform.position).normalized * Mathf.Min(100f, Mathf.MoveTowards(rb.velocity.magnitude, 100f, Time.fixedDeltaTime * Mathf.Max(10f, Vector3.Distance(attachedTransform.position, base.transform.position)) * 10f));
		}
		if ((float)ignorePlayerTimer > 0.66f && !inPlayer && Vector3.Distance(base.transform.position, attachedTransform.position) < 1f)
		{
			TouchPlayer();
			return;
		}
		if (Physics.Raycast(base.transform.position, attachedTransform.position - base.transform.position, Vector3.Distance(base.transform.position, attachedTransform.position), LayerMaskDefaults.Get(LMD.Environment)))
		{
			raycastBlockedTimer += Time.fixedDeltaTime;
		}
		else
		{
			raycastBlockedTimer = 0f;
		}
		if (raycastBlockedTimer >= 0.25f)
		{
			TurnIntoSawblade();
			return;
		}
		RaycastHit[] array = rb.SweepTestAll(rb.velocity.normalized, rb.velocity.magnitude * 5f * Time.fixedDeltaTime, QueryTriggerInteraction.Ignore);
		if (array == null || array.Length == 0)
		{
			return;
		}
		Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i].transform.gameObject;
			if (gameObject.gameObject == MonoSingleton<NewMovement>.Instance.gameObject && (float)ignorePlayerTimer > 0.66f)
			{
				TouchPlayer();
			}
			else if ((gameObject.layer == 10 || gameObject.layer == 11) && (gameObject.gameObject.CompareTag("Head") || gameObject.gameObject.CompareTag("Body") || gameObject.gameObject.CompareTag("Limb") || gameObject.gameObject.CompareTag("EndLimb") || gameObject.gameObject.CompareTag("Enemy")))
			{
				TouchEnemy(gameObject.transform);
			}
			else
			{
				if (gameObject.layer != 8 && gameObject.layer != 24 && gameObject.layer != 26 && !gameObject.CompareTag("Armor"))
				{
					continue;
				}
				if (gameObject.TryGetComponent<Breakable>(out var component) && component.weak)
				{
					component.Break();
					return;
				}
				base.transform.position = array[i].point;
				rb.velocity = Vector3.Reflect(rb.velocity.normalized, array[i].normal) * (rb.velocity.magnitude / 2f);
				flag = true;
				GameObject gameObject2 = UnityEngine.Object.Instantiate(ricochetEffect, array[i].point, Quaternion.LookRotation(array[i].normal));
				if (flag2 && gameObject2.TryGetComponent<AudioSource>(out var component2))
				{
					component2.enabled = false;
				}
				else
				{
					flag2 = true;
				}
				ignorePlayerTimer = 1f;
				break;
			}
		}
		if (flag)
		{
			CheckMultipleRicochets();
		}
	}

	private void TouchPlayer()
	{
		inPlayer = true;
		stopped = true;
		originalVelocity = rb.velocity;
		base.transform.position = MonoSingleton<NewMovement>.Instance.transform.position;
		model.gameObject.SetActive(value: false);
		sprite.localScale = Vector3.one * 20f;
	}

	private void TouchEnemy(Transform other)
	{
		if (hitAmount > 1)
		{
			if (!stopped && other.TryGetComponent<EnemyIdentifierIdentifier>(out var component) && (bool)component.eid)
			{
				if (component.eid.dead)
				{
					HitEnemy(other, component);
				}
				else if (!(sameEnemyHitCooldown > 0f) || !(currentHitEnemy != null) || !(currentHitEnemy == component.eid))
				{
					stopped = true;
					currentHitAmount = hitAmount;
					hitTarget = other;
					currentHitEnemy = component.eid;
					originalVelocity = rb.velocity;
					sameEnemyHitCooldown = 0.25f;
				}
			}
		}
		else
		{
			HitEnemy(other);
		}
	}

	private void HitEnemy(Transform other, EnemyIdentifierIdentifier eidid = null)
	{
		if (((bool)eidid || other.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) && (bool)eidid.eid && (!(sameEnemyHitCooldown > 0f) || !(currentHitEnemy != null) || !(currentHitEnemy == eidid.eid)) && !hitLimbs.Contains(other))
		{
			if (!eidid.eid.dead)
			{
				sameEnemyHitCooldown = 0.25f;
				currentHitEnemy = eidid.eid;
				currentHitAmount--;
			}
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = enemyHitSound;
			aud.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
			aud.volume = 0.2f;
			aud.Play();
			if ((bool)eidid && (bool)eidid.eid)
			{
				DamageEnemy(other, eidid.eid);
			}
		}
	}

	private void DamageEnemy(Transform other, EnemyIdentifier eid)
	{
		eid.hitter = (beenPunched ? "chainsawbounce" : "chainsaw");
		if (!eid.hitterWeapons.Contains(weaponType))
		{
			eid.hitterWeapons.Add(weaponType);
		}
		if (enemyHitParticle != null)
		{
			UnityEngine.Object.Instantiate(enemyHitParticle, other.transform.position, Quaternion.identity).transform.localScale *= 3f;
		}
		bool dead = eid.dead;
		eid.DeliverDamage(other.gameObject, (other.transform.position - base.transform.position).normalized * 3000f, base.transform.position, damage, tryForExplode: false, 0f, sourceWeapon);
		if (dead)
		{
			hitLimbs.Add(other);
		}
		if (heated)
		{
			Flammable componentInChildren = eid.GetComponentInChildren<Flammable>();
			if (componentInChildren != null)
			{
				componentInChildren.Burn(2f, componentInChildren.burning);
			}
		}
	}

	public void CheckMultipleRicochets(bool onStart = false)
	{
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		bool flag = false;
		for (int i = 0; i < 3; i++)
		{
			if (!Physics.Raycast(base.transform.position, rb.velocity.normalized, out var hitInfo, 5f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				break;
			}
			if (hitInfo.transform.TryGetComponent<Breakable>(out var component) && component.weak)
			{
				component.Break();
				continue;
			}
			base.transform.position = hitInfo.point;
			rb.velocity = Vector3.Reflect(rb.velocity.normalized, hitInfo.normal) * (rb.velocity.magnitude / 2f);
			GameObject gameObject = UnityEngine.Object.Instantiate(ricochetEffect, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
			if (flag && gameObject.TryGetComponent<AudioSource>(out var component2))
			{
				component2.enabled = false;
			}
			else
			{
				flag = true;
			}
		}
		if (onStart)
		{
			Collider[] array = Physics.OverlapSphere(base.transform.position, 1.5f, LayerMaskDefaults.Get(LMD.Enemies));
			if (array.Length != 0)
			{
				TouchEnemy(array[0].transform);
			}
		}
	}

	public void GetPunched()
	{
		beenPunched = true;
		beingPunched = false;
		inPlayer = false;
		stopped = false;
		playerHitTimer = 0f;
		ignorePlayerTimer = 0f;
		sameEnemyHitCooldown = 0f;
		sprite.localScale = Vector3.one * 100f;
		model.gameObject.SetActive(value: true);
		if (hitAmount < 3)
		{
			hitAmount++;
			if (hitAmount == 3)
			{
				heated = true;
			}
		}
	}

	public void TurnIntoSawblade()
	{
		Nail nail = UnityEngine.Object.Instantiate(sawbladeVersion, base.transform.position, base.transform.rotation);
		nail.sourceWeapon = sourceWeapon;
		nail.weaponType = weaponType;
		nail.heated = heated;
		nail.rb.velocity = ((rb.velocity == Vector3.zero) ? base.transform.forward : (stopped ? originalVelocity : rb.velocity)).normalized * 105f;
		UnityEngine.Object.Instantiate(ropeSnapSound, base.transform.position, Quaternion.identity).volume /= 2f;
		base.gameObject.SetActive(value: false);
		UnityEngine.Object.Destroy(base.gameObject);
	}
}

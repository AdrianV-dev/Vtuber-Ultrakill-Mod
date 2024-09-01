using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;

public class Grenade : MonoBehaviour
{
	public string hitterWeapon;

	public GameObject sourceWeapon;

	public GameObject explosion;

	public GameObject harmlessExplosion;

	public GameObject superExplosion;

	[SerializeField]
	private RevolverBeam grenadeBeam;

	private bool exploded;

	public bool enemy;

	[HideInInspector]
	public EnemyIdentifier originEnemy;

	public float totalDamageMultiplier = 1f;

	public bool rocket;

	[HideInInspector]
	public Rigidbody rb;

	[HideInInspector]
	public List<Magnet> magnets = new List<Magnet>();

	[HideInInspector]
	public Magnet latestEnemyMagnet;

	public float rocketSpeed;

	[SerializeField]
	private GameObject freezeEffect;

	private CapsuleCollider col;

	public bool playerRiding;

	private bool playerInRidingRange = true;

	private float downpull = -0.5f;

	public GameObject playerRideSound;

	[HideInInspector]
	public bool rideable;

	[HideInInspector]
	public bool hooked;

	private bool hasBeenRidden;

	private LayerMask rocketRideMask;

	public EnemyTarget proximityTarget;

	public GameObject proximityWindup;

	private bool selfExploding;

	[HideInInspector]
	public bool levelledUp;

	[HideInInspector]
	public float timeFrozen;

	[SerializeField]
	private GameObject levelUpEffect;

	public List<EnemyType> ignoreEnemyType = new List<EnemyType>();

	public bool frozen
	{
		get
		{
			if (!MonoSingleton<WeaponCharges>.Instance)
			{
				return false;
			}
			return MonoSingleton<WeaponCharges>.Instance.rocketFrozen;
		}
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		col = GetComponent<CapsuleCollider>();
		if (!enemy)
		{
			CanCollideWithPlayer(can: false);
		}
		MonoSingleton<ObjectTracker>.Instance.AddGrenade(this);
		rocketRideMask = LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies);
		rocketRideMask = (int)rocketRideMask | 0x40000;
	}

	private void Start()
	{
		MonoSingleton<WeaponCharges>.Instance.rocketCount++;
	}

	private void OnDestroy()
	{
		if (base.gameObject.scene.isLoaded)
		{
			MonoSingleton<ObjectTracker>.Instance.RemoveGrenade(this);
			if (playerRiding)
			{
				PlayerRideEnd();
			}
		}
		if (rocket && (bool)MonoSingleton<WeaponCharges>.Instance)
		{
			MonoSingleton<WeaponCharges>.Instance.rocketCount--;
			MonoSingleton<WeaponCharges>.Instance.timeSinceIdleFrozen = 0f;
		}
	}

	private void Update()
	{
		if (rocket && rocketSpeed != 0f && (bool)rb && !MonoSingleton<OptionsManager>.Instance.paused && playerRiding)
		{
			if (MonoSingleton<InputManager>.Instance.InputSource.Jump.WasPerformedThisFrame)
			{
				PlayerRideEnd();
				MonoSingleton<NewMovement>.Instance.Jump();
			}
			else if (MonoSingleton<InputManager>.Instance.InputSource.Slide.WasPerformedThisFrame)
			{
				PlayerRideEnd();
			}
		}
	}

	private void FixedUpdate()
	{
		if (!rocket || rocketSpeed == 0f || !rb)
		{
			return;
		}
		if (frozen)
		{
			if (magnets.Count > 0)
			{
				ignoreEnemyType.Clear();
			}
			rideable = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			timeFrozen += Time.fixedDeltaTime;
			if (timeFrozen >= 1f && (!enemy || hasBeenRidden) && !levelledUp)
			{
				levelledUp = true;
				if ((bool)levelUpEffect)
				{
					levelUpEffect.SetActive(value: true);
				}
			}
		}
		else if (playerRiding)
		{
			if (NoWeaponCooldown.NoCooldown || MonoSingleton<UnderwaterController>.Instance.inWater || MonoSingleton<WeaponCharges>.Instance.infiniteRocketRide)
			{
				if (MonoSingleton<UnderwaterController>.Instance.inWater && downpull > 0f)
				{
					downpull = 0f;
				}
				rb.velocity = base.transform.forward * rocketSpeed * 0.65f;
			}
			else
			{
				rb.velocity = Vector3.Lerp(base.transform.forward * (rocketSpeed * 0.65f), Vector3.down * 100f, Mathf.Max(0f, downpull));
				downpull += Time.fixedDeltaTime / 4.5f * Mathf.Max(1f, 1f + rb.velocity.normalized.y);
			}
		}
		else
		{
			rb.velocity = base.transform.forward * rocketSpeed;
		}
		if (playerRiding)
		{
			MonoSingleton<NewMovement>.Instance.rb.velocity = Vector3.zero;
			Vector3 vector = MonoSingleton<NewMovement>.Instance.transform.position + MonoSingleton<NewMovement>.Instance.playerCollider.center;
			bool flag = false;
			Vector3 vector2 = Vector3.positiveInfinity;
			Collider other = null;
			if (!Physics.CheckCapsule(vector + Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), vector - Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), 0.5f, rocketRideMask, QueryTriggerInteraction.Ignore))
			{
				RaycastHit[] array = Physics.CapsuleCastAll(vector + Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), vector - Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), 0.499f, rb.velocity.normalized, rb.velocity.magnitude * Time.fixedDeltaTime, rocketRideMask, QueryTriggerInteraction.Ignore);
				for (int i = 0; i < array.Length; i++)
				{
					if (!array[i].collider.isTrigger && array[i].collider.gameObject.layer != 12 && array[i].collider.gameObject.layer != 14 && (!array[i].collider.attachedRigidbody || array[i].collider.attachedRigidbody != rb))
					{
						Vector3 vector3 = MonoSingleton<NewMovement>.Instance.playerCollider.ClosestPoint(array[i].point);
						Vector3 vector4 = array[i].point - (array[i].point - vector3).normalized * Vector3.Distance(MonoSingleton<NewMovement>.Instance.transform.position, vector3);
						if (Vector3.Distance(MonoSingleton<NewMovement>.Instance.transform.position, vector4) < Vector3.Distance(MonoSingleton<NewMovement>.Instance.transform.position, vector2))
						{
							new GameObject().transform.position = base.transform.position;
							vector2 = vector4;
							other = array[i].collider;
						}
						flag = true;
					}
					else
					{
						_ = array[i].collider.isTrigger;
						_ = array[i].collider.gameObject.layer;
						_ = 12;
						_ = array[i].collider.gameObject.layer;
						_ = 14;
						if ((bool)array[i].collider.attachedRigidbody)
						{
							_ = array[i].collider.attachedRigidbody == rb;
						}
					}
				}
			}
			else
			{
				vector2 = MonoSingleton<NewMovement>.Instance.transform.position;
				other = Physics.OverlapCapsule(vector + Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), vector - Vector3.up * (MonoSingleton<NewMovement>.Instance.playerCollider.height / 2f), 0.5f, rocketRideMask, QueryTriggerInteraction.Ignore)[0];
				flag = true;
			}
			if (flag)
			{
				PlayerRideEnd();
				MonoSingleton<NewMovement>.Instance.transform.position = vector2;
				base.transform.position = MonoSingleton<NewMovement>.Instance.transform.position;
				Collision(other);
			}
		}
		else
		{
			float num = Vector3.Distance(MonoSingleton<NewMovement>.Instance.gc.transform.position, base.transform.position + base.transform.forward);
			if (num < 2.25f && (MonoSingleton<NewMovement>.Instance.rb.velocity.y < 0f || hooked) && !MonoSingleton<NewMovement>.Instance.gc.onGround && !MonoSingleton<NewMovement>.Instance.dead && rideable && (!enemy || MonoSingleton<NewMovement>.Instance.gc.heavyFall))
			{
				if (!MonoSingleton<NewMovement>.Instance.ridingRocket && !playerInRidingRange)
				{
					PlayerRideStart();
				}
				playerInRidingRange = true;
			}
			else if (playerInRidingRange && (num > 3f || MonoSingleton<NewMovement>.Instance.gc.onGround || (MonoSingleton<NewMovement>.Instance.rb.velocity.y > 0f && !hooked)))
			{
				playerInRidingRange = false;
			}
		}
		if (freezeEffect.activeSelf != frozen)
		{
			freezeEffect.SetActive(frozen);
		}
		if (magnets.Count > 0)
		{
			int num2 = magnets.Count - 1;
			while (num2 >= 0)
			{
				if (magnets[num2] == null)
				{
					magnets.RemoveAt(num2);
					num2--;
					continue;
				}
				if (frozen)
				{
					if ((bool)latestEnemyMagnet && latestEnemyMagnet.gameObject.activeInHierarchy && !Physics.Raycast(base.transform.position, latestEnemyMagnet.transform.position - base.transform.position, Vector3.Distance(latestEnemyMagnet.transform.position, base.transform.position), LayerMaskDefaults.Get(LMD.Environment)))
					{
						base.transform.LookAt(latestEnemyMagnet.transform.position);
					}
					else
					{
						base.transform.LookAt(magnets[num2].transform.position);
					}
				}
				else
				{
					base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(magnets[num2].transform.position - base.transform.position), Time.fixedDeltaTime * 180f);
				}
				break;
			}
		}
		else if ((bool)latestEnemyMagnet && latestEnemyMagnet.gameObject.activeInHierarchy && !Physics.Raycast(base.transform.position, latestEnemyMagnet.transform.position - base.transform.position, Vector3.Distance(latestEnemyMagnet.transform.position, base.transform.position), LayerMaskDefaults.Get(LMD.Environment)))
		{
			base.transform.LookAt(latestEnemyMagnet.transform.position);
		}
		if (proximityTarget != null && magnets.Count == 0 && !frozen && !playerRiding && !selfExploding && Vector3.Distance(proximityTarget.position, base.transform.position) < Vector3.Distance(proximityTarget.PredictTargetPosition(Time.fixedDeltaTime), base.transform.position + rb.velocity * Time.fixedDeltaTime))
		{
			selfExploding = true;
			rideable = true;
			Object.Instantiate(proximityWindup, col.bounds.center, Quaternion.identity);
			rb.isKinematic = true;
			Invoke("ProximityExplosion", 0.5f);
		}
	}

	private void LateUpdate()
	{
		if (!playerRiding)
		{
			return;
		}
		if (Vector3.Distance(base.transform.position, MonoSingleton<NewMovement>.Instance.transform.position) > 5f + rb.velocity.magnitude * Time.deltaTime)
		{
			PlayerRideEnd();
			return;
		}
		Vector2 vector = MonoSingleton<InputManager>.Instance.InputSource.Move.ReadValue<Vector2>();
		base.transform.Rotate(vector.y * Time.deltaTime * 165f, vector.x * Time.deltaTime * 165f, 0f, Space.Self);
		if (Physics.Raycast(base.transform.position + base.transform.forward, base.transform.up, 4f, LayerMaskDefaults.Get(LMD.Environment)))
		{
			if (Physics.Raycast(base.transform.position + base.transform.forward, Vector3.up, out var hitInfo, 2f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				MonoSingleton<NewMovement>.Instance.transform.position = base.transform.position + base.transform.forward - Vector3.up * hitInfo.distance;
			}
			else
			{
				MonoSingleton<NewMovement>.Instance.transform.position = base.transform.position + base.transform.forward;
			}
		}
		else
		{
			MonoSingleton<NewMovement>.Instance.transform.position = base.transform.position + base.transform.up * 2f + base.transform.forward;
		}
		MonoSingleton<CameraController>.Instance.CameraShake(0.1f);
	}

	private void OnCollisionEnter(Collision collision)
	{
		Collision(collision.collider);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (rocket && frozen && (other.gameObject.layer == 10 || other.gameObject.layer == 11) && !other.isTrigger)
		{
			Collision(other);
		}
	}

	public void Collision(Collider other)
	{
		if (exploded || (!enemy && other.CompareTag("Player")) || other.gameObject.layer == 14 || other.gameObject.layer == 20)
		{
			return;
		}
		bool flag = false;
		if ((other.gameObject.layer == 11 || other.gameObject.layer == 10) && (other.attachedRigidbody ? other.attachedRigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var component) : other.TryGetComponent<EnemyIdentifierIdentifier>(out component)) && (bool)component.eid)
		{
			if (component.eid.enemyType == EnemyType.MaliciousFace && !component.eid.isGasolined)
			{
				flag = true;
			}
			else
			{
				if (ignoreEnemyType.Count > 0 && ignoreEnemyType.Contains(component.eid.enemyType))
				{
					return;
				}
				if (component.eid.dead)
				{
					Physics.IgnoreCollision(col, other, ignore: true);
					return;
				}
			}
		}
		if (!flag && other.gameObject.CompareTag("Armor"))
		{
			flag = true;
		}
		if (flag)
		{
			rb.constraints = RigidbodyConstraints.None;
			if (Physics.Raycast(base.transform.position - base.transform.forward, base.transform.forward, out var hitInfo, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
			{
				Vector3 velocity = rb.velocity;
				rb.velocity = Vector3.zero;
				rb.AddForce(Vector3.Reflect(velocity.normalized, hitInfo.normal).normalized * velocity.magnitude * 2f, ForceMode.VelocityChange);
				base.transform.forward = Vector3.Reflect(velocity.normalized, hitInfo.normal).normalized;
				rb.AddTorque(Random.insideUnitCircle.normalized * Random.Range(0, 250));
			}
			Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.ineffectiveSound, base.transform.position, Quaternion.identity).GetComponent<AudioSource>().volume = 0.75f;
			return;
		}
		bool harmless = false;
		bool big = false;
		bool flag2 = false;
		if (rocket)
		{
			if (other.gameObject.layer == 10 || other.gameObject.layer == 11)
			{
				EnemyIdentifierIdentifier component2 = other.GetComponent<EnemyIdentifierIdentifier>();
				if ((bool)component2 && (bool)component2.eid)
				{
					if (levelledUp)
					{
						flag2 = true;
					}
					else if (!component2.eid.dead && !component2.eid.flying && (((bool)component2.eid.gce && !component2.eid.gce.onGround) || (float)component2.eid.timeSinceSpawned <= 0.15f))
					{
						flag2 = true;
					}
					if (component2.eid.stuckMagnets.Count > 0)
					{
						foreach (Magnet stuckMagnet in component2.eid.stuckMagnets)
						{
							if (!(stuckMagnet == null))
							{
								stuckMagnet.DamageMagnet((!flag2) ? 1 : 2);
							}
						}
					}
					if (component2.eid == originEnemy && !component2.eid.blessed)
					{
						if (hasBeenRidden && !frozen && originEnemy.enemyType == EnemyType.Guttertank)
						{
							originEnemy.Explode(fromExplosion: true);
							MonoSingleton<StyleHUD>.Instance.AddPoints(300, "ultrakill.roundtrip", null, component2.eid);
						}
						else
						{
							MonoSingleton<StyleHUD>.Instance.AddPoints(100, "ultrakill.rocketreturn", null, component2.eid);
						}
					}
				}
				MonoSingleton<TimeController>.Instance.HitStop(0.05f);
			}
			else if (!enemy || !other.gameObject.CompareTag("Player"))
			{
				harmless = true;
			}
		}
		else if (other.gameObject.layer != 8)
		{
			MonoSingleton<TimeController>.Instance.HitStop(0.05f);
		}
		Explode(big, harmless, flag2);
	}

	private void ProximityExplosion()
	{
		Explode(big: true);
	}

	public void Explode(bool big = false, bool harmless = false, bool super = false, float sizeMultiplier = 1f, bool ultrabooster = false, GameObject exploderWeapon = null, bool fup = false)
	{
		if (exploded)
		{
			return;
		}
		exploded = true;
		if (MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(base.transform.position))
		{
			harmless = false;
		}
		GameObject gameObject = (harmless ? Object.Instantiate(harmlessExplosion, base.transform.position, Quaternion.identity) : ((!super) ? Object.Instantiate(this.explosion, base.transform.position, Quaternion.identity) : Object.Instantiate(superExplosion, base.transform.position, Quaternion.identity)));
		Explosion[] componentsInChildren = gameObject.GetComponentsInChildren<Explosion>();
		foreach (Explosion explosion in componentsInChildren)
		{
			explosion.sourceWeapon = exploderWeapon ?? sourceWeapon;
			explosion.hitterWeapon = hitterWeapon;
			explosion.isFup = fup;
			if (enemy)
			{
				explosion.enemy = true;
			}
			if (ignoreEnemyType.Count > 0)
			{
				explosion.toIgnore = ignoreEnemyType;
			}
			if (rocket && super && big)
			{
				explosion.maxSize *= 2.5f;
				explosion.speed *= 2.5f;
			}
			else if (big || (rocket && frozen))
			{
				explosion.maxSize *= 1.5f;
				explosion.speed *= 1.5f;
			}
			if (totalDamageMultiplier != 1f)
			{
				explosion.damage = (int)((float)explosion.damage * totalDamageMultiplier);
			}
			explosion.maxSize *= sizeMultiplier;
			explosion.speed *= sizeMultiplier;
			if ((bool)originEnemy)
			{
				explosion.originEnemy = originEnemy;
			}
			if (ultrabooster)
			{
				explosion.ultrabooster = true;
			}
			if (rocket && explosion.damage != 0)
			{
				explosion.rocketExplosion = true;
			}
		}
		gameObject.transform.localScale *= sizeMultiplier;
		Object.Destroy(base.gameObject);
	}

	public void PlayerRideStart()
	{
		CanCollideWithPlayer(can: false);
		if (enemy && proximityTarget != null)
		{
			CancelInvoke("ProximityExplosion");
			proximityTarget = null;
			rb.isKinematic = false;
		}
		ignoreEnemyType.Clear();
		playerRiding = true;
		MonoSingleton<NewMovement>.Instance.ridingRocket = this;
		MonoSingleton<NewMovement>.Instance.gc.heavyFall = false;
		MonoSingleton<NewMovement>.Instance.gc.ForceOff();
		MonoSingleton<NewMovement>.Instance.slopeCheck.ForceOff();
		Object.Instantiate(playerRideSound);
		if (!hasBeenRidden && !enemy)
		{
			MonoSingleton<NewMovement>.Instance.rocketRides++;
			hasBeenRidden = true;
			if (MonoSingleton<NewMovement>.Instance.rocketRides > 3)
			{
				downpull += 0.25f * (float)(MonoSingleton<NewMovement>.Instance.rocketRides - 3);
			}
		}
		else if (!hasBeenRidden)
		{
			hasBeenRidden = true;
		}
	}

	public void PlayerRideEnd()
	{
		playerRiding = false;
		MonoSingleton<NewMovement>.Instance.ridingRocket = null;
		MonoSingleton<NewMovement>.Instance.gc.StopForceOff();
		MonoSingleton<NewMovement>.Instance.slopeCheck.StopForceOff();
	}

	public void CanCollideWithPlayer(bool can = true)
	{
		Physics.IgnoreCollision(col, MonoSingleton<NewMovement>.Instance.playerCollider, !can);
	}

	public void GrenadeBeam(Vector3 targetPoint, GameObject newSourceWeapon = null)
	{
		if (!exploded)
		{
			RevolverBeam revolverBeam = Object.Instantiate(grenadeBeam, base.transform.position, Quaternion.LookRotation(targetPoint - base.transform.position));
			revolverBeam.sourceWeapon = ((newSourceWeapon != null) ? newSourceWeapon : sourceWeapon);
			if (levelledUp)
			{
				revolverBeam.hitParticle = superExplosion;
			}
			exploded = true;
			MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(targetPoint);
			Object.Destroy(base.gameObject);
		}
	}
}

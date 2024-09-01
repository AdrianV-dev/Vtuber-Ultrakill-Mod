using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	public GameObject sourceWeapon;

	private Rigidbody rb;

	public float speed;

	public float turnSpeed;

	public float speedRandomizer;

	private AudioSource aud;

	public GameObject explosionEffect;

	public float damage;

	public float enemyDamageMultiplier = 1f;

	public bool friendly;

	public bool playerBullet;

	public string bulletType;

	public string weaponType;

	public bool decorative;

	private Vector3 origScale;

	private bool active = true;

	public EnemyType safeEnemyType;

	public bool explosive;

	public bool bigExplosion;

	public List<EnemyIdentifier> alreadyHitEnemies = new List<EnemyIdentifier>();

	public HomingType homingType;

	public float turningSpeedMultiplier = 1f;

	public EnemyTarget target;

	private float maxSpeed;

	private Quaternion targetRotation;

	public float predictiveHomingMultiplier;

	public bool hittingPlayer;

	private NewMovement nmov;

	public bool boosted;

	[HideInInspector]
	public bool parried;

	private Collider col;

	private float radius;

	public bool undeflectable;

	public bool unparryable;

	public bool keepTrail;

	public bool strong;

	public bool spreaded;

	private int difficulty;

	public bool precheckForCollisions;

	public bool canHitCoin;

	public bool ignoreExplosions;

	private List<Collider> alreadyDeflectedBy = new List<Collider>();

	private void Start()
	{
		if ((bool)aud)
		{
			aud.pitch = Random.Range(1.8f, 2f);
			if (aud.enabled)
			{
				aud.Play();
			}
		}
		if (decorative)
		{
			origScale = base.transform.localScale;
			base.transform.localScale = Vector3.zero;
		}
		if (speed != 0f)
		{
			speed += Random.Range(0f - speedRandomizer, speedRandomizer);
		}
		if (col != null && !decorative)
		{
			col.enabled = false;
			col.enabled = true;
		}
		maxSpeed = speed;
		if (target == null)
		{
			target = EnemyTarget.TrackPlayerIfAllowed();
		}
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		aud = GetComponent<AudioSource>();
		if (col == null)
		{
			col = GetComponentInChildren<Collider>();
		}
		difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
	}

	public static float GetProjectileSpeedMulti(int difficulty)
	{
		if (difficulty > 2)
		{
			return 1.35f;
		}
		return difficulty switch
		{
			1 => 0.75f, 
			0 => 0.5f, 
			_ => 1f, 
		};
	}

	private void Update()
	{
		if (homingType == HomingType.None || target == null || hittingPlayer)
		{
			return;
		}
		float num = predictiveHomingMultiplier;
		Vector3 vector = (target.isPlayer ? target.headPosition : target.position);
		if (Vector3.Distance(base.transform.position, vector) < 15f)
		{
			num = 0f;
		}
		switch (homingType)
		{
		case HomingType.Gradual:
		{
			if (difficulty == 1)
			{
				maxSpeed += Time.deltaTime * 17.5f;
			}
			else if (difficulty == 0)
			{
				maxSpeed += Time.deltaTime * 10f;
			}
			else
			{
				maxSpeed += Time.deltaTime * 25f;
			}
			Quaternion to = Quaternion.LookRotation(vector + target.GetVelocity() * num - base.transform.position);
			if (difficulty == 0)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 100f * turningSpeedMultiplier);
			}
			else if (difficulty == 1)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 135f * turningSpeedMultiplier);
			}
			else if (difficulty == 2)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 185f * turningSpeedMultiplier);
			}
			else
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 200f * turningSpeedMultiplier);
			}
			rb.velocity = base.transform.forward * maxSpeed;
			break;
		}
		case HomingType.Instant:
		{
			Quaternion to = Quaternion.LookRotation(vector + target.GetVelocity() * num - base.transform.position);
			if (difficulty == 0)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 100f * turningSpeedMultiplier);
			}
			else if (difficulty == 1)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 135f * turningSpeedMultiplier);
			}
			else if (difficulty == 2)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 185f * turningSpeedMultiplier);
			}
			else
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * 200f * turningSpeedMultiplier);
			}
			rb.velocity = base.transform.forward * speed;
			break;
		}
		case HomingType.Loose:
		{
			maxSpeed += Time.deltaTime * 10f;
			base.transform.LookAt(base.transform.position + rb.velocity);
			Vector3 normalized = (vector + target.GetVelocity() * num - base.transform.position).normalized;
			rb.AddForce(normalized * speed * Time.deltaTime * 200f, ForceMode.Acceleration);
			rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
			break;
		}
		case HomingType.HorizontalOnly:
		{
			base.transform.LookAt(target.position + rb.velocity);
			Vector3 vector2 = vector + target.GetVelocity() * num;
			vector2.y = base.transform.position.y;
			float num2 = Mathf.Clamp(vector2.x - base.transform.position.x, 0f - turnSpeed, turnSpeed);
			float num3 = Mathf.Clamp(vector2.z - base.transform.position.z, 0f - turnSpeed, turnSpeed);
			if (Vector3.Distance(base.transform.position, vector2) < turnSpeed / 20f)
			{
				num2 = (vector2 - base.transform.position).x;
				num3 = (vector2 - base.transform.position).z;
			}
			float num4 = 15f;
			if (difficulty == 1)
			{
				num4 = 10f;
			}
			else if (difficulty == 0)
			{
				num4 = 5f;
			}
			else if (difficulty >= 3)
			{
				num4 = 25f;
			}
			float x = Mathf.MoveTowards(rb.velocity.x, num2, Time.deltaTime * num4 * turningSpeedMultiplier);
			float z = Mathf.MoveTowards(rb.velocity.z, num3, Time.deltaTime * num4 * turningSpeedMultiplier);
			rb.velocity = new Vector3(x, rb.velocity.y, z);
			break;
		}
		default:
			maxSpeed += Time.deltaTime * 10f;
			targetRotation = Quaternion.LookRotation(vector + target.GetVelocity() * num - base.transform.position);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
			rb.velocity = base.transform.forward * maxSpeed;
			break;
		}
	}

	private void FixedUpdate()
	{
		if (!hittingPlayer && !undeflectable && !decorative && speed != 0f && homingType == HomingType.None)
		{
			rb.velocity = base.transform.forward * speed;
		}
		if (decorative && base.transform.localScale.x < origScale.x)
		{
			aud.pitch = base.transform.localScale.x / origScale.x * 2.8f;
			base.transform.localScale = Vector3.Slerp(base.transform.localScale, origScale, Time.deltaTime * speed);
		}
		if (precheckForCollisions)
		{
			LayerMask layerMask = LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment);
			layerMask = (int)layerMask | 4;
			if (Physics.SphereCast(base.transform.position, radius, rb.velocity.normalized, out var hitInfo, rb.velocity.magnitude * Time.fixedDeltaTime, layerMask))
			{
				base.transform.position = base.transform.position + rb.velocity.normalized * hitInfo.distance;
				Collided(hitInfo.collider);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		Collided(other);
	}

	private void Collided(Collider other)
	{
		if (!active)
		{
			return;
		}
		EnemyIdentifierIdentifier component2;
		EnemyIdentifierIdentifier component3;
		if (!friendly && !hittingPlayer && other.gameObject.CompareTag("Player"))
		{
			if (target.isPlayer && MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
			{
				MonoSingleton<PlatformerMovement>.Instance.Explode();
				if (explosive)
				{
					Explode();
					return;
				}
				if (keepTrail)
				{
					KeepTrail();
				}
				CreateExplosionEffect();
				Object.Destroy(base.gameObject);
				return;
			}
			if (spreaded)
			{
				ProjectileSpread componentInParent = GetComponentInParent<ProjectileSpread>();
				if (componentInParent != null && componentInParent.parried)
				{
					return;
				}
			}
			hittingPlayer = true;
			rb.velocity = Vector3.zero;
			if (keepTrail)
			{
				KeepTrail();
			}
			base.transform.position = new Vector3(other.transform.position.x, base.transform.position.y, other.transform.position.z);
			nmov = other.gameObject.GetComponentInParent<NewMovement>();
			Invoke("RecheckPlayerHit", 0.05f);
		}
		else if (canHitCoin && other.gameObject.CompareTag("Coin"))
		{
			Coin component = other.gameObject.GetComponent<Coin>();
			if ((bool)component && !component.shot)
			{
				if (!friendly)
				{
					if (target != null)
					{
						component.customTarget = target;
					}
					component.DelayedEnemyReflect();
				}
				else
				{
					component.DelayedReflectRevolver(component.transform.position);
				}
			}
			if (explosive)
			{
				Explode();
				return;
			}
			if (keepTrail)
			{
				KeepTrail();
			}
			active = false;
			CreateExplosionEffect();
			Object.Destroy(base.gameObject);
		}
		else if ((other.gameObject.CompareTag("Armor") && (friendly || !other.TryGetComponent<EnemyIdentifierIdentifier>(out component2) || !component2.eid || component2.eid.enemyType != safeEnemyType)) || (boosted && other.gameObject.layer == 11 && other.gameObject.CompareTag("Body") && other.TryGetComponent<EnemyIdentifierIdentifier>(out component3) && (bool)component3.eid && component3.eid.enemyType == EnemyType.MaliciousFace && !component3.eid.isGasolined))
		{
			if (!alreadyDeflectedBy.Contains(other) && Physics.Raycast(base.transform.position - base.transform.forward, base.transform.forward, out var hitInfo, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
			{
				base.transform.forward = Vector3.Reflect(base.transform.forward, hitInfo.normal).normalized;
				base.transform.position = hitInfo.point + base.transform.forward;
				Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.ineffectiveSound, base.transform.position, Quaternion.identity);
				alreadyDeflectedBy.Add(other);
			}
		}
		else if (active && (other.gameObject.CompareTag("Head") || other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Limb") || other.gameObject.CompareTag("EndLimb")) && !other.gameObject.CompareTag("Armor"))
		{
			EnemyIdentifierIdentifier componentInParent2 = other.gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
			EnemyIdentifier enemyIdentifier = null;
			if (componentInParent2 != null && componentInParent2.eid != null)
			{
				enemyIdentifier = componentInParent2.eid;
			}
			if (!(enemyIdentifier != null) || (alreadyHitEnemies.Count != 0 && alreadyHitEnemies.Contains(enemyIdentifier)) || ((enemyIdentifier.enemyType == safeEnemyType || EnemyIdentifier.CheckHurtException(safeEnemyType, enemyIdentifier.enemyType, target)) && (!friendly || enemyIdentifier.immuneToFriendlyFire) && !playerBullet && !parried))
			{
				return;
			}
			if (explosive)
			{
				Explode();
			}
			active = false;
			bool tryForExplode = false;
			bool dead = enemyIdentifier.dead;
			if (playerBullet)
			{
				enemyIdentifier.hitter = bulletType;
				if (!enemyIdentifier.hitterWeapons.Contains(weaponType))
				{
					enemyIdentifier.hitterWeapons.Add(weaponType);
				}
			}
			else if (!friendly)
			{
				enemyIdentifier.hitter = "enemy";
			}
			else
			{
				enemyIdentifier.hitter = "projectile";
				tryForExplode = true;
			}
			if (boosted && !enemyIdentifier.blessed && !enemyIdentifier.dead)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(90, "ultrakill.projectileboost", sourceWeapon, enemyIdentifier);
			}
			bool flag = true;
			if (spreaded)
			{
				ProjectileSpread componentInParent3 = GetComponentInParent<ProjectileSpread>();
				if (componentInParent3 != null)
				{
					if (componentInParent3.hitEnemies.Contains(enemyIdentifier))
					{
						flag = false;
					}
					else
					{
						componentInParent3.hitEnemies.Add(enemyIdentifier);
					}
				}
			}
			if (!explosive)
			{
				if (flag)
				{
					if (playerBullet)
					{
						enemyIdentifier.DeliverDamage(other.gameObject, rb.velocity.normalized * 2500f, base.transform.position, damage / 4f * enemyDamageMultiplier, tryForExplode, 0f, sourceWeapon);
					}
					else if (friendly)
					{
						enemyIdentifier.DeliverDamage(other.gameObject, rb.velocity.normalized * 10000f, base.transform.position, damage / 4f * enemyDamageMultiplier, tryForExplode, 0f, sourceWeapon);
					}
					else
					{
						enemyIdentifier.DeliverDamage(other.gameObject, rb.velocity.normalized * 100f, base.transform.position, damage / 10f * enemyDamageMultiplier, tryForExplode, 0f, sourceWeapon);
					}
				}
				CreateExplosionEffect();
			}
			if (keepTrail)
			{
				KeepTrail();
			}
			if (!dead)
			{
				MonoSingleton<TimeController>.Instance.HitStop(0.005f);
			}
			if (!dead || other.gameObject.layer == 11 || boosted)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			alreadyHitEnemies.Add(enemyIdentifier);
			active = true;
		}
		else if (!hittingPlayer && (other.gameObject.layer == 8 || other.gameObject.layer == 24) && active)
		{
			Breakable component4 = other.gameObject.GetComponent<Breakable>();
			if (component4 != null && !component4.precisionOnly && (component4.weak || strong))
			{
				component4.Break();
			}
			if (other.gameObject.TryGetComponent<Bleeder>(out var component5))
			{
				bool flag2 = false;
				if (!friendly && !playerBullet && component5.ignoreTypes.Length != 0)
				{
					EnemyType[] ignoreTypes = component5.ignoreTypes;
					for (int i = 0; i < ignoreTypes.Length; i++)
					{
						if (ignoreTypes[i] == safeEnemyType)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					if (damage <= 10f)
					{
						component5.GetHit(base.transform.position, GoreType.Body);
					}
					else if (damage <= 30f)
					{
						component5.GetHit(base.transform.position, GoreType.Limb);
					}
					else
					{
						component5.GetHit(base.transform.position, GoreType.Head);
					}
				}
			}
			if (explosive)
			{
				Explode();
			}
			else
			{
				if (keepTrail)
				{
					KeepTrail();
				}
				CreateExplosionEffect();
				Object.Destroy(base.gameObject);
			}
			active = false;
		}
		else if (other.gameObject.layer == 0)
		{
			Rigidbody componentInParent4 = other.GetComponentInParent<Rigidbody>();
			if (componentInParent4 != null)
			{
				componentInParent4.AddForce(base.transform.forward * 1000f);
			}
		}
	}

	private void CreateExplosionEffect()
	{
		if (explosionEffect == null)
		{
			return;
		}
		Explosion[] componentsInChildren = Object.Instantiate(explosionEffect, base.transform.position, base.transform.rotation).GetComponentsInChildren<Explosion>();
		foreach (Explosion explosion in componentsInChildren)
		{
			explosion.sourceWeapon = sourceWeapon ?? explosion.sourceWeapon;
			if (explosion.damage != 0 && ((!friendly && !playerBullet) || (float)explosion.damage < damage))
			{
				explosion.damage = (int)damage;
			}
		}
		if (boosted || parried)
		{
			MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(base.transform.position);
		}
	}

	public void Explode()
	{
		if (!active)
		{
			return;
		}
		active = false;
		if (keepTrail)
		{
			KeepTrail();
		}
		Explosion[] componentsInChildren = Object.Instantiate(explosionEffect, base.transform.position - rb.velocity * 0.02f, base.transform.rotation).GetComponentsInChildren<Explosion>();
		foreach (Explosion explosion in componentsInChildren)
		{
			explosion.sourceWeapon = sourceWeapon ?? explosion.sourceWeapon;
			if (bigExplosion)
			{
				explosion.maxSize *= 1.5f;
			}
			if (explosion.damage != 0)
			{
				explosion.damage = Mathf.RoundToInt(damage);
			}
			explosion.enemy = true;
		}
		MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(base.transform.position);
		Object.Destroy(base.gameObject);
	}

	private void RecheckPlayerHit()
	{
		if (hittingPlayer)
		{
			hittingPlayer = false;
			col.enabled = false;
			undeflectable = true;
			Invoke("TimeToDie", 0.01f);
		}
	}

	private void TimeToDie()
	{
		bool flag = false;
		if (spreaded)
		{
			ProjectileSpread componentInParent = GetComponentInParent<ProjectileSpread>();
			if (componentInParent != null && componentInParent.parried)
			{
				flag = true;
			}
		}
		CreateExplosionEffect();
		if (!flag)
		{
			if (explosive)
			{
				base.transform.position = base.transform.position - base.transform.forward;
				Explode();
			}
			else
			{
				nmov.GetHurt(Mathf.RoundToInt(damage), invincible: true);
			}
		}
		Object.Destroy(base.gameObject);
	}

	private void KeepTrail()
	{
		TrailRenderer componentInChildren = GetComponentInChildren<TrailRenderer>();
		if (componentInChildren != null)
		{
			componentInChildren.transform.parent = null;
			componentInChildren.gameObject.AddComponent<RemoveOnTime>().time = 3f;
		}
	}
}

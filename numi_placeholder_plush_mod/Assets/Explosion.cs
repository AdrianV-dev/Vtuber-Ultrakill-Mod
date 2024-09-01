using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	public static float globalSizeMulti = 1f;

	public HurtCooldownCollection HurtCooldownCollection;

	public GameObject sourceWeapon;

	public bool enemy;

	public bool harmless;

	public bool lowQuality;

	private CameraController cc;

	private Light light;

	private MeshRenderer mr;

	private Color materialColor;

	private Material originalMaterial;

	private TimeSince explosionTime;

	private bool whiteExplosion;

	private bool fading;

	public float speed;

	public float maxSize;

	private LayerMask lmask;

	public int damage;

	public float enemyDamageMultiplier;

	[HideInInspector]
	public int playerDamageOverride = -1;

	public GameObject explosionChunk;

	public bool ignite;

	public bool friendlyFire;

	public bool isFup;

	private HashSet<int> hitColliders = new HashSet<int>();

	public string hitterWeapon;

	public bool halved;

	private SphereCollider scol;

	public AffectedSubjects canHit;

	private bool hasHitPlayer;

	[HideInInspector]
	public EnemyIdentifier originEnemy;

	public bool rocketExplosion;

	public List<EnemyType> toIgnore;

	[HideInInspector]
	public EnemyIdentifier interruptedEnemy;

	[HideInInspector]
	public bool ultrabooster;

	public bool unblockable;

	public bool electric;

	private void Start()
	{
		explosionTime = 0f;
		mr = GetComponent<MeshRenderer>();
		materialColor = mr.material.GetColor("_Color");
		originalMaterial = mr.sharedMaterial;
		mr.material = new Material(MonoSingleton<DefaultReferenceManager>.Instance.blankMaterial);
		whiteExplosion = true;
		cc = MonoSingleton<CameraController>.Instance;
		float num = Vector3.Distance(base.transform.position, cc.transform.position);
		float num2 = ((damage == 0) ? 0.25f : 1f);
		if (num < 3f * maxSize)
		{
			cc.CameraShake(1.5f * num2);
		}
		else if (num < 85f)
		{
			cc.CameraShake((1.5f - (num - 20f) / 65f * 1.5f) / 6f * maxSize * num2);
		}
		scol = GetComponent<SphereCollider>();
		if ((bool)scol)
		{
			scol.enabled = true;
		}
		if (speed == 0f)
		{
			speed = 1f;
		}
		if (!lowQuality)
		{
			lowQuality = MonoSingleton<PrefsManager>.Instance.GetBoolLocal("simpleExplosions");
		}
		if ((bool)MonoSingleton<ComponentsDatabase>.Instance && MonoSingleton<ComponentsDatabase>.Instance.scrollers.Count > 0)
		{
			Collider[] array = Physics.OverlapSphere(base.transform.position, 1f, LayerMaskDefaults.Get(LMD.Environment));
			foreach (Collider collider in array)
			{
				if (MonoSingleton<ComponentsDatabase>.Instance.scrollers.Contains(collider.transform) && collider.transform.TryGetComponent<ScrollingTexture>(out var component))
				{
					component.attachedObjects.Add(base.transform);
				}
			}
		}
		if (!lowQuality)
		{
			light = GetComponentInChildren<Light>();
			light.enabled = true;
			if (explosionChunk != null)
			{
				for (int j = 0; j < Random.Range(24, 30); j++)
				{
					GameObject obj = Object.Instantiate(explosionChunk, base.transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)), Random.rotation);
					Vector3 vector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 2f), Random.Range(-1f, 1f));
					obj.GetComponent<Rigidbody>().AddForce(vector * 250f, ForceMode.VelocityChange);
					Physics.IgnoreCollision(obj.GetComponent<Collider>(), scol);
				}
			}
		}
		lmask = (int)lmask | 0x100;
		lmask = (int)lmask | 0x1000000;
		lmask = (int)lmask | 0x4000000;
		speed *= globalSizeMulti;
		maxSize *= globalSizeMulti;
	}

	private void Update()
	{
		if (light != null)
		{
			light.range += 5f * Time.deltaTime * speed;
		}
		if (whiteExplosion && (float)explosionTime > 0.1f)
		{
			whiteExplosion = false;
			mr.material = new Material(originalMaterial);
		}
		if (fading)
		{
			materialColor.a -= 2f * Time.deltaTime;
			if (light != null)
			{
				light.intensity -= 65f * Time.deltaTime;
			}
			mr.material.SetColor("_Color", materialColor);
			if (materialColor.a <= 0f)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void FixedUpdate()
	{
		base.transform.localScale += Vector3.one * 0.05f * speed;
		float num = base.transform.lossyScale.x * scol.radius;
		if (!fading && num > maxSize)
		{
			harmless = true;
			scol.enabled = false;
			fading = true;
			speed /= 4f;
		}
		if (!halved && num > maxSize / 2f)
		{
			halved = true;
			damage = Mathf.RoundToInt((float)damage / 1.5f);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer != 9 && !harmless)
		{
			Collide(other);
		}
	}

	private void Collide(Collider other)
	{
		Vector3 position = other.transform.position;
		Vector3 normalized = (position - base.transform.position).normalized;
		float num = Vector3.Distance(position, base.transform.position);
		Vector3 vector = base.transform.position - normalized * 0.01f;
		float maxDistance = Vector3.Distance(vector, position);
		int instanceID = other.GetInstanceID();
		if (!hitColliders.Contains(instanceID))
		{
			Breakable component4;
			Bleeder component5;
			Glass component6;
			Flammable component7;
			if (!hasHitPlayer && other.gameObject.CompareTag("Player"))
			{
				if (Physics.Raycast(vector, normalized, out var _, maxDistance, 2048, QueryTriggerInteraction.Ignore) || (enemy && Physics.Raycast(position, -normalized, num - 0.1f, lmask, QueryTriggerInteraction.Ignore)))
				{
					return;
				}
				hasHitPlayer = true;
				hitColliders.Add(instanceID);
				if (canHit != AffectedSubjects.EnemiesOnly)
				{
					if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer && damage > 0)
					{
						MonoSingleton<PlatformerMovement>.Instance.Burn();
						return;
					}
					if (!MonoSingleton<NewMovement>.Instance.exploded && (MonoSingleton<NewMovement>.Instance.safeExplosionLaunchCooldown <= 0f || damage > 0))
					{
						int num2 = 200;
						if (rocketExplosion && damage == 0)
						{
							num2 = Mathf.RoundToInt(100f / ((float)(MonoSingleton<NewMovement>.Instance.rocketJumps + 3) / 3f));
							MonoSingleton<NewMovement>.Instance.rocketJumps++;
						}
						Vector3 vector2 = base.transform.position - position;
						if (new Vector3(vector2.x, 0f, vector2.z).sqrMagnitude < 0.0625f)
						{
							if (isFup)
							{
								MonoSingleton<NewMovement>.Instance.LaunchFromPointAtSpeed(position, 60f);
							}
							else
							{
								MonoSingleton<NewMovement>.Instance.LaunchFromPoint(position, num2, maxSize);
								if (ultrabooster && num < 12f)
								{
									MonoSingleton<NewMovement>.Instance.LaunchFromPoint(position, num2, maxSize);
								}
							}
						}
						else if (isFup)
						{
							MonoSingleton<NewMovement>.Instance.LaunchFromPointAtSpeed(base.transform.position, 60f);
						}
						else
						{
							MonoSingleton<NewMovement>.Instance.LaunchFromPoint(base.transform.position, num2, maxSize);
							if (ultrabooster && num < 12f)
							{
								MonoSingleton<NewMovement>.Instance.LaunchFromPoint(base.transform.position, num2, maxSize);
							}
						}
						if (damage <= 0)
						{
							MonoSingleton<NewMovement>.Instance.safeExplosionLaunchCooldown = 0.5f;
						}
					}
					if (damage > 0)
					{
						int num3 = damage;
						if (ultrabooster)
						{
							num3 = ((num < 3f) ? 35 : 50);
						}
						num3 = ((playerDamageOverride >= 0) ? playerDamageOverride : num3);
						MonoSingleton<NewMovement>.Instance.GetHurt(num3, invincible: true, enemy ? 1 : 0, explosion: true);
					}
				}
			}
			else if ((other.gameObject.layer == 10 || other.gameObject.layer == 11) && canHit != AffectedSubjects.PlayerOnly)
			{
				EnemyIdentifierIdentifier componentInParent = other.GetComponentInParent<EnemyIdentifierIdentifier>();
				if (componentInParent != null && componentInParent.eid != null)
				{
					if (!componentInParent.eid.dead && componentInParent.eid.TryGetComponent<Collider>(out var component))
					{
						int instanceID2 = component.GetInstanceID();
						if (hitColliders.Add(instanceID2) && (HurtCooldownCollection == null || HurtCooldownCollection.TryHurtCheckEnemy(componentInParent.eid)))
						{
							if (componentInParent.eid.enemyType == EnemyType.Idol)
							{
								if (!Physics.Linecast(base.transform.position, component.bounds.center, LayerMaskDefaults.Get(LMD.Environment)))
								{
									componentInParent.eid.hitter = hitterWeapon;
									componentInParent.eid.DeliverDamage(other.gameObject, Vector3.zero, position, 1f, tryForExplode: false, 0f, sourceWeapon, ignoreTotalDamageTakenMultiplier: false, fromExplosion: true);
								}
							}
							else if (componentInParent.eid.enemyType == EnemyType.MaliciousFace && !componentInParent.eid.isGasolined)
							{
								Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.ineffectiveSound, position, Quaternion.identity);
							}
							else if ((!enemy || (componentInParent.eid.enemyType != EnemyType.HideousMass && componentInParent.eid.enemyType != EnemyType.Sisyphus)) && !toIgnore.Contains(componentInParent.eid.enemyType))
							{
								if (componentInParent.eid.enemyType == EnemyType.Gutterman && hitterWeapon == "heavypunch")
								{
									componentInParent.eid.hitter = "heavypunch";
								}
								else if (hitterWeapon == "lightningbolt")
								{
									componentInParent.eid.hitter = "lightningbolt";
								}
								else
								{
									componentInParent.eid.hitter = (friendlyFire ? "ffexplosion" : (enemy ? "enemy" : "explosion"));
								}
								if (!componentInParent.eid.hitterWeapons.Contains(hitterWeapon))
								{
									componentInParent.eid.hitterWeapons.Add(hitterWeapon);
								}
								Vector3 vector3 = normalized;
								if (componentInParent.eid.enemyType == EnemyType.Drone && damage == 0)
								{
									vector3 = Vector3.zero;
								}
								else if (vector3.y <= 0.5f)
								{
									vector3 = new Vector3(vector3.x, vector3.y + 0.5f, vector3.z);
								}
								else if (vector3.y < 1f)
								{
									vector3 = new Vector3(vector3.x, 1f, vector3.z);
								}
								float num4 = (float)damage / 10f * enemyDamageMultiplier;
								if (rocketExplosion && componentInParent.eid.enemyType == EnemyType.Cerberus)
								{
									num4 *= 1.5f;
								}
								if (componentInParent.eid.enemyType != EnemyType.Soldier || componentInParent.eid.isGasolined || unblockable || BlindEnemies.Blind || !componentInParent.eid.TryGetComponent<Zombie>(out var component2) || !component2.grounded || !component2.zp || component2.zp.difficulty < 2)
								{
									if (electric)
									{
										componentInParent.eid.hitterAttributes.Add(HitterAttribute.Electricity);
									}
									componentInParent.eid.DeliverDamage(componentInParent.gameObject, vector3 * 50000f, position, num4, tryForExplode: false, 0f, sourceWeapon, ignoreTotalDamageTakenMultiplier: false, fromExplosion: true);
									if (ignite)
									{
										if (componentInParent.eid.flammables != null && componentInParent.eid.flammables.Count > 0)
										{
											componentInParent.eid.StartBurning(damage / 10);
										}
										else
										{
											Flammable componentInChildren = componentInParent.eid.GetComponentInChildren<Flammable>();
											if (componentInChildren != null)
											{
												componentInChildren.Burn(damage / 10);
											}
										}
									}
								}
								else
								{
									componentInParent.eid.hitter = "blocked";
									if (component2.zp.difficulty <= 3 || electric)
									{
										if (electric)
										{
											componentInParent.eid.hitterAttributes.Add(HitterAttribute.Electricity);
										}
										componentInParent.eid.DeliverDamage(other.gameObject, Vector3.zero, position, num4 * 0.25f, tryForExplode: false, 0f, sourceWeapon, ignoreTotalDamageTakenMultiplier: false, fromExplosion: true);
									}
									component2.zp.Block(base.transform.position);
								}
							}
						}
					}
					else if (componentInParent.eid.dead)
					{
						hitColliders.Add(instanceID);
						componentInParent.eid.hitter = (enemy ? "enemy" : "explosion");
						componentInParent.eid.DeliverDamage(other.gameObject, normalized * 5000f, position, (float)damage / 10f * enemyDamageMultiplier, tryForExplode: false, 0f, sourceWeapon, ignoreTotalDamageTakenMultiplier: false, fromExplosion: true);
						if (ignite && componentInParent.TryGetComponent<Flammable>(out var _))
						{
							Flammable componentInChildren2 = componentInParent.eid.GetComponentInChildren<Flammable>();
							if (componentInChildren2 != null)
							{
								componentInChildren2.Burn(damage / 10);
							}
						}
					}
				}
			}
			else if (other.TryGetComponent<Breakable>(out component4) && !component4.unbreakable && !component4.precisionOnly && (!component4.playerOnly || !enemy))
			{
				if (!component4.accurateExplosionsOnly)
				{
					component4.Break();
				}
				else
				{
					Vector3 vector4 = other.ClosestPoint(base.transform.position);
					if (!Physics.Raycast(vector4 + (vector4 - base.transform.position).normalized * 0.001f, base.transform.position - vector4, Vector3.Distance(base.transform.position, vector4), lmask, QueryTriggerInteraction.Ignore))
					{
						component4.Break();
					}
				}
			}
			else if (other.TryGetComponent<Bleeder>(out component5))
			{
				bool flag = false;
				if (toIgnore.Count > 0 && component5.ignoreTypes.Length != 0)
				{
					EnemyType[] ignoreTypes = component5.ignoreTypes;
					foreach (EnemyType enemyType in ignoreTypes)
					{
						for (int j = 0; j < toIgnore.Count; j++)
						{
							if (enemyType == toIgnore[j])
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							break;
						}
					}
				}
				if (!flag)
				{
					component5.GetHit(position, GoreType.Head, fromExplosion: true);
				}
			}
			else if (other.TryGetComponent<Glass>(out component6))
			{
				component6.Shatter();
			}
			else if (ignite && other.TryGetComponent<Flammable>(out component7) && (!enemy || !component7.playerOnly) && (enemy || !component7.enemyOnly))
			{
				component7.Burn(4f);
			}
		}
		if (other.gameObject.CompareTag("Player") && MonoSingleton<PlayerTracker>.Instance.playerType != PlayerType.Platformer)
		{
			return;
		}
		Rigidbody component8 = other.GetComponent<Rigidbody>();
		bool flag2 = other.gameObject.layer == 14;
		if ((!((bool)component8 && flag2) || !other.gameObject.CompareTag("Metal") || !other.TryGetComponent<Nail>(out var component9) || component9.magnets.Count == 0) && (bool)component8 && (!flag2 || component8.useGravity) && !other.gameObject.CompareTag("IgnorePushes"))
		{
			hitColliders.Add(instanceID);
			Vector3 a = normalized * Mathf.Max(5f - num, 0f);
			a = Vector3.Scale(a, new Vector3(7500f, 1f, 7500f));
			if (component8.useGravity)
			{
				a = new Vector3(a.x, 18750f, a.z);
			}
			if (other.gameObject.layer == 27 || other.gameObject.layer == 9)
			{
				a = Vector3.ClampMagnitude(a, 5000f);
			}
			if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer && other.gameObject == MonoSingleton<PlatformerMovement>.Instance.gameObject)
			{
				a *= 30f;
			}
			component8.AddForce(a);
		}
		if (flag2)
		{
			ThrownSword component10 = other.GetComponent<ThrownSword>();
			Projectile component11 = other.GetComponent<Projectile>();
			if (component10 != null)
			{
				component10.deflected = true;
			}
			if (component11 != null && !component11.ignoreExplosions)
			{
				component11.homingType = HomingType.None;
				other.transform.LookAt(position + normalized);
				component11.friendly = true;
				component11.target = null;
				component11.turnSpeed = 0f;
				component11.speed = Mathf.Max(component11.speed, 65f);
			}
		}
	}
}

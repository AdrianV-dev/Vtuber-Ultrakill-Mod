using System.Collections.Generic;
using Sandbox;
using UnityEngine;
using UnityEngine.Serialization;

public class HurtZone : MonoBehaviour, IAlter, IAlterOptions<float>
{
	public EnviroDamageType damageType;

	public bool trigger;

	public AffectedSubjects affected;

	public float bounceForce;

	private Collider col;

	public float hurtCooldown = 1f;

	[FormerlySerializedAs("damage")]
	public float setDamage;

	public float hardDamagePercentage = 0.35f;

	public float enemyDamageOverride;

	private int hurtingPlayer;

	private float playerHurtCooldown;

	private List<HurtZoneEnemyTracker> enemies = new List<HurtZoneEnemyTracker>();

	public GameObject hurtParticle;

	private int difficulty;

	private float damageMultiplier = 1f;

	private NewMovement newMovement;

	private PlayerTracker playerTracker;

	private PlatformerMovement platformerMovement;

	public List<EnemyType> ignoredEnemyTypes = new List<EnemyType>();

	public GameObject sourceWeapon;

	private float damage => setDamage * damageMultiplier;

	public string alterKey => "hurt_zone";

	public string alterCategoryName => "Hurt Zone";

	public AlterOption<float>[] options => new AlterOption<float>[2]
	{
		new AlterOption<float>
		{
			key = "damage",
			name = "Damage",
			value = setDamage,
			callback = delegate(float f)
			{
				setDamage = f;
			},
			constraints = new SliderConstraints
			{
				min = 0f,
				max = 200f
			}
		},
		new AlterOption<float>
		{
			key = "hurt_cooldown",
			name = "Hurt Cooldown",
			value = hurtCooldown,
			callback = delegate(float f)
			{
				hurtCooldown = f;
			},
			constraints = new SliderConstraints
			{
				min = 0f,
				max = 10f,
				step = 0.1f
			}
		}
	};

	private void Start()
	{
		difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		if (difficulty < 2 && damage < 100f)
		{
			if (difficulty == 1)
			{
				damageMultiplier = 0.5f;
			}
			else if (difficulty == 0)
			{
				damageMultiplier = 0.25f;
			}
		}
		col = GetComponent<Collider>();
		newMovement = MonoSingleton<NewMovement>.Instance;
		playerTracker = MonoSingleton<PlayerTracker>.Instance;
		platformerMovement = MonoSingleton<PlatformerMovement>.Instance;
	}

	private void OnDisable()
	{
		hurtingPlayer = 0;
		enemies.Clear();
	}

	private void FixedUpdate()
	{
		if (!base.enabled)
		{
			return;
		}
		if (hurtingPlayer > 0 && playerHurtCooldown <= 0f && damage > 0f)
		{
			if (playerTracker.playerType == PlayerType.FPS)
			{
				if (newMovement == null)
				{
					newMovement = MonoSingleton<NewMovement>.Instance;
				}
				if (!newMovement.dead && newMovement.gameObject.activeInHierarchy)
				{
					if (hardDamagePercentage > 0f)
					{
						newMovement.GetHurt((int)damage, invincible: true, 1f, explosion: false, instablack: false, hardDamagePercentage);
					}
					else
					{
						newMovement.GetHurt((int)damage, invincible: false);
					}
					if ((bool)hurtParticle)
					{
						Object.Instantiate(hurtParticle, newMovement.transform.position, Quaternion.identity);
					}
					if (bounceForce != 0f && (bool)col)
					{
						newMovement.Launch((newMovement.transform.position - col.ClosestPoint(newMovement.transform.position)).normalized * bounceForce);
					}
				}
				else
				{
					hurtingPlayer = 0;
				}
			}
			else
			{
				if (platformerMovement == null)
				{
					platformerMovement = MonoSingleton<PlatformerMovement>.Instance;
				}
				if (!platformerMovement.dead && platformerMovement.gameObject.activeInHierarchy)
				{
					if (damageType == EnviroDamageType.WeakBurn || damageType == EnviroDamageType.Burn || damageType == EnviroDamageType.Acid)
					{
						platformerMovement.Burn();
					}
					else
					{
						platformerMovement.Explode();
						if ((bool)hurtParticle)
						{
							Object.Instantiate(hurtParticle, platformerMovement.transform.position, Quaternion.identity);
						}
					}
				}
				else
				{
					hurtingPlayer = 0;
				}
			}
			playerHurtCooldown = hurtCooldown;
		}
		else if (playerHurtCooldown > 0f)
		{
			playerHurtCooldown -= Time.deltaTime;
		}
		if (enemies.Count <= 0)
		{
			return;
		}
		for (int num = enemies.Count - 1; num >= 0; num--)
		{
			if (enemies[num] == null || enemies[num].target == null || !enemies[num].HasLimbs(col))
			{
				enemies.RemoveAt(num);
				continue;
			}
			EnemyIdentifier target = enemies[num].target;
			float timer = enemies[num].timer;
			timer -= Time.deltaTime;
			if (timer <= 0f)
			{
				if (!DamageEnemy(target, num))
				{
					continue;
				}
				timer = ((!target.dead || damageType != EnviroDamageType.Acid) ? hurtCooldown : 0.1f);
			}
			enemies[num].timer = timer;
		}
	}

	private bool DamageEnemy(EnemyIdentifier eid, int i)
	{
		if (eid.enemyType == EnemyType.V2 && eid.TryGetComponent<V2>(out var component) && component.inIntro)
		{
			return false;
		}
		if (damageType == EnviroDamageType.Burn || damageType == EnviroDamageType.WeakBurn)
		{
			eid.hitter = "fire";
		}
		else if (damageType == EnviroDamageType.Acid)
		{
			eid.hitter = "acid";
		}
		else if (damageType == EnviroDamageType.Chainsaw)
		{
			eid.hitter = "chainsawzone";
		}
		else
		{
			eid.hitter = "environment";
		}
		GameObject gameObject = eid.gameObject;
		gameObject = ((damageType != EnviroDamageType.Chainsaw || !Physics.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward, out var hitInfo, 15f, LayerMaskDefaults.Get(LMD.Enemies)) || !hitInfo.transform.TryGetComponent<EnemyIdentifierIdentifier>(out var component2) || !component2.eid || !(component2.eid == eid)) ? enemies[i].limbs[enemies[i].limbs.Count - 1].gameObject : hitInfo.transform.gameObject);
		if (eid.dead && damageType != EnviroDamageType.Chainsaw)
		{
			if (eid.enemyClass == EnemyClass.Demon || (eid.enemyClass == EnemyClass.Machine && (bool)eid.machine && !eid.machine.dismemberment))
			{
				enemies.RemoveAt(i);
				return false;
			}
			if (gameObject == eid.gameObject || gameObject.layer == 12 || gameObject.layer == 20 || gameObject.CompareTag("Body"))
			{
				enemies[i].limbs.RemoveAt(enemies[i].limbs.Count - 1);
			}
		}
		Vector3 vector = eid.transform.position;
		if (damageType == EnviroDamageType.Chainsaw)
		{
			vector = (eid.dead ? gameObject.transform.position : ((eid.TryGetComponent<Collider>(out var component3) && component3.Raycast(new Ray(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward), out hitInfo, 15f)) ? hitInfo.point : ((!component3) ? new Vector3(eid.transform.position.x, MonoSingleton<CameraController>.Instance.GetDefaultPos().y, eid.transform.position.z) : component3.ClosestPoint(MonoSingleton<CameraController>.Instance.GetDefaultPos()))));
			MonoSingleton<CameraController>.Instance.CameraShake(0.2f);
		}
		if ((bool)hurtParticle && !eid.dead)
		{
			Object.Instantiate(hurtParticle, vector, Quaternion.identity);
		}
		eid.DeliverDamage(gameObject, Vector3.zero, vector, (enemyDamageOverride == 0f) ? (damage / 2f) : enemyDamageOverride, tryForExplode: false, 0f, sourceWeapon);
		if ((damageType == EnviroDamageType.Burn || damageType == EnviroDamageType.WeakBurn) && !eid.dead)
		{
			Flammable componentInChildren = eid.GetComponentInChildren<Flammable>();
			if (componentInChildren != null)
			{
				componentInChildren.Burn(4f);
			}
		}
		return true;
	}

	private HurtZoneEnemyTracker EnemiesContains(EnemyIdentifier eid)
	{
		if (enemies.Count == 0)
		{
			return null;
		}
		for (int num = enemies.Count - 1; num >= 0; num--)
		{
			if (enemies[num] == null || enemies[num].target == null)
			{
				enemies.RemoveAt(num);
			}
			else if (enemies[num].target == eid)
			{
				return enemies[num];
			}
		}
		return null;
	}

	private void Enter(Collider other)
	{
		if (affected != AffectedSubjects.EnemiesOnly && other.gameObject.CompareTag("Player"))
		{
			hurtingPlayer++;
		}
		else
		{
			if (affected == AffectedSubjects.PlayerOnly || (other.gameObject.layer != 10 && other.gameObject.layer != 11 && other.gameObject.layer != 12 && other.gameObject.layer != 20))
			{
				return;
			}
			EnemyIdentifierIdentifier enemyIdentifierIdentifier = ((other.gameObject.layer == 12) ? other.gameObject.GetComponentInChildren<EnemyIdentifierIdentifier>() : ((other.gameObject.layer != 20 || !other.transform.parent) ? other.gameObject.GetComponent<EnemyIdentifierIdentifier>() : other.transform.parent.GetComponentInChildren<EnemyIdentifierIdentifier>()));
			if (!(enemyIdentifierIdentifier != null) || !(enemyIdentifierIdentifier.eid != null) || (ignoredEnemyTypes.Count != 0 && ignoredEnemyTypes.Contains(enemyIdentifierIdentifier.eid.enemyType)) || (enemyIdentifierIdentifier.eid.dead && ((damageType != EnviroDamageType.Chainsaw && enemyIdentifierIdentifier.eid.enemyClass == EnemyClass.Demon) || other.gameObject.layer == 12 || other.gameObject.layer == 20)) || !(enemyIdentifierIdentifier.transform.localScale != Vector3.zero) || (damageType == EnviroDamageType.WeakBurn && (enemyIdentifierIdentifier.eid.enemyType == EnemyType.Streetcleaner || enemyIdentifierIdentifier.eid.enemyType == EnemyType.Sisyphus || enemyIdentifierIdentifier.eid.enemyType == EnemyType.Stalker)))
			{
				return;
			}
			HurtZoneEnemyTracker hurtZoneEnemyTracker = EnemiesContains(enemyIdentifierIdentifier.eid);
			if (hurtZoneEnemyTracker == null)
			{
				enemies.Add(new HurtZoneEnemyTracker(enemyIdentifierIdentifier.eid, other, hurtCooldown));
				if (base.enabled)
				{
					DamageEnemy(enemyIdentifierIdentifier.eid, enemies.Count - 1);
				}
			}
			else
			{
				hurtZoneEnemyTracker.limbs.Add(other);
			}
		}
	}

	private void Exit(Collider other)
	{
		if (affected != AffectedSubjects.EnemiesOnly && other.gameObject.CompareTag("Player") && hurtingPlayer > 0)
		{
			hurtingPlayer--;
		}
		else
		{
			if (affected == AffectedSubjects.PlayerOnly || (other.gameObject.layer != 10 && other.gameObject.layer != 11 && other.gameObject.layer != 12 && other.gameObject.layer != 20))
			{
				return;
			}
			EnemyIdentifierIdentifier enemyIdentifierIdentifier = ((other.gameObject.layer == 12) ? other.gameObject.GetComponentInChildren<EnemyIdentifierIdentifier>() : ((other.gameObject.layer != 20 || !other.transform.parent) ? other.gameObject.GetComponent<EnemyIdentifierIdentifier>() : other.transform.parent.GetComponentInChildren<EnemyIdentifierIdentifier>()));
			if (!(enemyIdentifierIdentifier != null) || !(enemyIdentifierIdentifier.eid != null))
			{
				return;
			}
			HurtZoneEnemyTracker hurtZoneEnemyTracker = EnemiesContains(enemyIdentifierIdentifier.eid);
			if (hurtZoneEnemyTracker != null && hurtZoneEnemyTracker.limbs.Contains(other))
			{
				hurtZoneEnemyTracker.limbs.Remove(other);
				if (!hurtZoneEnemyTracker.HasLimbs(col))
				{
					enemies.Remove(hurtZoneEnemyTracker);
				}
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (trigger || other.gameObject.layer == 20)
		{
			Enter(other);
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!trigger)
		{
			Enter(other.collider);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (trigger || other.gameObject.layer == 20)
		{
			Exit(other);
		}
	}

	private void OnCollisionExit(Collision other)
	{
		if (!trigger)
		{
			Exit(other.collider);
		}
	}
}

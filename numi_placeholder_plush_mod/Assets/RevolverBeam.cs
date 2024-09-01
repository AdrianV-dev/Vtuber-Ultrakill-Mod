using System.Collections.Generic;
using CustomRay;
using Sandbox;
using UnityEngine;

public class RevolverBeam : MonoBehaviour
{
	private const float ForceBulletPropMulti = 0.005f;

	public EnemyTarget target;

	public BeamType beamType;

	public HitterAttribute[] attributes;

	private LineRenderer lr;

	private AudioSource aud;

	private Light muzzleLight;

	public Vector3 alternateStartPoint;

	public GameObject sourceWeapon;

	[HideInInspector]
	public int bodiesPierced;

	private int enemiesPierced;

	private RaycastHit[] allHits;

	[HideInInspector]
	public List<RaycastResult> hitList = new List<RaycastResult>();

	private GunControl gc;

	private RaycastHit hit;

	private Vector3 shotHitPoint;

	public CameraController cc;

	private bool maliciousIgnorePlayer;

	public GameObject hitParticle;

	public int bulletForce;

	public bool quickDraw;

	public int gunVariation;

	public float damage;

	[HideInInspector]
	public float addedDamage;

	public float enemyDamageOverride;

	public float critDamageOverride;

	public float screenshakeMultiplier = 1f;

	public int hitAmount;

	public int maxHitsPerTarget;

	private int currentHits;

	public bool noMuzzleflash;

	private bool fadeOut;

	private bool didntHit;

	private LayerMask ignoreEnemyTrigger;

	private LayerMask enemyLayerMask;

	private LayerMask pierceLayerMask;

	public int ricochetAmount;

	[HideInInspector]
	public bool hasBeenRicocheter;

	public GameObject ricochetSound;

	public GameObject enemyHitSound;

	public bool fake;

	public EnemyType ignoreEnemyType;

	public bool deflected;

	private bool chargeBacked;

	public bool strongAlt;

	public bool ultraRicocheter = true;

	public bool canHitProjectiles;

	private bool hasHitProjectile;

	[HideInInspector]
	public List<EnemyIdentifier> hitEids = new List<EnemyIdentifier>();

	[HideInInspector]
	public Transform previouslyHitTransform;

	[HideInInspector]
	public bool aimAssist;

	[HideInInspector]
	public bool intentionalRicochet;

	private void Start()
	{
		if (aimAssist)
		{
			RicochetAimAssist(base.gameObject, intentionalRicochet);
		}
		if (ricochetAmount > 0)
		{
			hasBeenRicocheter = true;
		}
		muzzleLight = GetComponent<Light>();
		lr = GetComponent<LineRenderer>();
		cc = MonoSingleton<CameraController>.Instance;
		gc = cc.GetComponentInChildren<GunControl>();
		if (beamType == BeamType.Enemy)
		{
			enemyLayerMask = (int)enemyLayerMask | 4;
		}
		enemyLayerMask = (int)enemyLayerMask | 0x400;
		enemyLayerMask = (int)enemyLayerMask | 0x800;
		if (canHitProjectiles)
		{
			enemyLayerMask = (int)enemyLayerMask | 0x4000;
		}
		pierceLayerMask = (int)pierceLayerMask | 0x100;
		pierceLayerMask = (int)pierceLayerMask | 0x1000000;
		pierceLayerMask = (int)pierceLayerMask | 0x4000000;
		ignoreEnemyTrigger = (int)enemyLayerMask | (int)pierceLayerMask;
		if (!fake)
		{
			Shoot();
		}
		else
		{
			fadeOut = true;
		}
		if (maxHitsPerTarget == 0)
		{
			maxHitsPerTarget = 99;
		}
	}

	private void Update()
	{
		if (fadeOut)
		{
			lr.widthMultiplier -= Time.deltaTime * 1.5f;
			if (muzzleLight != null)
			{
				muzzleLight.intensity -= Time.deltaTime * 100f;
			}
			if (lr.widthMultiplier <= 0f)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	public void FakeShoot(Vector3 target)
	{
		Vector3 position = base.transform.position;
		if (alternateStartPoint != Vector3.zero)
		{
			position = alternateStartPoint;
		}
		lr.SetPosition(0, position);
		lr.SetPosition(1, target);
		Transform child = base.transform.GetChild(0);
		if (!noMuzzleflash)
		{
			child.SetPositionAndRotation(position, base.transform.rotation);
		}
		else
		{
			child.gameObject.SetActive(value: false);
		}
	}

	private void Shoot()
	{
		if (hitAmount == 1)
		{
			fadeOut = true;
			if (beamType != BeamType.Enemy)
			{
				if (beamType == BeamType.Railgun)
				{
					cc.CameraShake(2f * screenshakeMultiplier);
				}
				else if (strongAlt)
				{
					cc.CameraShake(0.25f * screenshakeMultiplier);
				}
			}
			bool flag = Physics.Raycast(base.transform.position, base.transform.forward, out hit, float.PositiveInfinity, ignoreEnemyTrigger);
			CheckWater(hit.distance);
			bool flag2 = false;
			RaycastHit hitInfo = default(RaycastHit);
			if (flag && (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 24))
			{
				flag2 = Physics.SphereCast(base.transform.position, (beamType == BeamType.Enemy) ? 0.1f : 0.4f, base.transform.forward, out hitInfo, Vector3.Distance(base.transform.position, hit.point) - ((beamType == BeamType.Enemy) ? 0.1f : 0.4f), enemyLayerMask);
			}
			if (flag2)
			{
				HitSomething(hitInfo);
			}
			else if (flag)
			{
				HitSomething(hit);
			}
			else
			{
				shotHitPoint = base.transform.position + base.transform.forward * 1000f;
			}
		}
		else
		{
			if (Physics.Raycast(base.transform.position, base.transform.forward, out hit, float.PositiveInfinity, pierceLayerMask))
			{
				shotHitPoint = hit.point;
			}
			else
			{
				shotHitPoint = base.transform.position + base.transform.forward * 999f;
				didntHit = true;
			}
			CheckWater(Vector3.Distance(base.transform.position, shotHitPoint));
			float radius = 0.6f;
			if (beamType == BeamType.Railgun)
			{
				radius = 1.2f;
			}
			else if (beamType == BeamType.Enemy)
			{
				radius = 0.3f;
			}
			allHits = Physics.SphereCastAll(base.transform.position, radius, base.transform.forward, Vector3.Distance(base.transform.position, shotHitPoint), enemyLayerMask, QueryTriggerInteraction.Collide);
		}
		Vector3 position = base.transform.position;
		if (alternateStartPoint != Vector3.zero)
		{
			position = alternateStartPoint;
		}
		lr.SetPosition(0, position);
		lr.SetPosition(1, shotHitPoint);
		if (hitAmount != 1)
		{
			PiercingShotOrder();
		}
		Transform child = base.transform.GetChild(0);
		if (!noMuzzleflash)
		{
			child.SetPositionAndRotation(position, base.transform.rotation);
		}
		else
		{
			child.gameObject.SetActive(value: false);
		}
	}

	private void CheckWater(float distance)
	{
		if (attributes.Length == 0)
		{
			return;
		}
		bool flag = false;
		HitterAttribute[] array = attributes;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == HitterAttribute.Electricity)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		Water component = null;
		List<Water> list = new List<Water>();
		List<GameObject> alreadyHitObjects = new List<GameObject>();
		Collider[] array2 = Physics.OverlapSphere(base.transform.position, 0.01f, 16, QueryTriggerInteraction.Collide);
		if (array2.Length != 0)
		{
			for (int j = 0; j < array2.Length; j++)
			{
				if ((!array2[j].attachedRigidbody && !array2[j].TryGetComponent<Water>(out component)) || ((bool)array2[j].attachedRigidbody && !array2[j].attachedRigidbody.TryGetComponent<Water>(out component)) || list.Contains(component))
				{
					return;
				}
				list.Add(component);
				EnemyIdentifier.Zap(base.transform.position, 2f, alreadyHitObjects, sourceWeapon, null, component, waterOnly: true);
			}
		}
		RaycastHit[] array3 = Physics.RaycastAll(base.transform.position, base.transform.forward, distance, 16, QueryTriggerInteraction.Collide);
		if (array3.Length == 0)
		{
			return;
		}
		for (int k = 0; k < array3.Length && array3[k].transform.TryGetComponent<Water>(out component); k++)
		{
			if (list.Contains(component))
			{
				break;
			}
			list.Add(component);
			EnemyIdentifier.Zap(array3[k].point, 2f, alreadyHitObjects, sourceWeapon, null, component, waterOnly: true);
		}
	}

	private void HitSomething(RaycastHit hit)
	{
		bool flag = false;
		if (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 24)
		{
			ExecuteHits(hit);
		}
		else if (beamType != 0 && hit.transform.gameObject.CompareTag("Coin"))
		{
			flag = true;
			lr.SetPosition(1, hit.transform.position);
			GameObject gameObject = Object.Instantiate(base.gameObject, hit.point, base.transform.rotation);
			gameObject.SetActive(value: false);
			RevolverBeam component = gameObject.GetComponent<RevolverBeam>();
			component.bodiesPierced = 0;
			component.noMuzzleflash = true;
			component.alternateStartPoint = Vector3.zero;
			if (beamType == BeamType.MaliciousFace || beamType == BeamType.Enemy)
			{
				component.deflected = true;
			}
			Coin component2 = hit.transform.gameObject.GetComponent<Coin>();
			if (component2 != null)
			{
				if (component.deflected)
				{
					component2.ignoreBlessedEnemies = true;
				}
				sourceWeapon = component2.sourceWeapon ?? sourceWeapon;
				component2.DelayedReflectRevolver(hit.point, gameObject);
			}
			fadeOut = true;
		}
		else
		{
			ExecuteHits(hit);
		}
		shotHitPoint = hit.point;
		if (hit.transform.gameObject.CompareTag("Armor") || flag || !(hitParticle != null))
		{
			return;
		}
		GameObject obj = Object.Instantiate(hitParticle, shotHitPoint, base.transform.rotation);
		obj.transform.forward = hit.normal;
		Explosion[] componentsInChildren = obj.GetComponentsInChildren<Explosion>();
		Explosion[] array = componentsInChildren;
		foreach (Explosion explosion in array)
		{
			explosion.sourceWeapon = sourceWeapon ?? explosion.sourceWeapon;
			if (explosion.damage > 0 && addedDamage > 0f)
			{
				explosion.playerDamageOverride = explosion.damage;
				explosion.damage += Mathf.RoundToInt(addedDamage * 20f);
			}
		}
		if ((beamType != BeamType.MaliciousFace && (beamType != BeamType.Railgun || !maliciousIgnorePlayer)) || componentsInChildren.Length == 0)
		{
			return;
		}
		int @int = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		if (beamType == BeamType.MaliciousFace)
		{
			array = componentsInChildren;
			foreach (Explosion explosion2 in array)
			{
				if (deflected || maliciousIgnorePlayer)
				{
					explosion2.unblockable = true;
					explosion2.canHit = AffectedSubjects.EnemiesOnly;
				}
				else
				{
					explosion2.enemy = true;
				}
				if (@int < 2)
				{
					explosion2.maxSize *= 0.65f;
					explosion2.speed *= 0.65f;
				}
			}
		}
		else
		{
			array = componentsInChildren;
			foreach (Explosion explosion3 in array)
			{
				explosion3.sourceWeapon = sourceWeapon ?? explosion3.sourceWeapon;
				explosion3.canHit = AffectedSubjects.EnemiesOnly;
			}
		}
	}

	private void PiercingShotOrder()
	{
		hitList.Clear();
		RaycastHit[] array = allHits;
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if (raycastHit.transform != previouslyHitTransform)
			{
				hitList.Add(new RaycastResult(raycastHit));
			}
		}
		bool flag = true;
		Transform transform = hit.transform;
		if (!didntHit)
		{
			GameObject gameObject = transform.gameObject;
			if (gameObject.layer == 8 || gameObject.layer == 24)
			{
				if (gameObject.TryGetComponent<SandboxProp>(out var _) && hit.rigidbody != null)
				{
					hit.rigidbody.AddForceAtPosition(base.transform.forward * bulletForce * 0.005f, hit.point, ForceMode.VelocityChange);
				}
				AttributeChecker component4;
				if (transform.TryGetComponent<Breakable>(out var _) || gameObject.TryGetComponent<Bleeder>(out var _))
				{
					flag = true;
				}
				else if (transform.TryGetComponent<AttributeChecker>(out component4))
				{
					flag = true;
				}
			}
			if (flag || gameObject.CompareTag("Glass") || gameObject.CompareTag("GlassFloor") || gameObject.CompareTag("Armor"))
			{
				hitList.Add(new RaycastResult(hit));
			}
		}
		hitList.Sort();
		PiercingShotCheck();
	}

	private void PiercingShotCheck()
	{
		if (enemiesPierced < hitList.Count)
		{
			RaycastResult raycastResult = hitList[enemiesPierced];
			RaycastHit rrhit = raycastResult.rrhit;
			Transform transform = raycastResult.transform;
			if (transform == null)
			{
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			GameObject gameObject = transform.gameObject;
			if (gameObject.CompareTag("Armor") || (ricochetAmount > 0 && (gameObject.layer == 8 || gameObject.layer == 24 || gameObject.layer == 0)))
			{
				bool flag = !gameObject.CompareTag("Armor");
				GameObject gameObject2 = Object.Instantiate(base.gameObject, rrhit.point, base.transform.rotation);
				gameObject2.transform.forward = Vector3.Reflect(base.transform.forward, rrhit.normal);
				lr.SetPosition(1, rrhit.point);
				RevolverBeam component = gameObject2.GetComponent<RevolverBeam>();
				component.noMuzzleflash = true;
				component.alternateStartPoint = Vector3.zero;
				component.bodiesPierced = bodiesPierced;
				component.previouslyHitTransform = transform;
				component.aimAssist = true;
				component.intentionalRicochet = flag;
				if (flag)
				{
					ricochetAmount--;
					if (beamType != 0 || component.maxHitsPerTarget < 3 || (strongAlt && component.maxHitsPerTarget < 4))
					{
						component.maxHitsPerTarget++;
					}
					component.hitEids.Clear();
				}
				component.ricochetAmount = ricochetAmount;
				GameObject gameObject3 = Object.Instantiate(ricochetSound, rrhit.point, Quaternion.identity);
				gameObject3.SetActive(value: false);
				gameObject2.SetActive(value: false);
				MonoSingleton<DelayedActivationManager>.Instance.Add(gameObject2, 0.1f);
				MonoSingleton<DelayedActivationManager>.Instance.Add(gameObject3, 0.1f);
				if (gameObject.TryGetComponent<Glass>(out var component2) && !component2.broken)
				{
					component2.Shatter();
				}
				if (gameObject.TryGetComponent<Breakable>(out var component3) && (strongAlt || component3.weak || beamType == BeamType.Railgun))
				{
					component3.Break();
				}
				fadeOut = true;
				enemiesPierced = hitList.Count;
				return;
			}
			if (gameObject.CompareTag("Coin") && bodiesPierced < hitAmount)
			{
				if (!gameObject.TryGetComponent<Coin>(out var component4))
				{
					enemiesPierced++;
					PiercingShotCheck();
					return;
				}
				lr.SetPosition(1, transform.position);
				GameObject gameObject4 = Object.Instantiate(base.gameObject, rrhit.point, base.transform.rotation);
				gameObject4.SetActive(value: false);
				RevolverBeam component5 = gameObject4.GetComponent<RevolverBeam>();
				component5.bodiesPierced = 0;
				component5.noMuzzleflash = true;
				component5.alternateStartPoint = Vector3.zero;
				component5.hitEids.Clear();
				Revolver component6;
				if (beamType == BeamType.Enemy)
				{
					component4.ignoreBlessedEnemies = true;
					component5.deflected = true;
				}
				else if (beamType == BeamType.Revolver && strongAlt && component4.hitTimes > 1 && (bool)sourceWeapon && sourceWeapon.TryGetComponent<Revolver>(out component6) && component6.altVersion)
				{
					component6.InstaClick();
				}
				component4.DelayedReflectRevolver(rrhit.point, gameObject4);
				fadeOut = true;
				return;
			}
			if ((gameObject.layer == 10 || gameObject.layer == 11) && bodiesPierced < hitAmount && !gameObject.CompareTag("Breakable"))
			{
				EnemyIdentifierIdentifier componentInParent = gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
				if (!componentInParent)
				{
					if (attributes.Length != 0 && transform.TryGetComponent<AttributeChecker>(out var component7))
					{
						HitterAttribute[] array = attributes;
						for (int i = 0; i < array.Length; i++)
						{
							if (array[i] == component7.targetAttribute)
							{
								component7.DelayedActivate();
								break;
							}
						}
					}
					enemiesPierced++;
					currentHits = 0;
					PiercingShotCheck();
					return;
				}
				EnemyIdentifier eid = componentInParent.eid;
				if (eid != null)
				{
					if ((!hitEids.Contains(eid) || (eid.dead && beamType == BeamType.Revolver && enemiesPierced == hitList.Count - 1)) && ((beamType != BeamType.Enemy && beamType != BeamType.MaliciousFace) || deflected || (eid.enemyType != ignoreEnemyType && !eid.immuneToFriendlyFire && !EnemyIdentifier.CheckHurtException(ignoreEnemyType, eid.enemyType, target))))
					{
						bool dead = eid.dead;
						ExecuteHits(rrhit);
						if (!dead || gameObject.layer == 11 || (beamType == BeamType.Revolver && enemiesPierced == hitList.Count - 1))
						{
							currentHits++;
							bodiesPierced++;
							Object.Instantiate(hitParticle, rrhit.point, base.transform.rotation);
							MonoSingleton<TimeController>.Instance.HitStop(0.05f);
						}
						else
						{
							if (beamType == BeamType.Revolver)
							{
								hitEids.Add(eid);
							}
							enemiesPierced++;
							currentHits = 0;
						}
						if (currentHits >= maxHitsPerTarget)
						{
							hitEids.Add(eid);
							currentHits = 0;
							enemiesPierced++;
						}
						if (beamType == BeamType.Revolver && !dead)
						{
							Invoke("PiercingShotCheck", 0.05f);
						}
						else if (beamType == BeamType.Revolver)
						{
							PiercingShotCheck();
						}
						else if (!dead)
						{
							Invoke("PiercingShotCheck", 0.025f);
						}
						else
						{
							Invoke("PiercingShotCheck", 0.01f);
						}
					}
					else
					{
						enemiesPierced++;
						currentHits = 0;
						PiercingShotCheck();
					}
				}
				else
				{
					ExecuteHits(rrhit);
					enemiesPierced++;
					PiercingShotCheck();
				}
				return;
			}
			if (canHitProjectiles && gameObject.layer == 14)
			{
				if (!hasHitProjectile)
				{
					Invoke("PiercingShotCheck", 0.01f);
				}
				else
				{
					MonoSingleton<TimeController>.Instance.HitStop(0.05f);
					Invoke("PiercingShotCheck", 0.05f);
				}
				ExecuteHits(rrhit);
				enemiesPierced++;
				return;
			}
			if (gameObject.CompareTag("Glass") || gameObject.CompareTag("GlassFloor"))
			{
				gameObject.TryGetComponent<Glass>(out var component8);
				if (!component8.broken)
				{
					component8.Shatter();
				}
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			if (beamType == BeamType.Enemy && bodiesPierced < hitAmount && !rrhit.collider.isTrigger && gameObject.CompareTag("Player"))
			{
				ExecuteHits(rrhit);
				bodiesPierced++;
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			if (transform.TryGetComponent<Breakable>(out var component9) && (beamType == BeamType.Railgun || component9.weak))
			{
				if (component9.interrupt)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(100, "ultrakill.interruption", sourceWeapon);
					MonoSingleton<TimeController>.Instance.ParryFlash();
					if (canHitProjectiles)
					{
						component9.breakParticle = MonoSingleton<DefaultReferenceManager>.Instance.superExplosion;
					}
					if ((bool)component9.interruptEnemy && !component9.interruptEnemy.blessed)
					{
						component9.interruptEnemy.Explode(fromExplosion: true);
					}
				}
				component9.Break();
			}
			else if (bodiesPierced < hitAmount)
			{
				ExecuteHits(rrhit);
			}
			Object.Instantiate(hitParticle, rrhit.point, Quaternion.LookRotation(rrhit.normal));
			enemiesPierced++;
			PiercingShotCheck();
		}
		else
		{
			enemiesPierced = 0;
			fadeOut = true;
		}
	}

	public void ExecuteHits(RaycastHit currentHit)
	{
		Transform transform = currentHit.transform;
		if (!(transform != null))
		{
			return;
		}
		GameObject gameObject = transform.gameObject;
		if (transform.TryGetComponent<Breakable>(out var component) && (strongAlt || beamType == BeamType.Railgun || component.weak))
		{
			if (component.interrupt)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100, "ultrakill.interruption", sourceWeapon);
				MonoSingleton<TimeController>.Instance.ParryFlash();
				if (canHitProjectiles)
				{
					component.breakParticle = MonoSingleton<DefaultReferenceManager>.Instance.superExplosion;
				}
				if ((bool)component.interruptEnemy && !component.interruptEnemy.blessed)
				{
					component.interruptEnemy.Explode(fromExplosion: true);
				}
			}
			component.Break();
		}
		if (gameObject.TryGetComponent<Glass>(out var component2) && !component2.broken && beamType == BeamType.Enemy)
		{
			component2.Shatter();
		}
		if (canHitProjectiles && gameObject.layer == 14 && gameObject.TryGetComponent<Projectile>(out var component3) && (component3.speed != 0f || component3.turnSpeed != 0f || component3.decorative))
		{
			Object.Instantiate((!hasHitProjectile) ? MonoSingleton<DefaultReferenceManager>.Instance.superExplosion : component3.explosionEffect, component3.transform.position, Quaternion.identity);
			Object.Destroy(component3.gameObject);
			if (!hasHitProjectile)
			{
				MonoSingleton<TimeController>.Instance.ParryFlash();
			}
			hasHitProjectile = true;
		}
		if (gameObject.TryGetComponent<Bleeder>(out var component4))
		{
			if (beamType == BeamType.Railgun || strongAlt)
			{
				component4.GetHit(currentHit.point, GoreType.Head);
			}
			else
			{
				component4.GetHit(currentHit.point, GoreType.Body);
			}
		}
		if (gameObject.TryGetComponent<SandboxProp>(out var _) && currentHit.rigidbody != null)
		{
			currentHit.rigidbody.AddForceAtPosition(base.transform.forward * bulletForce * 0.005f, hit.point, ForceMode.VelocityChange);
		}
		if (transform.TryGetComponent<Coin>(out var component6) && beamType == BeamType.Revolver)
		{
			if (quickDraw)
			{
				component6.quickDraw = true;
			}
			component6.DelayedReflectRevolver(currentHit.point);
		}
		if (gameObject.CompareTag("Enemy") || gameObject.CompareTag("Body") || gameObject.CompareTag("Limb") || gameObject.CompareTag("EndLimb") || gameObject.CompareTag("Head"))
		{
			EnemyIdentifier eid = transform.GetComponentInParent<EnemyIdentifierIdentifier>().eid;
			if ((bool)eid && !deflected && (beamType == BeamType.MaliciousFace || beamType == BeamType.Enemy) && (eid.enemyType == ignoreEnemyType || eid.immuneToFriendlyFire || EnemyIdentifier.CheckHurtException(ignoreEnemyType, eid.enemyType, target)))
			{
				enemiesPierced++;
				return;
			}
			if (beamType != BeamType.Enemy)
			{
				if (hitAmount > 1)
				{
					cc.CameraShake(1f * screenshakeMultiplier);
				}
				else
				{
					cc.CameraShake(0.5f * screenshakeMultiplier);
				}
			}
			if ((bool)eid && !eid.dead && quickDraw && !eid.blessed && !eid.puppet)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(50, "ultrakill.quickdraw", sourceWeapon, eid);
				quickDraw = false;
			}
			string text = "";
			if (beamType == BeamType.Revolver)
			{
				text = "revolver";
			}
			else if (beamType == BeamType.Railgun)
			{
				text = "railcannon";
			}
			else if (beamType == BeamType.MaliciousFace || beamType == BeamType.Enemy)
			{
				text = "enemy";
			}
			if ((bool)eid)
			{
				eid.hitter = text;
				if (attributes != null && attributes.Length != 0)
				{
					HitterAttribute[] array = attributes;
					foreach (HitterAttribute item in array)
					{
						eid.hitterAttributes.Add(item);
					}
				}
				if (!eid.hitterWeapons.Contains(text + gunVariation))
				{
					eid.hitterWeapons.Add(text + gunVariation);
				}
			}
			float critMultiplier = 1f;
			if (beamType != 0)
			{
				critMultiplier = 0f;
			}
			if (critDamageOverride != 0f || strongAlt)
			{
				critMultiplier = critDamageOverride;
			}
			float num = ((enemyDamageOverride != 0f) ? enemyDamageOverride : damage);
			if ((bool)eid && deflected)
			{
				if (beamType == BeamType.MaliciousFace && eid.enemyType == EnemyType.MaliciousFace)
				{
					num = 999f;
				}
				else if (beamType == BeamType.Enemy)
				{
					num *= 2.5f;
				}
				if (!chargeBacked)
				{
					chargeBacked = true;
					if (!eid.blessed)
					{
						MonoSingleton<StyleHUD>.Instance.AddPoints(400, "ultrakill.chargeback", sourceWeapon, eid);
					}
				}
			}
			bool tryForExplode = false;
			if (strongAlt)
			{
				tryForExplode = true;
			}
			if ((bool)eid)
			{
				eid.DeliverDamage(gameObject, (transform.position - base.transform.position).normalized * bulletForce, currentHit.point, num, tryForExplode, critMultiplier, sourceWeapon);
			}
			if (beamType != BeamType.MaliciousFace && beamType != BeamType.Enemy)
			{
				if ((bool)eid && !eid.dead && beamType == BeamType.Revolver && !eid.blessed && gameObject.CompareTag("Head"))
				{
					gc.headshots++;
					gc.headShotComboTime = 3f;
				}
				else if (beamType == BeamType.Railgun || !gameObject.CompareTag("Head"))
				{
					gc.headshots = 0;
					gc.headShotComboTime = 0f;
				}
				if (gc.headshots > 1 && (bool)eid && !eid.blessed)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(gc.headshots * 20, "ultrakill.headshotcombo", count: gc.headshots, sourceWeapon: sourceWeapon, eid: eid);
				}
			}
			if ((bool)enemyHitSound)
			{
				Object.Instantiate(enemyHitSound, currentHit.point, Quaternion.identity);
			}
		}
		else if (gameObject.layer == 10)
		{
			Grenade componentInParent = transform.GetComponentInParent<Grenade>();
			if (componentInParent != null)
			{
				if (beamType != BeamType.Enemy || !componentInParent.enemy || componentInParent.playerRiding)
				{
					MonoSingleton<TimeController>.Instance.ParryFlash();
				}
				if ((beamType == BeamType.Railgun && hitAmount == 1) || beamType == BeamType.MaliciousFace)
				{
					maliciousIgnorePlayer = true;
					componentInParent.Explode(componentInParent.rocket, harmless: false, !componentInParent.rocket, 2f, ultrabooster: true, sourceWeapon);
				}
				else
				{
					componentInParent.Explode(componentInParent.rocket, harmless: false, !componentInParent.rocket, 1f, ultrabooster: false, sourceWeapon);
				}
			}
			else
			{
				Cannonball componentInParent2 = transform.GetComponentInParent<Cannonball>();
				if ((bool)componentInParent2)
				{
					MonoSingleton<TimeController>.Instance.ParryFlash();
					componentInParent2.Explode();
				}
			}
		}
		else if (beamType == BeamType.Enemy && !currentHit.collider.isTrigger && gameObject.CompareTag("Player"))
		{
			if ((bool)enemyHitSound)
			{
				Object.Instantiate(enemyHitSound, currentHit.point, Quaternion.identity);
			}
			if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
			{
				MonoSingleton<NewMovement>.Instance.GetHurt(Mathf.RoundToInt(damage * 10f), invincible: true);
			}
			else
			{
				MonoSingleton<PlatformerMovement>.Instance.Explode();
			}
		}
		else
		{
			if ((bool)gc)
			{
				gc.headshots = 0;
				gc.headShotComboTime = 0f;
			}
			if (gameObject.CompareTag("Armor"))
			{
				GameObject gameObject2 = Object.Instantiate(base.gameObject, currentHit.point, base.transform.rotation);
				gameObject2.transform.forward = Vector3.Reflect(base.transform.forward, currentHit.normal);
				RevolverBeam component7 = gameObject2.GetComponent<RevolverBeam>();
				component7.noMuzzleflash = true;
				component7.alternateStartPoint = Vector3.zero;
				component7.aimAssist = true;
				GameObject gameObject3 = Object.Instantiate(ricochetSound, currentHit.point, Quaternion.identity);
				gameObject3.SetActive(value: false);
				gameObject2.SetActive(value: false);
				MonoSingleton<DelayedActivationManager>.Instance.Add(gameObject2, 0.1f);
				MonoSingleton<DelayedActivationManager>.Instance.Add(gameObject3, 0.1f);
			}
		}
	}

	private void RicochetAimAssist(GameObject beam, bool aimAtHead = false)
	{
		RaycastHit[] array = Physics.SphereCastAll(beam.transform.position, 5f, beam.transform.forward, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.Enemies));
		if (array == null || array.Length == 0)
		{
			return;
		}
		Vector3 worldPosition = beam.transform.forward * 1000f;
		float num = float.PositiveInfinity;
		GameObject gameObject = null;
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			Coin component;
			bool flag2 = MonoSingleton<CoinList>.Instance.revolverCoinsList.Count > 0 && array[i].transform.TryGetComponent<Coin>(out component) && (!component.shot || component.shotByEnemy);
			if ((!flag || flag2) && (!(array[i].distance > num) || (!flag && flag2)) && (!(array[i].distance < 0.1f) || flag2) && !Physics.Raycast(beam.transform.position, array[i].point - beam.transform.position, array[i].distance, LayerMaskDefaults.Get(LMD.Environment)) && (flag2 || (array[i].transform.TryGetComponent<EnemyIdentifierIdentifier>(out var component2) && (bool)component2.eid && !component2.eid.dead)))
			{
				if (flag2)
				{
					flag = true;
				}
				worldPosition = (flag2 ? array[i].transform.position : array[i].point);
				num = array[i].distance;
				gameObject = array[i].transform.gameObject;
			}
		}
		if ((bool)gameObject)
		{
			if (aimAtHead && !flag && (critDamageOverride != 0f || (beamType == BeamType.Revolver && !strongAlt)) && gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out var component3) && (bool)component3.eid && (bool)component3.eid.weakPoint && !Physics.Raycast(beam.transform.position, component3.eid.weakPoint.transform.position - beam.transform.position, Vector3.Distance(component3.eid.weakPoint.transform.position, beam.transform.position), LayerMaskDefaults.Get(LMD.Environment)))
			{
				worldPosition = component3.eid.weakPoint.transform.position;
			}
			beam.transform.LookAt(worldPosition);
		}
	}
}

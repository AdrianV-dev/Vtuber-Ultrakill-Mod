using System.Collections.Generic;
using UnityEngine;

public class ContinuousBeam : MonoBehaviour
{
	public EnemyTarget target;

	private LineRenderer lr;

	private LayerMask environmentMask;

	private LayerMask hitMask;

	public bool canHitPlayer = true;

	public bool canHitEnemy = true;

	public bool ignoreInvincibility;

	public float beamWidth = 0.35f;

	public bool enemy;

	public EnemyType safeEnemyType;

	public float damage;

	private float playerCooldown;

	private List<EnemyIdentifier> hitEnemies = new List<EnemyIdentifier>();

	private List<float> enemyCooldowns = new List<float>();

	public GameObject impactEffect;

	private void Start()
	{
		lr = GetComponent<LineRenderer>();
		environmentMask = (int)environmentMask | 0x100;
		environmentMask = (int)environmentMask | 0x800;
		environmentMask = (int)environmentMask | 0x1000000;
		hitMask = (int)hitMask | (int)environmentMask;
		hitMask = (int)hitMask | 0x400;
		hitMask = (int)hitMask | 4;
		if (ignoreInvincibility)
		{
			hitMask = (int)hitMask | 0x8000;
		}
	}

	private void Update()
	{
		Vector3 zero = Vector3.zero;
		zero = ((!Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, float.PositiveInfinity, environmentMask)) ? (base.transform.position + base.transform.forward * 999f) : hitInfo.point);
		lr.SetPosition(0, base.transform.position);
		lr.SetPosition(1, zero);
		if ((bool)impactEffect)
		{
			impactEffect.transform.position = zero;
		}
		RaycastHit[] array = Physics.SphereCastAll(base.transform.position + base.transform.forward * beamWidth, beamWidth, base.transform.forward, Vector3.Distance(base.transform.position, zero) - beamWidth, hitMask);
		if (array != null && array.Length != 0)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (canHitPlayer && playerCooldown <= 0f && array[i].collider.gameObject.CompareTag("Player"))
				{
					playerCooldown = 0.5f;
					if (!Physics.Raycast(base.transform.position, array[i].point - base.transform.position, array[i].distance, environmentMask))
					{
						MonoSingleton<NewMovement>.Instance.GetHurt(Mathf.RoundToInt(damage), invincible: true, 1f, explosion: false, instablack: false, 0.35f, ignoreInvincibility: true);
					}
				}
				else if ((array[i].transform.gameObject.layer == 10 || array[i].transform.gameObject.layer == 11) && canHitEnemy)
				{
					EnemyIdentifierIdentifier component = array[i].transform.GetComponent<EnemyIdentifierIdentifier>();
					if (!component || !component.eid || (enemy && (component.eid.enemyType == safeEnemyType || component.eid.immuneToFriendlyFire || EnemyIdentifier.CheckHurtException(safeEnemyType, component.eid.enemyType, target))))
					{
						continue;
					}
					EnemyIdentifier eid = component.eid;
					bool flag = false;
					if (hitEnemies.Contains(eid))
					{
						Debug.Log("hit hit hit");
						flag = true;
					}
					if (!flag || enemyCooldowns[hitEnemies.IndexOf(eid)] <= 0f)
					{
						if (!flag)
						{
							hitEnemies.Add(eid);
							enemyCooldowns.Add(0.5f);
						}
						else
						{
							enemyCooldowns[hitEnemies.IndexOf(eid)] = 0.5f;
						}
						if (enemy)
						{
							eid.hitter = "enemy";
						}
						eid.DeliverDamage(array[i].transform.gameObject, (zero - base.transform.position).normalized * 1000f, array[i].point, damage / 10f, tryForExplode: true);
					}
				}
				else if (array[i].transform.gameObject.layer == 8 || array[i].transform.gameObject.layer == 24)
				{
					Breakable component2 = array[i].transform.GetComponent<Breakable>();
					if ((bool)component2 && !component2.playerOnly && !component2.precisionOnly)
					{
						component2.Break();
					}
					if (array[i].transform.gameObject.TryGetComponent<Bleeder>(out var component3))
					{
						component3.GetHit(array[i].point, GoreType.Small);
					}
				}
			}
		}
		if (playerCooldown > 0f)
		{
			playerCooldown = Mathf.MoveTowards(playerCooldown, 0f, Time.deltaTime);
		}
		if (enemyCooldowns.Count > 0)
		{
			for (int j = 0; j < enemyCooldowns.Count; j++)
			{
				enemyCooldowns[j] = Mathf.MoveTowards(enemyCooldowns[j], 0f, Time.deltaTime);
			}
		}
	}
}

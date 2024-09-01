using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;

public class Magnet : MonoBehaviour
{
	private List<Rigidbody> affectedRbs = new List<Rigidbody>();

	private List<Rigidbody> removeRbs = new List<Rigidbody>();

	private List<EnemyIdentifier> eids = new List<EnemyIdentifier>();

	private List<Rigidbody> eidRbs = new List<Rigidbody>();

	public List<EnemyIdentifier> ignoredEids = new List<EnemyIdentifier>();

	public EnemyIdentifier onEnemy;

	public List<Magnet> connectedMagnets = new List<Magnet>();

	public List<Rigidbody> sawblades = new List<Rigidbody>();

	public List<Rigidbody> rockets = new List<Rigidbody>();

	public List<Rigidbody> chainsaws = new List<Rigidbody>();

	private SphereCollider col;

	public float strength;

	private LayerMask lmask;

	private RaycastHit rhit;

	[SerializeField]
	private float maxWeight = 10f;

	private TimeBomb tb;

	[HideInInspector]
	public float health = 3f;

	private float maxWeightFinal => maxWeight;

	private void Start()
	{
		col = GetComponent<SphereCollider>();
		lmask = (int)lmask | 0x400;
		lmask = (int)lmask | 0x800;
		tb = GetComponentInParent<TimeBomb>();
		col.enabled = false;
		col.enabled = true;
	}

	private void OnEnable()
	{
		MonoSingleton<ObjectTracker>.Instance.AddMagnet(this);
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<ObjectTracker>.Instance)
		{
			MonoSingleton<ObjectTracker>.Instance.RemoveMagnet(this);
		}
	}

	private void OnDestroy()
	{
		Launch();
		if (connectedMagnets.Count > 0)
		{
			for (int num = connectedMagnets.Count - 1; num >= 0; num--)
			{
				if (connectedMagnets[num] != null)
				{
					DisconnectMagnets(connectedMagnets[num]);
				}
			}
		}
		if ((bool)tb && tb.gameObject.activeInHierarchy)
		{
			Object.Destroy(tb.gameObject);
		}
	}

	public void Launch()
	{
		if (eids.Count > 0)
		{
			for (int num = eids.Count - 1; num >= 0; num--)
			{
				if ((bool)eids[num])
				{
					ExitEnemy(eids[num]);
				}
			}
		}
		if (affectedRbs.Count == 0 && sawblades.Count == 0)
		{
			return;
		}
		List<Nail> list = new List<Nail>();
		foreach (Rigidbody sawblade in sawblades)
		{
			if (!(sawblade != null))
			{
				continue;
			}
			sawblade.velocity = (base.transform.position - sawblade.transform.position).normalized * sawblade.velocity.magnitude;
			if (sawblade.TryGetComponent<Nail>(out var component))
			{
				component.MagnetRelease(this);
				if (component.magnets.Count == 0)
				{
					list.Add(component);
				}
			}
		}
		foreach (Rigidbody affectedRb in affectedRbs)
		{
			if (!(affectedRb != null))
			{
				continue;
			}
			affectedRb.velocity = Vector3.zero;
			if (Physics.SphereCast(new Ray(affectedRb.transform.position, affectedRb.transform.position - base.transform.position), 5f, out rhit, 100f, lmask))
			{
				affectedRb.AddForce((rhit.point - affectedRb.transform.position).normalized * strength * 10f);
			}
			else
			{
				affectedRb.AddForce((base.transform.position - affectedRb.transform.position).normalized * strength * -10f);
			}
			if (affectedRb.TryGetComponent<Nail>(out var component2))
			{
				component2.MagnetRelease(this);
				if (component2.magnets.Count == 0)
				{
					affectedRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
					list.Add(component2);
				}
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		GameObject obj = new GameObject("NailBurstController");
		NailBurstController nailBurstController = obj.AddComponent<NailBurstController>();
		nailBurstController.nails = new List<Nail>(list);
		obj.AddComponent<RemoveOnTime>().time = 5f;
		foreach (Nail item in list)
		{
			item.nbc = nailBurstController;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		Magnet component6;
		if (other.gameObject.layer == 14 && other.gameObject.CompareTag("Metal"))
		{
			Rigidbody attachedRigidbody = other.attachedRigidbody;
			if (!(attachedRigidbody != null) || affectedRbs.Contains(attachedRigidbody))
			{
				return;
			}
			Grenade component2;
			Chainsaw component3;
			if (attachedRigidbody.TryGetComponent<Nail>(out var component))
			{
				component.MagnetCaught(this);
				if (!component.sawblade)
				{
					affectedRbs.Add(attachedRigidbody);
					if (OptionsMenuToManager.simpleNailPhysics)
					{
						attachedRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
					}
				}
				else if (!sawblades.Contains(attachedRigidbody))
				{
					sawblades.Add(attachedRigidbody);
				}
				if (component.chainsaw && Vector3.Distance(base.transform.position, component.transform.position) > 20f)
				{
					component.transform.position = Vector3.MoveTowards(component.transform.position, base.transform.position, Vector3.Distance(base.transform.position, component.transform.position) - 20f);
				}
			}
			else if (attachedRigidbody.TryGetComponent<Grenade>(out component2))
			{
				if ((onEnemy != null && !onEnemy.dead) || component2.enemy)
				{
					if (!component2.magnets.Contains(this))
					{
						component2.latestEnemyMagnet = this;
						component2.magnets.Add(this);
					}
					if (!rockets.Contains(attachedRigidbody))
					{
						rockets.Add(attachedRigidbody);
					}
				}
			}
			else if (attachedRigidbody.TryGetComponent<Chainsaw>(out component3))
			{
				if (!chainsaws.Contains(attachedRigidbody))
				{
					chainsaws.Add(attachedRigidbody);
				}
			}
			else
			{
				affectedRbs.Add(attachedRigidbody);
			}
		}
		else if (other.gameObject.layer == 12 || other.gameObject.layer == 11)
		{
			EnemyIdentifier component4 = other.gameObject.GetComponent<EnemyIdentifier>();
			if (component4 != null && !component4.bigEnemy && !eids.Contains(component4) && !ignoredEids.Contains(component4))
			{
				Rigidbody component5 = component4.GetComponent<Rigidbody>();
				if (component5 != null)
				{
					component5.mass /= 2f;
					eids.Add(component4);
					eidRbs.Add(component5);
				}
			}
		}
		else if (other.TryGetComponent<Magnet>(out component6) && component6 != this && !connectedMagnets.Contains(component6))
		{
			ConnectMagnets(component6);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == 14 && other.gameObject.CompareTag("Metal"))
		{
			Rigidbody attachedRigidbody = other.attachedRigidbody;
			if (!(attachedRigidbody != null))
			{
				return;
			}
			if (affectedRbs.Contains(attachedRigidbody))
			{
				affectedRbs.Remove(attachedRigidbody);
				if (other.TryGetComponent<Nail>(out var component))
				{
					component.MagnetRelease(this);
					if (component.magnets.Count == 0)
					{
						attachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
					}
				}
			}
			else if (sawblades.Contains(attachedRigidbody))
			{
				if (other.TryGetComponent<Nail>(out var component2))
				{
					component2.MagnetRelease(this);
				}
				sawblades.Remove(attachedRigidbody);
			}
			else if (rockets.Contains(attachedRigidbody))
			{
				if (other.TryGetComponent<Grenade>(out var component3) && component3.magnets.Contains(this))
				{
					component3.magnets.Remove(this);
				}
				rockets.Remove(attachedRigidbody);
			}
			else if (chainsaws.Contains(attachedRigidbody))
			{
				chainsaws.Remove(attachedRigidbody);
			}
		}
		else if (other.gameObject.layer == 12)
		{
			EnemyIdentifier component4 = other.gameObject.GetComponent<EnemyIdentifier>();
			ExitEnemy(component4);
		}
	}

	public void ConnectMagnets(Magnet target)
	{
		if (!target.connectedMagnets.Contains(this))
		{
			target.connectedMagnets.Add(this);
		}
		if (!connectedMagnets.Contains(target))
		{
			connectedMagnets.Add(target);
		}
	}

	public void DisconnectMagnets(Magnet target)
	{
		if (target.connectedMagnets.Contains(this))
		{
			target.connectedMagnets.Remove(this);
		}
		if (connectedMagnets.Contains(target))
		{
			connectedMagnets.Remove(target);
		}
	}

	public void ExitEnemy(EnemyIdentifier eid)
	{
		if (eid != null && eids.Contains(eid))
		{
			int index = eids.IndexOf(eid);
			eids.RemoveAt(index);
			if (eidRbs[index] != null)
			{
				eidRbs[index].mass *= 2f;
			}
			eidRbs.RemoveAt(index);
		}
	}

	private void Update()
	{
		float num = 0f;
		float num2 = strength * Time.deltaTime;
		Vector3 position = base.transform.position;
		foreach (Rigidbody affectedRb in affectedRbs)
		{
			if (affectedRb != null)
			{
				Vector3 position2 = affectedRb.transform.position;
				if (Mathf.Abs(Vector3.Dot(affectedRb.velocity, position - position2)) < 1000f)
				{
					affectedRb.AddForce((position - position2) * ((col.radius - Vector3.Distance(position2, position)) / col.radius * 50f * num2));
					num += affectedRb.mass;
				}
			}
			else
			{
				removeRbs.Add(affectedRb);
			}
		}
		if (chainsaws.Count > 0)
		{
			for (int num3 = chainsaws.Count - 1; num3 >= 0; num3--)
			{
				if (chainsaws[num3] == null)
				{
					chainsaws.RemoveAt(num3);
				}
				else if (Vector3.Distance(base.transform.position, chainsaws[num3].position) < 15f && Vector3.Dot(chainsaws[num3].position - base.transform.position, chainsaws[num3].velocity.normalized) < 0f)
				{
					Vector3 position3 = chainsaws[num3].transform.position;
					if (Mathf.Abs(Vector3.Dot(chainsaws[num3].velocity, position - position3)) < 1000f)
					{
						chainsaws[num3].AddForce((position - position3) * ((col.radius - Vector3.Distance(position3, position)) / col.radius * 50f * num2));
						num += chainsaws[num3].mass;
					}
				}
			}
		}
		foreach (Rigidbody sawblade in sawblades)
		{
			if (sawblade != null)
			{
				num += sawblade.mass;
			}
			else
			{
				removeRbs.Add(sawblade);
			}
		}
		if (removeRbs.Count > 0)
		{
			foreach (Rigidbody removeRb in removeRbs)
			{
				affectedRbs.Remove(removeRb);
			}
			removeRbs.Clear();
		}
		for (int num4 = eids.Count - 1; num4 >= 0; num4--)
		{
			EnemyIdentifier enemyIdentifier = eids[num4];
			Rigidbody rigidbody = eidRbs[num4];
			if (enemyIdentifier != null && rigidbody != null && !ignoredEids.Contains(enemyIdentifier))
			{
				Vector3 position4 = rigidbody.transform.position;
				if (enemyIdentifier.nailsAmount > 0 && !eidRbs[num4].isKinematic)
				{
					enemyIdentifier.useBrakes = false;
					enemyIdentifier.pulledByMagnet = true;
					rigidbody.AddForce((position - position4).normalized * ((col.radius - Vector3.Distance(position4, position)) / col.radius * (float)enemyIdentifier.nailsAmount * 5f * num2));
					num += rigidbody.mass;
				}
			}
			else
			{
				eids.RemoveAt(num4);
				eidRbs.RemoveAt(num4);
			}
		}
		float num5 = maxWeightFinal * (float)(connectedMagnets.Count + 1);
		if (num > num5 && !PauseTimedBombs.Paused)
		{
			Object.Destroy(tb.gameObject);
			return;
		}
		tb.beeperColor = Color.Lerp(Color.green, Color.red, num / num5);
		tb.beeperPitch = num / num5 / 2f + 0.25f;
		tb.beeperSizeMultiplier = num / num5 + 1f;
	}

	public IEnumerator Zap(List<GameObject> alreadyHitObjects, float damage = 1f, GameObject sourceWeapon = null)
	{
		alreadyHitObjects.Add(base.gameObject);
		yield return new WaitForSeconds(0.25f);
		EnemyIdentifier.Zap(base.transform.position, damage, alreadyHitObjects, sourceWeapon);
		DamageMagnet(1f);
	}

	public void DamageMagnet(float damage)
	{
		health -= damage;
		if (health <= 0f)
		{
			if ((bool)base.transform.parent && base.transform.parent.TryGetComponent<Harpoon>(out var component))
			{
				Object.Destroy(component.gameObject);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}

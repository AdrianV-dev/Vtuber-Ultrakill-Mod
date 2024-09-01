using System.Collections.Generic;
using UnityEngine;

public class PhysicalShockwave : MonoBehaviour
{
	public EnemyTarget target;

	public int damage;

	public float speed;

	public float maxSize;

	public float force;

	public bool hasHurtPlayer;

	public bool enemy;

	public bool noDamageToEnemy;

	private List<Collider> hitColliders = new List<Collider>();

	public EnemyType enemyType;

	public GameObject soundEffect;

	[HideInInspector]
	public bool fading;

	private ScaleNFade[] faders;

	private void Start()
	{
		if (soundEffect != null)
		{
			Object.Instantiate(soundEffect, base.transform.position, Quaternion.identity);
		}
		faders = GetComponentsInChildren<ScaleNFade>();
		if (!fading)
		{
			ScaleNFade[] array = faders;
			foreach (ScaleNFade obj in array)
			{
				obj.enabled = false;
				obj.fade = true;
				obj.fadeSpeed = speed / 10f;
			}
		}
	}

	private void Update()
	{
		base.transform.localScale = new Vector3(base.transform.localScale.x + Time.deltaTime * speed, base.transform.localScale.y, base.transform.localScale.z + Time.deltaTime * speed);
		if (!fading && (base.transform.localScale.x > maxSize || base.transform.localScale.z > maxSize))
		{
			fading = true;
			ScaleNFade[] array = faders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			Invoke("GetDestroyed", speed / 10f);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!fading)
		{
			CheckCollision(collision.collider);
		}
	}

	private void OnTriggerEnter(Collider collision)
	{
		if (!fading)
		{
			CheckCollision(collision);
		}
	}

	private void CheckCollision(Collider col)
	{
		Landmine component4;
		if (!hasHurtPlayer && col.gameObject.layer != 15 && col.gameObject.CompareTag("Player"))
		{
			hasHurtPlayer = true;
			if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
			{
				NewMovement instance = MonoSingleton<NewMovement>.Instance;
				instance.GetHurt(damage, invincible: true);
				instance.LaunchFromPoint(instance.transform.position + Vector3.down, 30f, 30f);
			}
			else if (damage == 0)
			{
				MonoSingleton<PlatformerMovement>.Instance.Jump();
			}
			else
			{
				MonoSingleton<PlatformerMovement>.Instance.Explode();
			}
		}
		else if (col.gameObject.layer == 10)
		{
			EnemyIdentifierIdentifier component = col.gameObject.GetComponent<EnemyIdentifierIdentifier>();
			if (!(component != null) || !(component.eid != null) || (enemy && (component.eid.enemyType == enemyType || component.eid.immuneToFriendlyFire || EnemyIdentifier.CheckHurtException(enemyType, component.eid.enemyType, target))))
			{
				return;
			}
			Collider component2 = component.eid.GetComponent<Collider>();
			float multiplier = (float)damage / 10f;
			if (noDamageToEnemy || base.transform.localScale.x > 10f || base.transform.localScale.z > 10f)
			{
				multiplier = 0f;
			}
			if (component2 != null && !hitColliders.Contains(component2) && !component.eid.dead)
			{
				hitColliders.Add(component2);
				if (enemy)
				{
					component.eid.hitter = "enemy";
				}
				else
				{
					component.eid.hitter = "explosion";
				}
				if (component.eid.enemyType == EnemyType.Turret && component.eid.TryGetComponent<Turret>(out var component3) && component3.lodged)
				{
					component3.Unlodge();
				}
				component.eid.DeliverDamage(col.gameObject, Vector3.up * force * 2f, col.transform.position, multiplier, tryForExplode: false);
			}
			else if (component2 != null && component.eid.dead)
			{
				hitColliders.Add(component2);
				component.eid.hitter = "explosion";
				component.eid.DeliverDamage(col.gameObject, Vector3.up * 2000f, col.transform.position, multiplier, tryForExplode: false);
			}
		}
		else if (!enemy && (bool)col.attachedRigidbody && col.attachedRigidbody.TryGetComponent<Landmine>(out component4))
		{
			component4.Activate(1.5f);
		}
	}

	private void GetDestroyed()
	{
		Object.Destroy(base.gameObject);
	}
}

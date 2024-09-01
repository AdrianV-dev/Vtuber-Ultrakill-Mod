using System;
using System.Collections.Generic;
using plog;
using UnityEngine;
using UnityEngine.AI;

namespace Sandbox
{

	public class SandboxEnemy : SandboxSpawnableInstance
	{
		private static readonly plog.Logger Log = new plog.Logger("SandboxEnemy");

		public EnemyIdentifier enemyId;

		public EnemyRadianceConfig radiance;

		private bool lastSpeedBuffState;

		private bool lastDamageBuffState;

		private bool lastHealthBuffState;

		private bool lastKinematicState;

		public override void Awake()
		{
			base.Awake();
			enemyId = GetComponent<EnemyIdentifier>();
			if (enemyId == null)
			{
				enemyId = GetComponentInChildren<EnemyIdentifier>();
			}
			radiance = new EnemyRadianceConfig(enemyId);
		}

		public void RestoreRadiance(EnemyRadianceConfig config)
		{
			radiance = config;
			if (config != null)
			{
				UpdateRadiance();
			}
		}

		public void UpdateRadiance()
		{
			enemyId.radianceTier = radiance.tier;
			if (!lastSpeedBuffState && radiance.speedEnabled)
			{
				enemyId.SpeedBuff(radiance.speedBuff);
			}
			else if (lastSpeedBuffState && !radiance.speedEnabled)
			{
				enemyId.SpeedUnbuff();
			}
			enemyId.speedBuffModifier = radiance.speedBuff;
			if (!lastDamageBuffState && radiance.damageEnabled)
			{
				enemyId.DamageBuff(radiance.damageBuff);
			}
			else if (lastDamageBuffState && !radiance.damageEnabled)
			{
				enemyId.DamageUnbuff();
			}
			enemyId.damageBuffModifier = radiance.damageBuff;
			if (!lastHealthBuffState && radiance.healthEnabled)
			{
				enemyId.HealthBuff(radiance.healthBuff);
			}
			else if (lastHealthBuffState && !radiance.healthEnabled)
			{
				enemyId.HealthUnbuff();
			}
			enemyId.healthBuffModifier = radiance.healthBuff;
			lastSpeedBuffState = radiance.speedEnabled;
			lastDamageBuffState = radiance.damageEnabled;
			lastHealthBuffState = radiance.healthEnabled;
			enemyId.UpdateBuffs();
		}

		private void OnEnable()
		{
			enemyId = GetComponent<EnemyIdentifier>();
			if (!enemyId)
			{
				enemyId = GetComponentInChildren<EnemyIdentifier>();
			}
		}

		public SavedEnemy SaveEnemy()
		{
			if (!enemyId || enemyId.health < 0f || enemyId.dead)
			{
				return null;
			}
			SavedEnemy savedEnemy = new SavedEnemy
			{
				Radiance = radiance
			};
			SavedGeneric saveObject = savedEnemy;
			BaseSave(ref saveObject);
			if (enemyId.originalScale != Vector3.zero)
			{
				savedEnemy.Scale = SavedVector3.One;
			}
			return savedEnemy;
		}

		public override void Pause(bool freeze = true)
		{
			base.Pause(freeze);
			GameObject gameObject = collider.gameObject;
			EnemyIdentifier enemyIdentifier = null;
			if (collider.gameObject.TryGetComponent<EnemyIdentifier>(out var component))
			{
				enemyIdentifier = component;
				enemyIdentifier.enabled = false;
			}
			else
			{
				enemyIdentifier = collider.gameObject.GetComponentInChildren<EnemyIdentifier>();
				if (enemyIdentifier != null)
				{
					enemyIdentifier.enabled = false;
					gameObject = enemyIdentifier.gameObject;
				}
			}
			foreach (Behaviour enemyComponent in GetEnemyComponents(gameObject))
			{
				enemyComponent.enabled = false;
			}
			if (gameObject.TryGetComponent<NavMeshAgent>(out var component2))
			{
				component2.enabled = false;
			}
			if (gameObject.TryGetComponent<Animator>(out var component3))
			{
				component3.enabled = false;
			}
			if (gameObject.TryGetComponent<Rigidbody>(out var component4))
			{
				lastKinematicState = component4.isKinematic;
				if (component4.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic)
				{
					component4.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
				}
				component4.isKinematic = true;
			}
		}

		public override void Resume()
		{
			base.Resume();
			if (collider == null)
			{
				return;
			}
			foreach (Behaviour enemyComponent in GetEnemyComponents(collider.gameObject))
			{
				enemyComponent.enabled = true;
			}
			if (collider.gameObject.TryGetComponent<NavMeshAgent>(out var component))
			{
				component.enabled = true;
			}
			if (collider.gameObject.TryGetComponent<EnemyIdentifier>(out var component2))
			{
				component2.enabled = true;
			}
			if (collider.gameObject.TryGetComponent<Animator>(out var component3))
			{
				component3.enabled = true;
			}
			if (collider.gameObject.TryGetComponent<Rigidbody>(out var component4))
			{
				component4.isKinematic = lastKinematicState;
			}
		}

		private IEnumerable<Component> GetEnemyComponents(GameObject obj)
		{
			foreach (Type type in EnemyTypes.types)
			{
				Component component;
				if (sourceObject.fullEnemyComponent)
				{
					Component[] componentsInChildren = obj.GetComponentsInChildren(type);
					Component[] array = componentsInChildren;
					for (int i = 0; i < array.Length; i++)
					{
						yield return array[i];
					}
				}
				else if (obj.TryGetComponent(type, out component))
				{
					yield return component;
				}
			}
		}

		private void Update()
		{
			if (!(enemyId != null))
			{
				Log.Fine("Destroying sandbox enemy due to missing EnemyIdentifier component.");
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}
}
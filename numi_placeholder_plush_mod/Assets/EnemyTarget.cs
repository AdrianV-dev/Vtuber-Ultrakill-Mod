using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTarget
{
	public bool isPlayer;

	public Transform targetTransform;

	public EnemyIdentifier enemyIdentifier;

	public Rigidbody rigidbody;

	public bool isEnemy
	{
		get
		{
			if (!isPlayer)
			{
				return enemyIdentifier != null;
			}
			return false;
		}
	}

	public Vector3 position
	{
		get
		{
			if (!(enemyIdentifier != null) || !enemyIdentifier.overrideCenter)
			{
				if (!(targetTransform != null))
				{
					return Vector3.zero;
				}
				return targetTransform.position;
			}
			return enemyIdentifier.overrideCenter.position;
		}
	}

	public Vector3 headPosition => headTransform.position;

	public Transform headTransform
	{
		get
		{
			if (!(enemyIdentifier == null) || !isPlayer || MonoSingleton<PlayerTracker>.Instance.playerType != 0)
			{
				return trackedTransform;
			}
			return MonoSingleton<CameraController>.Instance.cam.transform;
		}
	}

	public Transform trackedTransform
	{
		get
		{
			if (!(enemyIdentifier != null) || !enemyIdentifier.overrideCenter)
			{
				return targetTransform;
			}
			return enemyIdentifier.overrideCenter;
		}
	}

	public Vector3 forward => targetTransform.forward;

	public Vector3 right => targetTransform.right;

	public bool isOnGround
	{
		get
		{
			if (isPlayer)
			{
				return MonoSingleton<PlayerTracker>.Instance.GetOnGround();
			}
			return true;
		}
	}

	public bool isValid
	{
		get
		{
			if (targetTransform != null && targetTransform.gameObject.activeInHierarchy)
			{
				if (!(enemyIdentifier == null))
				{
					return !enemyIdentifier.dead;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsTargetTransform(Transform other)
	{
		if (isPlayer)
		{
			return other == MonoSingleton<PlayerTracker>.Instance.GetPlayer().parent;
		}
		return other == targetTransform;
	}

	public EnemyTarget(Transform targetTransform)
	{
		isPlayer = false;
		this.targetTransform = targetTransform;
		enemyIdentifier = this.targetTransform.GetComponent<EnemyIdentifier>();
		rigidbody = this.targetTransform.GetComponent<Rigidbody>();
	}

	public EnemyTarget(EnemyIdentifier otherEnemy)
	{
		isPlayer = false;
		targetTransform = otherEnemy.transform;
		enemyIdentifier = otherEnemy;
		enemyIdentifier = targetTransform.GetComponent<EnemyIdentifier>();
		rigidbody = targetTransform.GetComponent<Rigidbody>();
	}

	public Vector3 GetVelocity()
	{
		if (isPlayer)
		{
			return MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity();
		}
		if (targetTransform == null)
		{
			return Vector3.zero;
		}
		if (targetTransform.TryGetComponent<NavMeshAgent>(out var component) && component.enabled)
		{
			return component.velocity;
		}
		if (rigidbody != null)
		{
			return rigidbody.velocity;
		}
		return Vector3.zero;
	}

	public Vector3 PredictTargetPosition(float time, bool includeGravity = false)
	{
		Vector3 vector = GetVelocity() * time;
		if (rigidbody != null)
		{
			if (includeGravity && ((isEnemy && !rigidbody.isKinematic) || (isPlayer && !MonoSingleton<NewMovement>.Instance.gc.onGround)))
			{
				vector += 0.5f * Physics.gravity * (time * time);
			}
			if (Physics.Raycast(rigidbody.position, vector, out var hitInfo, vector.magnitude, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point;
			}
			else
			{
				vector += rigidbody.position;
			}
		}
		else if ((bool)targetTransform)
		{
			vector += targetTransform.position;
		}
		return vector;
	}

	private EnemyTarget()
	{
		isPlayer = false;
		targetTransform = null;
	}

	public static EnemyTarget TrackPlayer()
	{
		PlayerTracker instance = MonoSingleton<PlayerTracker>.Instance;
		return new EnemyTarget
		{
			isPlayer = true,
			targetTransform = instance.GetPlayer().transform,
			rigidbody = instance.GetRigidbody()
		};
	}

	public static EnemyTarget TrackPlayerIfAllowed()
	{
		if (EnemyIgnorePlayer.Active || BlindEnemies.Blind)
		{
			return null;
		}
		return TrackPlayer();
	}

	public override string ToString()
	{
		return string.Concat(isPlayer ? "Player: " : (isEnemy ? "Enemy: " : "Custom Target: "), targetTransform.name, " (", targetTransform.position, ")");
	}
}

using ULTRAKILL.Cheats;
using UnityEngine;

public class AlwaysLookAtCamera : MonoBehaviour
{
	public Transform overrideTarget;

	public EnemyTarget target;

	[Space]
	[Tooltip("If the target is player (null), use the camera instead of the player head position. Helpful in third-person mode.")]
	public bool preferCameraOverHead;

	[Tooltip("Copies camera's rotation instead of looking at the camera, this will mean the object always appears flat like a sprite.")]
	public bool faceScreenInsteadOfCamera;

	public bool dontRotateIfBlind;

	public float speed;

	public bool easeIn;

	public float maxAngle;

	[Space]
	public bool useXAxis = true;

	public bool useYAxis = true;

	public bool useZAxis = true;

	[Space]
	public Vector3 rotationOffset;

	[Space]
	public float maxXAxisFromParent;

	public float maxYAxisFromParent;

	public float maxZAxisFromParent;

	[Header("Enemy")]
	public EnemyIdentifier eid;

	private int difficulty;

	public bool difficultyVariance;

	private float difficultySpeedMultiplier = 1f;

	private void Start()
	{
		if ((bool)eid && eid.difficultyOverride >= 0)
		{
			difficulty = eid.difficultyOverride;
		}
		else
		{
			difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		}
		UpdateDifficulty();
		EnsureTargetExists();
		SlowUpdate();
	}

	private void EnsureTargetExists()
	{
		if (target == null || !target.isValid || (overrideTarget != null && target.trackedTransform != overrideTarget))
		{
			target = ((overrideTarget == null) ? EnemyTarget.TrackPlayer() : new EnemyTarget(overrideTarget));
		}
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 0.5f);
		EnsureTargetExists();
	}

	private void LateUpdate()
	{
		if (target == null || !target.isValid || (dontRotateIfBlind && BlindEnemies.Blind))
		{
			return;
		}
		float num = speed;
		if ((bool)eid)
		{
			num *= eid.totalSpeedModifier;
		}
		if (difficultyVariance)
		{
			num *= difficultySpeedMultiplier;
		}
		Transform transform = ((preferCameraOverHead && target.isPlayer) ? MonoSingleton<CameraController>.Instance.cam.transform : target.headTransform);
		if (speed == 0f && useXAxis && useYAxis && useZAxis)
		{
			if (faceScreenInsteadOfCamera)
			{
				base.transform.rotation = transform.rotation;
				base.transform.Rotate(Vector3.up * 180f, Space.Self);
			}
			else
			{
				base.transform.LookAt(transform);
			}
		}
		else
		{
			Vector3 position = transform.position;
			if (!useXAxis)
			{
				position.x = base.transform.position.x;
			}
			if (!useYAxis)
			{
				position.y = base.transform.position.y;
			}
			if (!useZAxis)
			{
				position.z = base.transform.position.z;
			}
			Quaternion quaternion = Quaternion.LookRotation(position - base.transform.position, Vector3.up);
			if (maxAngle != 0f && Quaternion.Angle(base.transform.rotation, quaternion) > maxAngle)
			{
				return;
			}
			if (speed == 0f)
			{
				base.transform.rotation = quaternion;
			}
			if (easeIn)
			{
				float num2 = 1f;
				if (difficultyVariance)
				{
					if (difficulty == 1)
					{
						num2 = 0.8f;
					}
					else if (difficulty == 0)
					{
						num2 = 0.5f;
					}
				}
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, Time.deltaTime * num * (Quaternion.Angle(base.transform.rotation, quaternion) * num2));
			}
			else
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, Time.deltaTime * num);
			}
		}
		if (maxXAxisFromParent != 0f)
		{
			base.transform.localRotation = Quaternion.Euler(Mathf.Clamp(base.transform.localRotation.eulerAngles.x, 0f - maxXAxisFromParent, maxXAxisFromParent), base.transform.localRotation.eulerAngles.y, base.transform.localRotation.eulerAngles.z);
		}
		if (maxYAxisFromParent != 0f)
		{
			base.transform.localRotation = Quaternion.Euler(base.transform.localRotation.eulerAngles.x, Mathf.Clamp(base.transform.localRotation.eulerAngles.y, 0f - maxYAxisFromParent, maxYAxisFromParent), base.transform.localRotation.eulerAngles.z);
		}
		if (maxZAxisFromParent != 0f)
		{
			base.transform.localRotation = Quaternion.Euler(base.transform.localRotation.eulerAngles.x, base.transform.localRotation.eulerAngles.y, Mathf.Clamp(base.transform.localRotation.eulerAngles.z, 0f - maxZAxisFromParent, maxZAxisFromParent));
		}
		if (rotationOffset != Vector3.zero)
		{
			base.transform.localRotation = Quaternion.Euler(base.transform.localRotation.eulerAngles + rotationOffset);
		}
	}

	public void ChangeOverrideTarget(EnemyTarget target)
	{
		this.target = target;
		overrideTarget = target.trackedTransform;
	}

	public void ChangeOverrideTarget(Transform target)
	{
		this.target = new EnemyTarget(target);
		overrideTarget = target;
	}

	public void SnapToTarget()
	{
		EnsureTargetExists();
		if (target != null)
		{
			Vector3 headPosition = target.headPosition;
			if (!useXAxis)
			{
				headPosition.x = base.transform.position.x;
			}
			if (!useYAxis)
			{
				headPosition.y = base.transform.position.y;
			}
			if (!useZAxis)
			{
				headPosition.z = base.transform.position.z;
			}
			Quaternion rotation = Quaternion.LookRotation(headPosition - base.transform.position);
			base.transform.rotation = rotation;
		}
	}

	public void ChangeSpeed(float newSpeed)
	{
		speed = newSpeed;
	}

	public void ChangeDifficulty(int newDiff)
	{
		difficulty = newDiff;
		UpdateDifficulty();
	}

	public void UpdateDifficulty()
	{
		if (difficulty == 1)
		{
			difficultySpeedMultiplier = 0.8f;
		}
		else if (difficulty == 0)
		{
			difficultySpeedMultiplier = 0.6f;
		}
	}
}

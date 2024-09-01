using UnityEngine;

public class Spin : MonoBehaviour
{
	public Vector3 spinDirection;

	public float speed;

	public bool inLateUpdate;

	private Vector3 totalRotation;

	public bool notRelative;

	public bool gradual;

	public float gradualSpeed;

	private float currentSpeed;

	public bool off;

	[HideInInspector]
	private AudioSource aud;

	[HideInInspector]
	private float originalPitch;

	[HideInInspector]
	public float pitchMultiplier = 1f;

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
		if (difficultyVariance)
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
		if (gradual)
		{
			aud = GetComponent<AudioSource>();
			if ((bool)aud)
			{
				originalPitch = aud.pitch;
				aud.pitch = currentSpeed;
			}
		}
	}

	private void FixedUpdate()
	{
		if (inLateUpdate)
		{
			return;
		}
		float num = speed * difficultySpeedMultiplier;
		if ((bool)eid)
		{
			num *= eid.totalSpeedModifier;
		}
		if (gradual)
		{
			if (!off && currentSpeed != num)
			{
				currentSpeed = Mathf.MoveTowards(currentSpeed, num, Time.deltaTime * gradualSpeed);
			}
			else if (off && currentSpeed != 0f)
			{
				currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * gradualSpeed);
			}
			if (currentSpeed != 0f)
			{
				if (!notRelative)
				{
					base.transform.Rotate(spinDirection, currentSpeed * Time.deltaTime, Space.Self);
				}
				else
				{
					base.transform.Rotate(spinDirection, currentSpeed * Time.deltaTime, Space.World);
				}
			}
			if ((bool)aud)
			{
				aud.pitch = currentSpeed / num * originalPitch * pitchMultiplier;
			}
		}
		else if (!notRelative)
		{
			base.transform.Rotate(spinDirection, num * Time.deltaTime, Space.Self);
		}
		else
		{
			base.transform.Rotate(spinDirection, num * Time.deltaTime, Space.World);
		}
	}

	private void LateUpdate()
	{
		if (inLateUpdate)
		{
			if (totalRotation == Vector3.zero)
			{
				totalRotation = base.transform.localRotation.eulerAngles;
			}
			float num = speed * difficultySpeedMultiplier;
			if ((bool)eid)
			{
				num *= eid.totalSpeedModifier;
			}
			base.transform.localRotation = Quaternion.Euler(totalRotation);
			base.transform.Rotate(spinDirection, num * Time.deltaTime);
			totalRotation = base.transform.localRotation.eulerAngles;
		}
	}

	public void ChangeState(bool on)
	{
		off = !on;
	}

	public void ChangeSpeed(float newSpeed)
	{
		speed = newSpeed;
	}

	public void ChangeGradualSpeed(float newGradualSpeed)
	{
		gradualSpeed = newGradualSpeed;
	}

	public void ChangePitchMultiplier(float newPitch)
	{
		pitchMultiplier = newPitch;
	}

	public void ChangeSpinDirection(Vector3 newDirection)
	{
		spinDirection = newDirection;
	}
}

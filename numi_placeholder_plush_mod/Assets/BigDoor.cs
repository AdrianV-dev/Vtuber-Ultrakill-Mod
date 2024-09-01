using UnityEngine;

public class BigDoor : MonoBehaviour
{
	public bool open;

	[HideInInspector]
	public bool gotPos;

	public Vector3 openRotation;

	[HideInInspector]
	public Quaternion targetRotation;

	[HideInInspector]
	public Quaternion origRotation;

	public float speed;

	private float tempSpeed;

	public float gradualSpeedMultiplier;

	private CameraController cc;

	public bool screenShake;

	private AudioSource aud;

	public AudioClip openSound;

	public AudioClip closeSound;

	private float origPitch;

	public Light openLight;

	public bool reverseDirection;

	private Door controller;

	public bool playerSpeedMultiplier;

	private void Awake()
	{
		if (!gotPos)
		{
			targetRotation.eulerAngles = base.transform.localRotation.eulerAngles + openRotation;
			origRotation = base.transform.localRotation;
			gotPos = true;
		}
		cc = MonoSingleton<CameraController>.Instance;
		aud = GetComponent<AudioSource>();
		if ((bool)aud)
		{
			origPitch = aud.pitch;
		}
		controller = GetComponentInParent<Door>();
		tempSpeed = speed;
		if (open)
		{
			base.transform.localRotation = targetRotation;
		}
	}

	private void Update()
	{
		if (gradualSpeedMultiplier != 0f)
		{
			if ((open && base.transform.localRotation != targetRotation) || (!open && base.transform.localRotation != origRotation))
			{
				tempSpeed += Time.deltaTime * tempSpeed * gradualSpeedMultiplier;
			}
			else
			{
				tempSpeed = speed;
			}
		}
		if (open && base.transform.localRotation != targetRotation)
		{
			base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, targetRotation, Time.deltaTime * (playerSpeedMultiplier ? Mathf.Max(tempSpeed, tempSpeed * (MonoSingleton<NewMovement>.Instance.rb.velocity.magnitude / 15f)) : tempSpeed));
			if (screenShake)
			{
				cc.CameraShake(0.05f);
			}
			if (base.transform.localRotation == targetRotation)
			{
				if ((bool)aud)
				{
					aud.clip = closeSound;
					aud.loop = false;
					aud.pitch = Random.Range(origPitch - 0.1f, origPitch + 0.1f);
					aud.Play();
				}
				controller?.onFullyOpened?.Invoke();
			}
		}
		else
		{
			if (open || !(base.transform.localRotation != origRotation))
			{
				return;
			}
			base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, origRotation, Time.deltaTime * (playerSpeedMultiplier ? Mathf.Max(tempSpeed, tempSpeed * (MonoSingleton<NewMovement>.Instance.rb.velocity.magnitude / 15f)) : tempSpeed));
			if (screenShake)
			{
				cc.CameraShake(0.05f);
			}
			if (base.transform.localRotation == origRotation)
			{
				if ((bool)aud)
				{
					aud.clip = closeSound;
					aud.loop = false;
					aud.pitch = Random.Range(origPitch - 0.1f, origPitch + 0.1f);
					aud.Play();
				}
				if ((bool)controller && controller.doorType != 0)
				{
					controller.BigDoorClosed();
				}
				if (openLight != null)
				{
					openLight.enabled = false;
				}
			}
		}
	}

	public void Open()
	{
		if (!(base.transform.localRotation != targetRotation))
		{
			return;
		}
		if (!aud)
		{
			aud = GetComponent<AudioSource>();
			origPitch = aud.pitch;
		}
		open = true;
		if ((bool)aud)
		{
			aud.clip = openSound;
			aud.loop = true;
			aud.pitch = Random.Range(origPitch - 0.1f, origPitch + 0.1f);
			aud.Play();
		}
		if (Quaternion.Angle(base.transform.localRotation, origRotation) < 20f)
		{
			if (reverseDirection)
			{
				targetRotation.eulerAngles = origRotation.eulerAngles - openRotation;
			}
			else
			{
				targetRotation.eulerAngles = origRotation.eulerAngles + openRotation;
			}
		}
	}

	public void Close()
	{
		if (base.transform.localRotation != origRotation)
		{
			open = false;
			if ((bool)aud)
			{
				aud.clip = openSound;
				aud.loop = true;
				aud.pitch = Random.Range(origPitch / 2f - 0.1f, origPitch / 2f + 0.1f);
				aud.Play();
			}
		}
	}
}

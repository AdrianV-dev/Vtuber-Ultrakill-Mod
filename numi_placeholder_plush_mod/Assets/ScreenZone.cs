using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScreenZone : MonoBehaviour
{
	protected bool inZone;

	protected bool touchMode;

	private GunControl gc;

	private FistControl pun;

	[SerializeField]
	private AudioSource music;

	private float originalVolume;

	[SerializeField]
	private float angleLimit;

	[SerializeField]
	private Transform angleSourceOverride;

	[Space]
	[SerializeField]
	protected UnityEvent onEnterZone = new UnityEvent();

	[SerializeField]
	protected UnityEvent onExitZone = new UnityEvent();

	private GraphicRaycaster raycaster;

	private void Awake()
	{
		if ((bool)music)
		{
			originalVolume = music.volume;
		}
	}

	private void OnDisable()
	{
		if (base.gameObject.scene.isLoaded && inZone)
		{
			UpdatePlayerState(active: false);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			if (gc == null)
			{
				gc = other.GetComponentInChildren<GunControl>();
			}
			if (pun == null)
			{
				pun = other.GetComponentInChildren<FistControl>();
			}
			inZone = true;
			if (raycaster == null)
			{
				raycaster = GetComponentInChildren<GraphicRaycaster>(includeInactive: true);
			}
			if (raycaster == null)
			{
				raycaster = base.transform.parent.GetComponentInChildren<GraphicRaycaster>(includeInactive: true);
			}
			ControllerPointer.raycaster = raycaster;
			onEnterZone?.Invoke();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			if (gc == null)
			{
				gc = other.GetComponentInChildren<GunControl>();
			}
			if ((bool)raycaster && ControllerPointer.raycaster == raycaster)
			{
				ControllerPointer.raycaster = null;
			}
			onExitZone?.Invoke();
			inZone = false;
			UpdatePlayerState(active: false);
		}
	}

	public virtual void UpdatePlayerState(bool active)
	{
		if (touchMode == active)
		{
			return;
		}
		if (active)
		{
			if (gc != null)
			{
				gc.NoWeapon();
			}
			if (pun != null)
			{
				pun.ShopMode();
			}
		}
		else
		{
			if (gc != null)
			{
				gc.YesWeapon();
			}
			if (pun != null)
			{
				pun.StopShop();
			}
		}
		touchMode = active;
	}

	protected virtual void Update()
	{
		if (music != null)
		{
			if (MonoSingleton<AudioMixerController>.Instance.optionsMusicVolume == 0f)
			{
				music.volume = 0f;
			}
			else
			{
				music.volume = originalVolume;
			}
			if (inZone)
			{
				if (music.pitch < 1f)
				{
					music.pitch = Mathf.MoveTowards(music.pitch, 1f, Time.deltaTime);
				}
			}
			else if (music.pitch > 0f)
			{
				music.pitch = Mathf.MoveTowards(music.pitch, 0f, Time.deltaTime);
			}
		}
		if (inZone)
		{
			float num = Vector3.Angle(MonoSingleton<CameraController>.Instance.transform.forward, (angleSourceOverride == null) ? base.transform.forward : angleSourceOverride.forward);
			UpdatePlayerState(angleLimit == 0f || !(num > angleLimit));
		}
	}
}

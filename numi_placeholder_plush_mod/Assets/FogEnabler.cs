using UnityEngine;

public class FogEnabler : MonoBehaviour
{
	public bool disable;

	public bool oneTime;

	private bool activated;

	private bool colliderless;

	private void Awake()
	{
		if (!TryGetComponent<Collider>(out var _) && !TryGetComponent<Rigidbody>(out var _))
		{
			colliderless = true;
		}
	}

	private void OnEnable()
	{
		if (colliderless)
		{
			Activate();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.transform == MonoSingleton<NewMovement>.Instance.transform)
		{
			Activate();
		}
	}

	private void Activate()
	{
		if (!oneTime || !activated)
		{
			activated = true;
			RenderSettings.fog = !disable;
		}
	}
}

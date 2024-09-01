using UnityEngine;

public class OutdoorsZone : MonoBehaviour
{
	private int hasRequested;

	private void Start()
	{
		if (!MonoSingleton<OutdoorLightMaster>.Instance)
		{
			return;
		}
		Collider component2;
		if (TryGetComponent<Rigidbody>(out var component))
		{
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if ((bool)collider.attachedRigidbody && collider.attachedRigidbody == component)
				{
					MonoSingleton<OutdoorLightMaster>.Instance.outdoorsZones.Add(collider);
				}
			}
		}
		else if (TryGetComponent<Collider>(out component2) && (bool)MonoSingleton<OutdoorLightMaster>.Instance && !MonoSingleton<OutdoorLightMaster>.Instance.outdoorsZones.Contains(component2))
		{
			MonoSingleton<OutdoorLightMaster>.Instance.outdoorsZones.Add(component2);
		}
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<OutdoorLightMaster>.Instance && hasRequested > 0)
		{
			for (int num = hasRequested; num > 0; num--)
			{
				MonoSingleton<OutdoorLightMaster>.Instance.RemoveRequest();
			}
			hasRequested = 0;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)MonoSingleton<OutdoorLightMaster>.Instance && other.gameObject.CompareTag("Player"))
		{
			if (hasRequested == 0)
			{
				MonoSingleton<OutdoorLightMaster>.Instance.AddRequest();
			}
			hasRequested++;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((bool)MonoSingleton<OutdoorLightMaster>.Instance && other.gameObject.CompareTag("Player"))
		{
			if (hasRequested == 1)
			{
				MonoSingleton<OutdoorLightMaster>.Instance.RemoveRequest();
			}
			hasRequested--;
		}
	}
}

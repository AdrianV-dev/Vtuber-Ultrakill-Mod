using System.Collections.Generic;
using UnityEngine;

public class BloodAbsorberChild : MonoBehaviour, IBloodstainReceiver
{
	[HideInInspector]
	public BloodAbsorber bloodGroup;

	private void Start()
	{
		bloodGroup = GetComponentInParent<BloodAbsorber>();
	}

	private void OnEnable()
	{
		BloodsplatterManager instance = MonoSingleton<BloodsplatterManager>.Instance;
		if (instance != null)
		{
			instance.bloodAbsorberChildren++;
		}
	}

	private void OnDisable()
	{
		BloodsplatterManager instance = MonoSingleton<BloodsplatterManager>.Instance;
		if (instance != null)
		{
			instance.bloodAbsorberChildren--;
		}
	}

	public bool HandleBloodstainHit(ref RaycastHit hit)
	{
		bloodGroup.HandleBloodstainHit(ref hit);
		return true;
	}

	public void ProcessWasherSpray(ref List<ParticleCollisionEvent> pEvents, Vector3 position)
	{
		bloodGroup.ProcessWasherSpray(ref pEvents, position);
	}
}

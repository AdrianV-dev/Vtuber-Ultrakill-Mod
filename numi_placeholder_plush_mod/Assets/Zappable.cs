using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zappable : MonoBehaviour
{
	private void OnEnable()
	{
		MonoSingleton<ObjectTracker>.Instance.AddZappable(this);
	}

	private void OnDisable()
	{
		if ((bool)MonoSingleton<ObjectTracker>.Instance)
		{
			MonoSingleton<ObjectTracker>.Instance.RemoveZappable(this);
		}
	}

	public IEnumerator Zap(List<GameObject> alreadyHitObjects, float damage = 1f, GameObject sourceWeapon = null)
	{
		alreadyHitObjects.Add(base.gameObject);
		yield return new WaitForSeconds(0.25f);
		EnemyIdentifier.Zap(base.transform.position, damage, alreadyHitObjects, sourceWeapon);
	}
}

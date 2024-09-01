using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Glass : MonoBehaviour
{
	public bool broken;

	public bool wall;

	private Transform[] glasses;

	public GameObject shatterParticle;

	private int kills;

	private Collider[] cols;

	private List<GameObject> enemies = new List<GameObject>();

	public void Shatter()
	{
		cols = GetComponentsInChildren<Collider>();
		base.gameObject.layer = 17;
		broken = true;
		glasses = base.transform.GetComponentsInChildren<Transform>();
		Transform[] array = glasses;
		foreach (Transform transform in array)
		{
			if (transform.gameObject != base.gameObject)
			{
				Object.Destroy(transform.gameObject);
			}
		}
		Collider[] array2 = cols;
		foreach (Collider collider in array2)
		{
			if (!collider.isTrigger)
			{
				collider.enabled = false;
			}
		}
		foreach (GameObject enemy in enemies)
		{
			if (enemy.TryGetComponent<GroundCheckEnemy>(out var _))
			{
				kills++;
			}
		}
		Invoke("BecomeObstacle", 0.5f);
		Object.Instantiate(shatterParticle, base.transform);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!broken && other.gameObject.layer == 20 && !enemies.Contains(other.gameObject))
		{
			enemies.Add(other.gameObject);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!broken && other.gameObject.layer == 20 && enemies.Contains(other.gameObject))
		{
			enemies.Remove(other.gameObject);
		}
	}

	private void BecomeObstacle()
	{
		NavMeshObstacle component = GetComponent<NavMeshObstacle>();
		if (wall)
		{
			component.carving = false;
			component.enabled = false;
		}
		else
		{
			component.enabled = true;
			Collider[] array = cols;
			foreach (Collider collider in array)
			{
				if (collider != null && !collider.isTrigger)
				{
					collider.enabled = false;
				}
			}
		}
		if (kills >= 3)
		{
			StatsManager instance = MonoSingleton<StatsManager>.Instance;
			if (instance.maxGlassKills < kills)
			{
				instance.maxGlassKills = kills;
			}
		}
		base.enabled = false;
	}
}

using System.Collections.Generic;
using UnityEngine;

public class EnemyCooldowns : MonoSingleton<EnemyCooldowns>
{
	public float virtueCooldown;

	public float ferrymanCooldown;

	public List<Drone> currentVirtues = new List<Drone>();

	public List<Ferryman> ferrymen = new List<Ferryman>();

	private void Start()
	{
		SlowUpdate();
	}

	private void Update()
	{
		if (virtueCooldown > 0f)
		{
			virtueCooldown = Mathf.MoveTowards(virtueCooldown, 0f, Time.deltaTime);
		}
		if (ferrymanCooldown > 0f)
		{
			ferrymanCooldown = Mathf.MoveTowards(ferrymanCooldown, 0f, Time.deltaTime);
		}
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 10f);
		for (int num = currentVirtues.Count - 1; num >= 0; num--)
		{
			if (currentVirtues[num] == null || !currentVirtues[num].gameObject.activeInHierarchy)
			{
				currentVirtues.RemoveAt(num);
			}
		}
		for (int num2 = ferrymen.Count - 1; num2 >= 0; num2--)
		{
			if (ferrymen[num2] == null || !ferrymen[num2].gameObject.activeInHierarchy)
			{
				ferrymen.RemoveAt(num2);
			}
		}
	}

	public void AddVirtue(Drone drn)
	{
		if (currentVirtues.Count <= 0 || !currentVirtues.Contains(drn))
		{
			currentVirtues.Add(drn);
		}
	}

	public void RemoveVirtue(Drone drn)
	{
		if (currentVirtues.Count > 0 && currentVirtues.Contains(drn))
		{
			currentVirtues.Remove(drn);
		}
	}

	public void AddFerryman(Ferryman fm)
	{
		if (ferrymen.Count <= 0 || !ferrymen.Contains(fm))
		{
			ferrymen.Add(fm);
		}
	}

	public void RemoveFerryman(Ferryman fm)
	{
		if (ferrymen.Count > 0 && ferrymen.Contains(fm))
		{
			ferrymen.Remove(fm);
		}
	}
}

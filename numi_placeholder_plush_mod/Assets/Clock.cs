using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
	public Transform hour;

	public Transform minute;

	private void Update()
	{
		float num = DateTime.Now.Hour;
		float num2 = DateTime.Now.Minute;
		float num3 = DateTime.Now.Second;
		hour.localRotation = Quaternion.Euler(0f, (num % 12f / 12f + num2 / 1440f) * 360f, 0f);
		minute.localRotation = Quaternion.Euler(0f, (num2 / 60f + num3 / 3600f) * 360f, 0f);
	}
}

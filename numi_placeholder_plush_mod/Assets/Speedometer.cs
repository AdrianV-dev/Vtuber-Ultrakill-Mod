using System;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
	public TextMeshProUGUI textMesh;

	public Vector3 lastPos;

	public bool classicVersion;

	private TimeSince lastUpdate;

	public RectTransform rect;

	private int type;

	private void Awake()
	{
		PrefsManager instance = MonoSingleton<PrefsManager>.Instance;
		instance.prefChanged = (Action<string, object>)Delegate.Combine(instance.prefChanged, new Action<string, object>(OnPrefChanged));
	}

	private void OnEnable()
	{
		type = MonoSingleton<PrefsManager>.Instance.GetInt("speedometer");
		base.gameObject.SetActive(type > 0);
	}

	private void OnDestroy()
	{
		if ((bool)MonoSingleton<PrefsManager>.Instance)
		{
			PrefsManager instance = MonoSingleton<PrefsManager>.Instance;
			instance.prefChanged = (Action<string, object>)Delegate.Remove(instance.prefChanged, new Action<string, object>(OnPrefChanged));
		}
	}

	private void OnPrefChanged(string id, object value)
	{
		if (id == "speedometer" && value is int num)
		{
			base.gameObject.SetActive(num > 0);
			type = num;
		}
	}

	private void FixedUpdate()
	{
		float num = 0f;
		string arg = "";
		switch (type)
		{
		case 0:
			return;
		case 1:
			num = MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity(trueVelocity: true).magnitude;
			arg = "u";
			break;
		case 2:
			num = Vector3.ProjectOnPlane(MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity(trueVelocity: true), Vector3.up).magnitude;
			arg = "hu";
			break;
		case 3:
			num = Mathf.Abs(MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity(trueVelocity: true).y);
			arg = "vu";
			break;
		}
		if ((float)lastUpdate > 0.064f)
		{
			if (classicVersion)
			{
				textMesh.text = $"{num:0}";
			}
			else
			{
				textMesh.text = $"SPEED: {num:0.00} {arg}/s";
			}
			lastUpdate = 0f;
		}
	}
}

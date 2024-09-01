using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsRestoreDefaultButton : MonoBehaviour
{
	[SerializeField]
	private GameObject buttonContainer;

	public string settingKey;

	[Header("Float")]
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private float valueToPrefMultiplier = 1f;

	[SerializeField]
	private float sliderTolerance = 0.01f;

	[SerializeField]
	private bool integerSlider;

	[Header("Integer")]
	[SerializeField]
	private TMP_Dropdown dropdown;

	[Header("Boolean")]
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private UnityEvent customToggleEvent;

	private float? defaultFloat;

	private int? defaultInt;

	private bool? defaultBool;

	public void RestoreDefault()
	{
		customToggleEvent?.Invoke();
		if (defaultFloat.HasValue)
		{
			slider.value = defaultFloat.Value / valueToPrefMultiplier;
		}
		else if (defaultBool.HasValue)
		{
			toggle.isOn = defaultBool.Value;
		}
		else if (defaultInt.HasValue)
		{
			if (dropdown != null)
			{
				dropdown.value = defaultInt.Value;
			}
			if (integerSlider && slider != null)
			{
				slider.value = defaultInt.Value;
			}
		}
	}

	private void Start()
	{
		if (MonoSingleton<PrefsManager>.Instance.defaultValues.ContainsKey(settingKey))
		{
			object obj = MonoSingleton<PrefsManager>.Instance.defaultValues[settingKey];
			if (obj != null)
			{
				if (!(obj is float num))
				{
					if (!(obj is bool flag))
					{
						if (obj is int num2)
						{
							int value = num2;
							defaultInt = value;
						}
					}
					else
					{
						bool value2 = flag;
						defaultBool = value2;
					}
				}
				else
				{
					float value3 = num;
					defaultFloat = value3;
				}
			}
		}
		if (slider != null)
		{
			if (integerSlider)
			{
				if (!defaultInt.HasValue)
				{
					defaultInt = 0;
				}
			}
			else if (!defaultFloat.HasValue)
			{
				defaultFloat = 0f;
			}
			slider.onValueChanged.AddListener(delegate
			{
				UpdateSelf();
			});
		}
		if (toggle != null)
		{
			if (!defaultBool.HasValue)
			{
				defaultBool = false;
			}
			toggle.onValueChanged.AddListener(delegate
			{
				UpdateSelf();
			});
		}
		if (dropdown != null)
		{
			if (!defaultInt.HasValue)
			{
				defaultInt = 0;
			}
			dropdown.onValueChanged.AddListener(delegate
			{
				UpdateSelf();
			});
		}
		UpdateSelf();
	}

	private void UpdateSelf()
	{
		Debug.Log("UpdateSelf " + settingKey + " DefaultInt: " + defaultInt + " DefaultBool: " + defaultBool + " DefaultFloat: " + defaultFloat);
		if (!defaultInt.HasValue && !defaultBool.HasValue && !defaultFloat.HasValue)
		{
			buttonContainer.SetActive(value: false);
		}
		else if (defaultFloat.HasValue && slider != null)
		{
			if (Math.Abs(defaultFloat.Value - slider.value * valueToPrefMultiplier) < sliderTolerance)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
		else if (defaultBool.HasValue && toggle != null)
		{
			if (defaultBool.Value == toggle.isOn)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
		else if (defaultInt.HasValue && (dropdown != null || (integerSlider && slider != null)))
		{
			int? num = ReadCurrentInt();
			if (!num.HasValue || defaultInt.Value == num)
			{
				buttonContainer.SetActive(value: false);
			}
			else
			{
				buttonContainer.SetActive(value: true);
			}
		}
	}

	private int? ReadCurrentInt()
	{
		if (dropdown != null)
		{
			return dropdown.value;
		}
		if (slider != null && integerSlider)
		{
			return (int)slider.value;
		}
		return null;
	}
}

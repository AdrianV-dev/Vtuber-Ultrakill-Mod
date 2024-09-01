using System;
using System.Linq;
using TMPro;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class HUDOptions : MonoSingleton<HUDOptions>
{
	public Action<bool> alwaysOnTopChanged;

	public TMP_Dropdown hudType;

	private HudController[] hudCons;

	public Slider bgOpacity;

	public Toggle alwaysOnTop;

	public Material hudMaterial;

	public Material hudTextMaterial;

	private Mask[] masks;

	public Toggle weaponIcon;

	public Toggle armIcon;

	public Toggle railcannonMeter;

	public Toggle styleMeter;

	public Toggle styleInfo;

	public TMP_Dropdown speedometer;

	[SerializeField]
	private TMP_Dropdown iconPackDropdown;

	public TMP_Dropdown crossHairType;

	public TMP_Dropdown crossHairColor;

	public TMP_Dropdown crossHairHud;

	public Toggle crossHairHudFade;

	[SerializeField]
	private Toggle powerUpMeter;

	[HideInInspector]
	public static bool powerUpMeterEnabled = true;

	public Crosshair crosshair { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		crosshair = GetComponentInChildren<Crosshair>(includeInactive: true);
	}

	private void Start()
	{
		crossHairType.value = MonoSingleton<PrefsManager>.Instance.GetInt("crossHair");
		crossHairType.RefreshShownValue();
		crossHairColor.value = MonoSingleton<PrefsManager>.Instance.GetInt("crossHairColor");
		crossHairColor.RefreshShownValue();
		crossHairHud.value = MonoSingleton<PrefsManager>.Instance.GetInt("crossHairHud");
		crossHairHud.RefreshShownValue();
		hudType.value = MonoSingleton<PrefsManager>.Instance.GetInt("hudType");
		hudType.RefreshShownValue();
		bgOpacity.value = MonoSingleton<PrefsManager>.Instance.GetFloat("hudBackgroundOpacity");
		hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		for (int i = 0; i < hudCons.Length; i++)
		{
			if (!hudCons[i].altHud)
			{
				masks = hudCons[i].GetComponentsInChildren<Mask>(includeInactive: true);
				break;
			}
		}
		if (MonoSingleton<PrefsManager>.Instance.GetBool("hudAlwaysOnTop"))
		{
			alwaysOnTop.isOn = true;
			AlwaysOnTop(stuff: true);
		}
		else
		{
			AlwaysOnTop(stuff: false);
		}
		weaponIcon.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("weaponIcons");
		armIcon.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("armIcons");
		railcannonMeter.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("railcannonMeter");
		styleMeter.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("styleMeter");
		styleInfo.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("styleInfo");
		crossHairHudFade.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("crossHairHudFade");
		powerUpMeter.isOn = MonoSingleton<PrefsManager>.Instance.GetBool("powerUpMeter");
		speedometer.value = MonoSingleton<PrefsManager>.Instance.GetInt("speedometer");
		speedometer.RefreshShownValue();
		iconPackDropdown.options = (from p in MonoSingleton<IconManager>.Instance.AvailableIconPacks()
			select new TMP_Dropdown.OptionData(p)).ToList();
		iconPackDropdown.SetValueWithoutNotify(MonoSingleton<IconManager>.Instance.CurrentIconPackId);
	}

	public void SetIconPack(int packId)
	{
		MonoSingleton<IconManager>.Instance.SetIconPack(packId);
		MonoSingleton<IconManager>.Instance.Reload();
	}

	public void CrossHairType(int stuff)
	{
		if (crosshair == null)
		{
			crosshair = GetComponentInChildren<Crosshair>();
		}
		MonoSingleton<PrefsManager>.Instance.SetInt("crossHair", stuff);
		if (crosshair != null)
		{
			crosshair.CheckCrossHair();
		}
	}

	public void CrossHairColor(int stuff)
	{
		if (crosshair == null)
		{
			crosshair = GetComponentInChildren<Crosshair>();
		}
		MonoSingleton<PrefsManager>.Instance.SetInt("crossHairColor", stuff);
		if (crosshair != null)
		{
			crosshair.CheckCrossHair();
		}
	}

	public void CrossHairHud(int stuff)
	{
		if (crosshair == null)
		{
			crosshair = GetComponentInChildren<Crosshair>();
		}
		MonoSingleton<PrefsManager>.Instance.SetInt("crossHairHud", stuff);
		if (crosshair != null)
		{
			crosshair.CheckCrossHair();
		}
	}

	public void HudType(int stuff)
	{
		if (hudCons == null || hudCons.Length < 4)
		{
			hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		}
		MonoSingleton<PrefsManager>.Instance.SetInt("hudType", stuff);
		HudController[] array = hudCons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CheckSituation();
		}
		GetComponent<OptionsMenuToManager>().CheckEasterEgg();
	}

	public void HudFade(bool stuff)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool("crossHairHudFade", stuff);
		FadeOutBars[] array = UnityEngine.Object.FindObjectsOfType<FadeOutBars>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CheckState();
		}
	}

	public void PowerUpMeterEnable(bool stuff)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool("powerUpMeter", stuff);
		powerUpMeterEnabled = stuff;
		if ((bool)MonoSingleton<PowerUpMeter>.Instance)
		{
			MonoSingleton<PowerUpMeter>.Instance.UpdateMeter();
		}
	}

	public void BgOpacity(float stuff)
	{
		if (hudCons == null || hudCons.Length < 4)
		{
			hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		}
		MonoSingleton<PrefsManager>.Instance.SetFloat("hudBackgroundOpacity", stuff);
		HudController[] array = hudCons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetOpacity(stuff);
		}
	}

	public void AlwaysOnTop(bool stuff)
	{
		alwaysOnTopChanged?.Invoke(stuff);
		MonoSingleton<PrefsManager>.Instance.SetBool("hudAlwaysOnTop", stuff);
		if (stuff)
		{
			hudMaterial.SetFloat("_ZTest", 8f);
			hudTextMaterial.SetFloat("_ZTest", 8f);
		}
		else
		{
			hudMaterial.SetFloat("_ZTest", 4f);
			hudTextMaterial.SetFloat("_ZTest", 4f);
		}
		Mask[] array = masks;
		foreach (Mask mask in array)
		{
			if (mask.enabled)
			{
				mask.enabled = false;
				mask.enabled = true;
			}
		}
	}

	public void WeaponIcon(bool stuff)
	{
		if (hudCons == null || hudCons.Length < 4)
		{
			hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		}
		MonoSingleton<PrefsManager>.Instance.SetBool("weaponIcons", stuff);
		if (stuff)
		{
			HudController[] array = hudCons;
			foreach (HudController hudController in array)
			{
				if (!hudController.altHud)
				{
					hudController.weaponIcon.SetActive(value: true);
					hudController.weaponIcon.transform.localPosition = new Vector3(hudController.weaponIcon.transform.localPosition.x, hudController.weaponIcon.transform.localPosition.y, 45f);
					hudController.speedometer.rect.anchoredPosition = new Vector2(-79f, 590f);
				}
				else
				{
					hudController.weaponIcon.SetActive(value: true);
				}
			}
		}
		else
		{
			HudController[] array = hudCons;
			foreach (HudController hudController2 in array)
			{
				if (!hudController2.altHud)
				{
					hudController2.weaponIcon.transform.localPosition = new Vector3(hudController2.weaponIcon.transform.localPosition.x, hudController2.weaponIcon.transform.localPosition.y, -9999f);
					hudController2.speedometer.rect.anchoredPosition = new Vector2(-79f, 190f);
				}
				else
				{
					hudController2.weaponIcon.SetActive(value: false);
				}
			}
		}
		MonoSingleton<RailcannonMeter>.Instance?.CheckStatus();
	}

	public void ArmIcon(bool stuff)
	{
		if (hudCons == null || hudCons.Length < 4)
		{
			hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		}
		MonoSingleton<PrefsManager>.Instance.SetBool("armIcons", stuff);
		HudController[] array;
		if (stuff)
		{
			array = hudCons;
			foreach (HudController hudController in array)
			{
				if (!hudController.altHud)
				{
					hudController.armIcon.transform.localPosition = new Vector3(hudController.armIcon.transform.localPosition.x, hudController.armIcon.transform.localPosition.y, 0f);
				}
				else
				{
					hudController.armIcon.SetActive(value: true);
				}
			}
			return;
		}
		array = hudCons;
		foreach (HudController hudController2 in array)
		{
			if (!hudController2.altHud)
			{
				hudController2.armIcon.transform.localPosition = new Vector3(hudController2.armIcon.transform.localPosition.x, hudController2.armIcon.transform.localPosition.y, -9999f);
			}
			else
			{
				hudController2.armIcon.SetActive(value: false);
			}
		}
	}

	public void RailcannonMeterOption(bool stuff)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool("railcannonMeter", stuff);
		MonoSingleton<RailcannonMeter>.Instance?.CheckStatus();
	}

	public void StyleMeter(bool stuff)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool("styleMeter", stuff);
		SetStyleVisibleTemp(stuff);
	}

	public void StyleInfo(bool stuff)
	{
		MonoSingleton<PrefsManager>.Instance.SetBool("styleInfo", stuff);
		SetStyleVisibleTemp(null, stuff);
	}

	public void SetStyleVisibleTemp(bool? meterVisible = null, bool? infoVisible = null)
	{
		if (HideUI.Active)
		{
			meterVisible = false;
			infoVisible = false;
		}
		else
		{
			if (!meterVisible.HasValue)
			{
				meterVisible = MonoSingleton<PrefsManager>.Instance.GetBool("styleMeter");
			}
			if (!infoVisible.HasValue)
			{
				infoVisible = MonoSingleton<PrefsManager>.Instance.GetBool("styleInfo");
			}
		}
		if (hudCons == null || hudCons.Length < 4)
		{
			hudCons = UnityEngine.Object.FindObjectsOfType<HudController>();
		}
		HudController[] array = hudCons;
		foreach (HudController hudController in array)
		{
			if (!hudController.altHud)
			{
				hudController.styleMeter.transform.localPosition = new Vector3(hudController.styleMeter.transform.localPosition.x, hudController.styleMeter.transform.localPosition.y, (!meterVisible.Value) ? (-9999) : 0);
				hudController.styleInfo.transform.localPosition = new Vector3(hudController.styleInfo.transform.localPosition.x, hudController.styleInfo.transform.localPosition.y, (!infoVisible.Value) ? (-9999) : 0);
				MonoSingleton<StyleHUD>.Instance.GetComponent<AudioSource>().enabled = infoVisible.Value;
			}
		}
	}
}

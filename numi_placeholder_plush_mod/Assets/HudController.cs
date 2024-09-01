using System;
using TMPro;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
	public static HudController Instance;

	public bool altHud;

	public bool colorless;

	private GameObject altHudObj;

	private HUDPos hudpos;

	public GameObject gunCanvas;

	public GameObject weaponIcon;

	public GameObject armIcon;

	public GameObject styleMeter;

	public GameObject styleInfo;

	public Speedometer speedometer;

	[Space]
	public Image[] hudBackgrounds;

	public TMP_Text[] textElements;

	[Space]
	public Material normalTextMaterial;

	public Material overlayTextMaterial;

	private void Awake()
	{
		if (!altHud && !Instance)
		{
			Instance = this;
		}
		if (altHud && altHudObj == null)
		{
			altHudObj = base.transform.GetChild(0).gameObject;
		}
		if (!altHud && hudpos == null)
		{
			hudpos = gunCanvas.GetComponent<HUDPos>();
		}
	}

	private void OnEnable()
	{
		if (!(MonoSingleton<HUDOptions>.Instance == null))
		{
			HUDOptions instance = MonoSingleton<HUDOptions>.Instance;
			instance.alwaysOnTopChanged = (Action<bool>)Delegate.Combine(instance.alwaysOnTopChanged, new Action<bool>(SetAlwaysOnTop));
		}
	}

	private void OnDisable()
	{
		if (!(MonoSingleton<HUDOptions>.Instance == null))
		{
			HUDOptions instance = MonoSingleton<HUDOptions>.Instance;
			instance.alwaysOnTopChanged = (Action<bool>)Delegate.Remove(instance.alwaysOnTopChanged, new Action<bool>(SetAlwaysOnTop));
		}
	}

	private void Start()
	{
		if (MapInfoBase.InstanceAnyType.hideStockHUD)
		{
			weaponIcon.SetActive(value: false);
			armIcon.SetActive(value: false);
			return;
		}
		CheckSituation();
		if (!MonoSingleton<PrefsManager>.Instance.GetBool("weaponIcons"))
		{
			if (!altHud)
			{
				speedometer.rect.anchoredPosition = new Vector2(-79f, 190f);
				weaponIcon.transform.localPosition = new Vector3(weaponIcon.transform.localPosition.x, weaponIcon.transform.localPosition.y, 45f);
			}
			else
			{
				weaponIcon.SetActive(value: false);
			}
		}
		if (!MonoSingleton<PrefsManager>.Instance.GetBool("armIcons"))
		{
			if (!altHud)
			{
				armIcon.transform.localPosition = new Vector3(armIcon.transform.localPosition.x, armIcon.transform.localPosition.y, 0f);
			}
			else
			{
				armIcon.SetActive(value: false);
			}
		}
		if (!altHud)
		{
			if (!MonoSingleton<PrefsManager>.Instance.GetBool("styleMeter"))
			{
				styleMeter.transform.localPosition = new Vector3(styleMeter.transform.localPosition.x, styleMeter.transform.localPosition.y, -9999f);
			}
			if (!MonoSingleton<PrefsManager>.Instance.GetBool("styleInfo"))
			{
				styleInfo.transform.localPosition = new Vector3(styleInfo.transform.localPosition.x, styleInfo.transform.localPosition.y, -9999f);
				MonoSingleton<StyleHUD>.Instance.GetComponent<AudioSource>().enabled = false;
			}
		}
		float @float = MonoSingleton<PrefsManager>.Instance.GetFloat("hudBackgroundOpacity");
		if (@float != 50f)
		{
			SetOpacity(@float);
		}
	}

	public void CheckSituation()
	{
		if (HideUI.Active)
		{
			if ((bool)gunCanvas)
			{
				gunCanvas.GetComponent<Canvas>().enabled = false;
			}
			if ((bool)altHudObj)
			{
				altHudObj.SetActive(value: false);
			}
			return;
		}
		if (altHud)
		{
			if ((bool)altHudObj)
			{
				if (MonoSingleton<PrefsManager>.Instance.GetInt("hudType") == 2 && !colorless)
				{
					altHudObj.SetActive(value: true);
				}
				else if (MonoSingleton<PrefsManager>.Instance.GetInt("hudType") == 3 && colorless)
				{
					altHudObj.SetActive(value: true);
				}
				else
				{
					altHudObj.SetActive(value: false);
				}
			}
			MonoSingleton<PrefsManager>.Instance.GetBool("speedometer");
			return;
		}
		if (MonoSingleton<PrefsManager>.Instance.GetInt("hudType") != 1)
		{
			if (gunCanvas == null)
			{
				gunCanvas = base.transform.Find("GunCanvas").gameObject;
			}
			if (hudpos == null)
			{
				hudpos = gunCanvas.GetComponent<HUDPos>();
			}
			gunCanvas.transform.localPosition = new Vector3(gunCanvas.transform.localPosition.x, gunCanvas.transform.localPosition.y, -100f);
			gunCanvas.GetComponent<Canvas>().enabled = false;
			if ((bool)hudpos)
			{
				hudpos.active = false;
			}
			return;
		}
		if (gunCanvas == null)
		{
			gunCanvas = base.transform.Find("GunCanvas").gameObject;
		}
		if (hudpos == null)
		{
			hudpos = gunCanvas.GetComponent<HUDPos>();
		}
		gunCanvas.GetComponent<Canvas>().enabled = true;
		gunCanvas.transform.localPosition = new Vector3(gunCanvas.transform.localPosition.x, gunCanvas.transform.localPosition.y, 1f);
		if ((bool)hudpos)
		{
			hudpos.active = true;
			hudpos.CheckPos();
		}
	}

	public void SetOpacity(float amount)
	{
		Image[] array = hudBackgrounds;
		foreach (Image image in array)
		{
			if ((bool)image)
			{
				Color color = image.color;
				color.a = amount / 100f;
				image.color = color;
			}
		}
	}

	public void SetAlwaysOnTop(bool onTop)
	{
		if (textElements != null)
		{
			TMP_Text[] array = textElements;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].fontSharedMaterial = (onTop ? overlayTextMaterial : normalTextMaterial);
			}
		}
	}
}

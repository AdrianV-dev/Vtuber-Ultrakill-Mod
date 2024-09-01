using Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldOptions : MonoBehaviour
{
	[SerializeField]
	private Image borderIcon;

	[SerializeField]
	private TMP_Text borderStatus;

	[SerializeField]
	private TMP_Text buttonText;

	[Space]
	[SerializeField]
	private GameObject border;

	private bool isBorderOn = true;

	public const string BorderEnabledKey = "border_enabled";

	private void Start()
	{
		if (MonoSingleton<MapVarManager>.Instance.GetBool("border_enabled") ?? false)
		{
			SetBorderOn(state: true);
		}
	}

	public void ToggleBorder()
	{
		SetBorderOn(!isBorderOn);
	}

	public void SetBorderOn(bool state)
	{
		isBorderOn = state;
		border.SetActive(state);
		borderIcon.color = (state ? Color.white : new Color(1f, 1f, 1f, 0.3f));
		borderStatus.text = (state ? "ENABLED" : "DISABLED");
		buttonText.text = (state ? "DISABLE" : "ENABLE");
		MonoSingleton<MapVarManager>.Instance.SetBool("border_enabled", state, persistent: true);
	}
}

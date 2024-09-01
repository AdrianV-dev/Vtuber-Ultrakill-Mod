using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlightParent : MonoBehaviour
{
	private Image[] buttons;

	private TMP_Text[] buttonTexts;

	private void Start()
	{
		buttons = GetComponentsInChildren<Image>();
		buttonTexts = new TMP_Text[buttons.Length];
		for (int i = 0; i < buttons.Length; i++)
		{
			buttonTexts[i] = buttons[i].GetComponentInChildren<TMP_Text>();
		}
	}

	public void ChangeButton(Image target)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			if (!(buttons[i] == null))
			{
				buttons[i].fillCenter = buttons[i] == target;
				if (buttonTexts[i] != null)
				{
					buttonTexts[i].color = ((buttons[i] == target) ? Color.black : Color.white);
				}
			}
		}
	}
}

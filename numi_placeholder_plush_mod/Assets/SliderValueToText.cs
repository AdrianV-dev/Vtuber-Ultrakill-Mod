using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueToText : MonoBehaviour
{
	public DecimalType decimalType;

	private string decString;

	private Slider targetSlider;

	private Text targetText;

	private TMP_Text targetTextTMP;

	public string suffix;

	public string ifMax;

	public string ifMin;

	public Color minColor;

	public Color maxColor;

	private Color origColor;

	private Color nullColor;

	private void Start()
	{
		switch (decimalType)
		{
		case DecimalType.Three:
			decString = "F3";
			break;
		case DecimalType.Two:
			decString = "F2";
			break;
		case DecimalType.One:
			decString = "F1";
			break;
		case DecimalType.NoDecimals:
			decString = "F0";
			break;
		}
		targetSlider = GetComponentInParent<Slider>();
		targetTextTMP = GetComponent<TMP_Text>();
		if (targetTextTMP == null)
		{
			targetText = GetComponent<Text>();
		}
		origColor = (targetTextTMP ? targetTextTMP.color : targetText.color);
		nullColor = new Color(0f, 0f, 0f, 0f);
	}

	private void Update()
	{
		string text = "";
		Color color = origColor;
		text = ((ifMax != "" && targetSlider.value == targetSlider.maxValue) ? ifMax : ((!(ifMin != "") || targetSlider.value != targetSlider.minValue) ? (targetSlider.value.ToString(decString) + suffix) : ifMin));
		if (maxColor != nullColor && targetSlider.value == targetSlider.maxValue)
		{
			color = maxColor;
		}
		else if (minColor != nullColor && targetSlider.value == targetSlider.minValue)
		{
			color = minColor;
		}
		if ((bool)targetTextTMP)
		{
			targetTextTMP.text = text;
			targetTextTMP.color = color;
		}
		else
		{
			targetText.text = text;
			targetText.color = color;
		}
	}
}

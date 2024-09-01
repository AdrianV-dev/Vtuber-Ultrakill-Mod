using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameConsole
{
    [Serializable]
    public class FilterButton
    {
    	public TMP_Text text;

    	public Image buttonBackground;

    	public Image miniIndicator;

    	public GameObject checkmark;

    	public bool active = true;

    	public void SetOpacity(float opacity)
    	{
    		Color color = buttonBackground.color;
    		color.a = opacity;
    		buttonBackground.color = color;
    		miniIndicator.color = color;
    	}

    	public void SetCheckmark(bool isChecked)
    	{
    		checkmark.SetActive(isChecked);
    	}
    }
}

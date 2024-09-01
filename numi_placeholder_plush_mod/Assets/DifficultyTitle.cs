using TMPro;
using UnityEngine;

public class DifficultyTitle : MonoBehaviour
{
	public bool lines;

	private TMP_Text txt2;

	private void Start()
	{
		Check();
	}

	private void OnEnable()
	{
		Check();
	}

	private void Check()
	{
		int @int = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		string text = "";
		if (lines)
		{
			text += "-- ";
		}
		switch (@int)
		{
		case 0:
			text += "HARMLESS";
			break;
		case 1:
			text += "LENIENT";
			break;
		case 2:
			text += "STANDARD";
			break;
		case 3:
			text += "VIOLENT";
			break;
		case 4:
			text += "BRUTAL";
			break;
		case 5:
			text += "ULTRAKILL MUST DIE";
			break;
		}
		if (lines)
		{
			text += " --";
		}
		if (!txt2)
		{
			txt2 = GetComponent<TMP_Text>();
		}
		if ((bool)txt2)
		{
			txt2.text = text;
		}
	}
}

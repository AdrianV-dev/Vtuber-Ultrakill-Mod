using System.Collections;
using TMPro;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class LevelNamePopup : MonoSingleton<LevelNamePopup>
{
	public TMP_Text layerText;

	private string layerString;

	public TMP_Text nameText;

	private string nameString;

	private bool activated;

	private bool fadingOut;

	private AudioSource aud;

	private float textTimer;

	private int currentLetter;

	private bool countTime;

	private void Start()
	{
		MapInfoBase instanceAnyType = MapInfoBase.InstanceAnyType;
		if ((bool)instanceAnyType)
		{
			layerString = instanceAnyType.layerName;
			nameString = instanceAnyType.levelName;
		}
		aud = GetComponent<AudioSource>();
		layerText.text = "";
		nameText.text = "";
	}

	private void Update()
	{
		if (countTime)
		{
			textTimer += Time.deltaTime;
		}
		if (fadingOut)
		{
			Color color = layerText.color;
			color.a = Mathf.MoveTowards(color.a, 0f, Time.deltaTime);
			layerText.color = color;
			nameText.color = color;
			if (color.a <= 0f)
			{
				fadingOut = false;
			}
		}
	}

	public void NameAppear()
	{
		if (!activated)
		{
			activated = true;
			StartCoroutine(ShowLayerText());
		}
	}

	private IEnumerator ShowLayerText()
	{
		countTime = true;
		currentLetter = 0;
		aud.Play();
		while (currentLetter <= layerString.Length)
		{
			while (textTimer >= 0.01f && currentLetter <= layerString.Length)
			{
				textTimer -= 0.01f;
				layerText.text = layerString.Substring(0, currentLetter);
				currentLetter++;
			}
			yield return new WaitForSeconds(0.01f);
		}
		countTime = false;
		aud.Stop();
		yield return new WaitForSeconds(0.5f);
		StartCoroutine(ShowNameText());
	}

	private IEnumerator ShowNameText()
	{
		countTime = true;
		currentLetter = 0;
		aud.Play();
		while (currentLetter <= nameString.Length)
		{
			while (textTimer >= 0.01f && currentLetter <= nameString.Length)
			{
				textTimer -= 0.01f;
				nameText.text = nameString.Substring(0, currentLetter);
				currentLetter++;
			}
			yield return new WaitForSeconds(0.01f);
		}
		countTime = false;
		aud.Stop();
		yield return new WaitForSeconds(3f);
		fadingOut = true;
	}
}

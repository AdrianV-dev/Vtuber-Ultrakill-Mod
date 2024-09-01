using TMPro;
using UnityEngine;

public class FlashingText : MonoBehaviour
{
	private TextMeshProUGUI text;

	private Color originalColor;

	public Color flashColor;

	public float fadeTime;

	private float fading = 1f;

	public float delay;

	private float cooldown;

	public bool forcePreciseTiming;

	public AudioSource[] matchToMusic;

	private void Start()
	{
		text = GetComponent<TextMeshProUGUI>();
		originalColor = text.color;
		text.color = flashColor;
		if (forcePreciseTiming)
		{
			Invoke("Flash", fadeTime + delay);
		}
	}

	private void Update()
	{
		if (matchToMusic.Length != 0)
		{
			for (int num = matchToMusic.Length - 1; num >= 0; num--)
			{
				if (matchToMusic[num].isPlaying)
				{
					text.color = Color.Lerp(flashColor, originalColor, matchToMusic[num].time % (fadeTime + delay));
					break;
				}
			}
			return;
		}
		fading = Mathf.MoveTowards(fading, 0f, Time.deltaTime / fadeTime);
		text.color = Color.Lerp(originalColor, flashColor, fading);
		if (fading == 0f)
		{
			if (cooldown != 0f)
			{
				cooldown = Mathf.MoveTowards(cooldown, 0f, Time.deltaTime);
			}
			if (cooldown == 0f)
			{
				fading = 1f;
				cooldown = delay;
			}
		}
	}

	private void Flash()
	{
		fading = 1f;
		cooldown = delay;
		text.color = Color.Lerp(originalColor, flashColor, 1f);
		if (forcePreciseTiming)
		{
			Invoke("Flash", fadeTime + delay);
		}
	}
}

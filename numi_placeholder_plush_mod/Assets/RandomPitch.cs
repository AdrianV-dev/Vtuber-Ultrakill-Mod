using UnityEngine;

public class RandomPitch : MonoBehaviour
{
	public float defaultPitch = 1f;

	public float pitchVariation = 0.1f;

	public bool oneTime = true;

	public bool playOnEnable = true;

	public bool nailgunOverheatFix;

	private bool beenPlayed;

	public AudioSource aud;

	private void Start()
	{
		if (nailgunOverheatFix)
		{
			Activate();
		}
	}

	private void OnEnable()
	{
		if (!nailgunOverheatFix)
		{
			Activate();
		}
	}

	private void Activate()
	{
		if (oneTime && beenPlayed)
		{
			return;
		}
		beenPlayed = true;
		if (!aud)
		{
			aud = GetComponent<AudioSource>();
		}
		if (aud != null)
		{
			if (pitchVariation == 0f)
			{
				aud.pitch = Random.Range(0.8f, 1.2f);
			}
			else
			{
				aud.pitch = Random.Range(defaultPitch - pitchVariation, defaultPitch + pitchVariation);
			}
			if (playOnEnable)
			{
				aud.Play();
			}
		}
	}
}

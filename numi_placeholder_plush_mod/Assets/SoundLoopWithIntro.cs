using UnityEngine;

public class SoundLoopWithIntro : MonoBehaviour
{
	private AudioSource aud;

	[SerializeField]
	private AudioClip intro;

	[SerializeField]
	private AudioClip loop;

	private bool introOver;

	private void Awake()
	{
		aud = GetComponent<AudioSource>();
		aud.clip = intro;
		aud.loop = false;
		aud.Play();
	}

	private void Update()
	{
		if (!introOver && (!aud.isPlaying || aud.time > aud.clip.length - 0.1f))
		{
			introOver = true;
			aud.clip = loop;
			aud.loop = true;
			aud.Play();
		}
	}
}

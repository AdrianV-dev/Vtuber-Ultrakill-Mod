using UnityEngine;

public class FinalDoorOpener : MonoBehaviour
{
	public bool startTimer;

	public bool startMusic;

	private bool opened;

	private bool opening;

	private bool closed;

	private FinalDoor fd;

	private void Awake()
	{
		fd = GetComponentInParent<FinalDoor>();
		if (fd != null)
		{
			fd.Open();
		}
		if (fd != null)
		{
			opening = true;
			Invoke("GoTime", 1f);
		}
		else
		{
			GoTime();
		}
	}

	private void OnEnable()
	{
		if (closed)
		{
			if (fd != null)
			{
				fd.Open();
			}
			if (fd != null)
			{
				Invoke("GoTime", 1f);
			}
			else
			{
				GoTime();
			}
		}
	}

	public void GoTime()
	{
		CancelInvoke("GoTime");
		if (!opened)
		{
			opening = false;
			opened = true;
			if (startTimer)
			{
				MonoSingleton<StatsManager>.Instance.StartTimer();
			}
			if (startMusic)
			{
				MonoSingleton<MusicManager>.Instance.StartMusic();
			}
			if ((bool)MonoSingleton<OutdoorLightMaster>.Instance)
			{
				MonoSingleton<OutdoorLightMaster>.Instance.FirstDoorOpen();
			}
			if ((bool)MonoSingleton<StatsManager>.Instance)
			{
				MonoSingleton<StatsManager>.Instance.levelStarted = true;
			}
		}
	}

	public void Close()
	{
		if (opened || opening)
		{
			closed = true;
			opened = false;
			opening = false;
			CancelInvoke("GoTime");
			if ((bool)fd)
			{
				fd.Close();
			}
		}
	}
}

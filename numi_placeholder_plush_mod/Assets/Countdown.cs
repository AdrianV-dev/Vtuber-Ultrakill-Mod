using TMPro;
using UnityEngine;

public class Countdown : MonoBehaviour
{
	public float countdownLength;

	private float time;

	public TextMeshProUGUI countdownText;

	public float decimalFontSize;

	public BossHealthBar bossbar;

	public bool invertBossBarAmount;

	public bool disableBossBarOnDisable;

	public bool paused;

	public bool resetOnEnable;

	public UltrakillEvent onZero;

	private bool done;

	private void Start()
	{
		if (time == 0f && !done)
		{
			time = countdownLength;
		}
	}

	private void OnEnable()
	{
		ResetTime();
	}

	private void OnDisable()
	{
		if ((bool)bossbar && disableBossBarOnDisable)
		{
			bossbar.secondaryBarValue = 0f;
			bossbar.secondaryBar = false;
		}
	}

	private void Update()
	{
		if (!paused)
		{
			time = Mathf.MoveTowards(time, 0f, Time.deltaTime);
		}
		if (!done && time <= 0f)
		{
			onZero?.Invoke();
			done = true;
		}
		if ((bool)countdownText)
		{
			if (decimalFontSize == 0f)
			{
				countdownText.text = time.ToString("F2");
			}
			else
			{
				int num = Mathf.FloorToInt(time % 1f * 100f);
				countdownText.text = Mathf.FloorToInt(time).ToString() + "<size=" + decimalFontSize + ((num < 10) ? ">.0" : ">.") + num.ToString();
			}
		}
		if ((bool)bossbar)
		{
			bossbar.secondaryBar = true;
			bossbar.secondaryBarValue = (invertBossBarAmount ? ((countdownLength - time) / countdownLength) : (time / countdownLength));
		}
	}

	public void PauseState(bool pause)
	{
		paused = pause;
	}

	public void ChangeTime(float newTime)
	{
		time = newTime;
	}

	public void ResetTime()
	{
		time = countdownLength;
		done = false;
	}
}

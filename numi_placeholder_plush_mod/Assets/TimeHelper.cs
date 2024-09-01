using UnityEngine;

public static class TimeHelper
{
	public static string ConvertSecondsToString(float seconds)
	{
		int num = Mathf.FloorToInt(seconds / 60f);
		return string.Concat(arg2: (seconds % 60f).ToString("00.000"), arg0: num, arg1: ":");
	}
}

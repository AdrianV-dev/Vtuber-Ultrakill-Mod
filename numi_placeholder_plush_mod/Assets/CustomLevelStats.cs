using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class CustomLevelStats : MonoBehaviour
{
	[SerializeField]
	private RankIcon mainRankIcon;

	[SerializeField]
	private TMP_Text secretsText;

	[SerializeField]
	private TMP_Text statsText;

	private const string AccentColor = "orange";

	public void LoadStats(string uuid)
	{
		if (uuid == null || string.IsNullOrEmpty(uuid))
		{
			statsText.text = "Unsupported";
			secretsText.text = string.Empty;
			mainRankIcon.gameObject.SetActive(value: false);
			return;
		}
		statsText.text = "No stats yet";
		Debug.Log("Loading stats for " + uuid);
		RankData customRankData = GameProgressSaver.GetCustomRankData(uuid);
		if (customRankData == null)
		{
			mainRankIcon.SetEmpty();
			secretsText.text = string.Empty;
			return;
		}
		int[] ranks = customRankData.ranks;
		int @int = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		int rank = ranks[@int];
		mainRankIcon.SetRank(rank);
		int secretsAmount = customRankData.secretsAmount;
		int num = customRankData.secretsFound.Count((bool x) => x);
		secretsText.text = $"Secrets\n{num}/{secretsAmount}";
		RankScoreData[] stats = customRankData.stats;
		if (stats != null && stats.Length >= @int && stats[@int] != null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			RankScoreData rankScoreData = stats[@int];
			stringBuilder.AppendLine("Time: <color=orange>" + TimeHelper.ConvertSecondsToString(rankScoreData.time) + "</color>");
			stringBuilder.AppendLine(string.Format("Kills: <color={0}>{1}</color>", "orange", rankScoreData.kills));
			stringBuilder.AppendLine(string.Format("Style: <color={0}>{1}</color>", "orange", rankScoreData.style));
			statsText.text = stringBuilder.ToString();
		}
	}
}

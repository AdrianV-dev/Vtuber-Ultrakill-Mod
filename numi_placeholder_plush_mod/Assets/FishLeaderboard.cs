using System.Text;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

public class FishLeaderboard : MonoBehaviour
{
	[SerializeField]
	private TMP_Text globalText;

	[SerializeField]
	private TMP_Text friendsText;

	private void OnEnable()
	{
		Fetch();
	}

	private async void Fetch()
	{
		LeaderboardEntry[] obj = await MonoSingleton<LeaderboardController>.Instance.GetFishScores(LeaderboardType.Global);
		StringBuilder strBlrd = new StringBuilder();
		strBlrd.AppendLine("<b>GLOBAL</b>");
		int num = 1;
		LeaderboardEntry[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			LeaderboardEntry leaderboardEntry = array[i];
			Friend user = leaderboardEntry.User;
			string arg = user.Name;
			user = leaderboardEntry.User;
			if (user.IsMe)
			{
				strBlrd.Append("<color=orange>");
			}
			string text = $"[{num}] {leaderboardEntry.Score} - {arg}";
			if (text.Length > 25)
			{
				text = text.Substring(0, 25);
			}
			strBlrd.AppendLine(text);
			user = leaderboardEntry.User;
			if (user.IsMe)
			{
				strBlrd.Append("</color>");
			}
			num++;
		}
		globalText.text = strBlrd.ToString();
		LeaderboardEntry[] obj2 = await MonoSingleton<LeaderboardController>.Instance.GetFishScores(LeaderboardType.Friends);
		strBlrd.Clear();
		strBlrd.AppendLine("<b>FRIENDS</b>");
		array = obj2;
		for (int i = 0; i < array.Length; i++)
		{
			LeaderboardEntry leaderboardEntry2 = array[i];
			Friend user = leaderboardEntry2.User;
			string arg2 = user.Name;
			user = leaderboardEntry2.User;
			if (user.IsMe)
			{
				strBlrd.Append("<color=orange>");
			}
			string text2 = $"[{leaderboardEntry2.GlobalRank}] {leaderboardEntry2.Score} - {arg2}";
			if (text2.Length > 25)
			{
				text2 = text2.Substring(0, 25);
			}
			strBlrd.AppendLine(text2);
			user = leaderboardEntry2.User;
			if (user.IsMe)
			{
				strBlrd.Append("</color>");
			}
		}
		friendsText.text = strBlrd.ToString();
	}
}

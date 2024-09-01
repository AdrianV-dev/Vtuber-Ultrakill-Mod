using System;

[Serializable]
public class RankData
{
	public int[] ranks;

	public int secretsAmount;

	public bool[] secretsFound;

	public bool challenge;

	public int levelNumber;

	public bool[] majorAssists;

	public RankScoreData[] stats;

	public RankData(StatsManager sman)
	{
		int @int = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		levelNumber = sman.levelNumber;
		RankData rank = GameProgressSaver.GetRank(returnNull: true);
		if (rank != null)
		{
			ranks = rank.ranks;
			if (rank.majorAssists != null)
			{
				majorAssists = rank.majorAssists;
			}
			else
			{
				majorAssists = new bool[6];
			}
			if (rank.stats != null)
			{
				stats = rank.stats;
			}
			else
			{
				stats = new RankScoreData[6];
			}
			if ((sman.rankScore >= rank.ranks[@int] && (rank.majorAssists == null || (!sman.majorUsed && rank.majorAssists[@int]))) || sman.rankScore > rank.ranks[@int] || rank.levelNumber != levelNumber)
			{
				majorAssists[@int] = sman.majorUsed;
				ranks[@int] = sman.rankScore;
				if (stats[@int] == null)
				{
					stats[@int] = new RankScoreData();
				}
				stats[@int].kills = sman.kills;
				stats[@int].style = sman.stylePoints;
				stats[@int].time = sman.seconds;
			}
			secretsAmount = sman.secretObjects.Length;
			secretsFound = new bool[secretsAmount];
			for (int i = 0; i < secretsAmount && i < rank.secretsFound.Length; i++)
			{
				if (sman.secretObjects[i] == null || rank.secretsFound[i])
				{
					secretsFound[i] = true;
				}
			}
			challenge = rank.challenge;
			return;
		}
		ranks = new int[6];
		stats = new RankScoreData[6];
		if (stats[@int] == null)
		{
			stats[@int] = new RankScoreData();
		}
		majorAssists = new bool[6];
		for (int j = 0; j < ranks.Length; j++)
		{
			ranks[j] = -1;
		}
		ranks[@int] = sman.rankScore;
		majorAssists[@int] = sman.majorUsed;
		stats[@int].kills = sman.kills;
		stats[@int].style = sman.stylePoints;
		stats[@int].time = sman.seconds;
		secretsAmount = sman.secretObjects.Length;
		secretsFound = new bool[secretsAmount];
		for (int k = 0; k < secretsAmount; k++)
		{
			if (sman.secretObjects[k] == null)
			{
				secretsFound[k] = true;
			}
		}
	}
}

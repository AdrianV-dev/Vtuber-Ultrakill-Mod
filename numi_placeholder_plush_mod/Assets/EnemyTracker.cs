using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.None)]
public class EnemyTracker : MonoSingleton<EnemyTracker>
{
	public List<EnemyIdentifier> enemies = new List<EnemyIdentifier>();

	public List<int> enemyRanks = new List<int>();

	public List<Drone> drones = new List<Drone>();

	private void Update()
	{
		if (!Debug.isDebugBuild || !Input.GetKeyDown(KeyCode.F9))
		{
			return;
		}
		foreach (EnemyIdentifier currentEnemy in GetCurrentEnemies())
		{
			currentEnemy.gameObject.SetActive(value: false);
			currentEnemy.gameObject.SetActive(value: true);
		}
	}

	private void OnGUI()
	{
		if (EnemyIdentifierDebug.Active)
		{
			List<EnemyIdentifier> currentEnemies = GetCurrentEnemies();
			GUI.color = Color.white;
			GUIStyle style = new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold
			};
			GUILayout.Label("[Enemy Tracker]", style);
			GUILayout.Space(12f);
			GUILayout.Label("Active Enemies: " + currentEnemies.Count, style);
			for (int i = 0; i < currentEnemies.Count; i++)
			{
				GUILayout.Label("Enemy " + i + ": " + currentEnemies[i].name, style);
				GUILayout.Label("Enemy Type: " + currentEnemies[i].enemyType, style);
				GUILayout.Label("Enemy Rank: " + enemyRanks[i], style);
				GUILayout.Space(12f);
			}
		}
	}

	public List<EnemyIdentifier> GetCurrentEnemies()
	{
		List<EnemyIdentifier> list = new List<EnemyIdentifier>();
		if (enemies != null && enemies.Count > 0)
		{
			for (int num = enemies.Count - 1; num >= 0; num--)
			{
				if (enemies[num].dead || enemies[num] == null || enemies[num].gameObject == null)
				{
					enemies.RemoveAt(num);
					enemyRanks.RemoveAt(num);
				}
				else if (enemies[num].gameObject.activeInHierarchy)
				{
					list.Add(enemies[num]);
				}
			}
		}
		return list;
	}

	public void UpdateIdolsNow()
	{
		foreach (EnemyIdentifier currentEnemy in GetCurrentEnemies())
		{
			if (currentEnemy.enemyType == EnemyType.Idol && currentEnemy.idol != null)
			{
				currentEnemy.idol.PickNewTarget();
			}
		}
	}

	public List<EnemyIdentifier> GetEnemiesOfType(EnemyType type)
	{
		List<EnemyIdentifier> currentEnemies = GetCurrentEnemies();
		if (currentEnemies.Count > 0)
		{
			for (int num = currentEnemies.Count - 1; num >= 0; num--)
			{
				if (currentEnemies[num].enemyType != type)
				{
					currentEnemies.RemoveAt(num);
				}
			}
		}
		return currentEnemies;
	}

	public void AddEnemy(EnemyIdentifier eid)
	{
		if (!enemies.Contains(eid))
		{
			enemies.Add(eid);
			enemyRanks.Add(GetEnemyRank(eid));
		}
	}

	public int GetEnemyRank(EnemyIdentifier eid)
	{
		return eid.enemyType switch
		{
			EnemyType.Cerberus => 3, 
			EnemyType.Drone => 1, 
			EnemyType.Ferryman => 5, 
			EnemyType.Filth => 0, 
			EnemyType.Gabriel => 6, 
			EnemyType.GabrielSecond => 6, 
			EnemyType.Gutterman => 4, 
			EnemyType.Guttertank => 4, 
			EnemyType.HideousMass => 6, 
			EnemyType.MaliciousFace => 3, 
			EnemyType.Mandalore => 5, 
			EnemyType.Mannequin => 2, 
			EnemyType.Mindflayer => 5, 
			EnemyType.Minos => 6, 
			EnemyType.MinosPrime => 7, 
			EnemyType.Minotaur => 6, 
			EnemyType.Puppet => 0, 
			EnemyType.Schism => 1, 
			EnemyType.Sisyphus => 6, 
			EnemyType.SisyphusPrime => 7, 
			EnemyType.Soldier => 1, 
			EnemyType.Stalker => 4, 
			EnemyType.Stray => 0, 
			EnemyType.Streetcleaner => 2, 
			EnemyType.Swordsmachine => 3, 
			EnemyType.Turret => 3, 
			EnemyType.V2 => 6, 
			EnemyType.V2Second => 6, 
			EnemyType.Virtue => 3, 
			EnemyType.Wicked => 6, 
			_ => -1, 
		};
	}
}

using GameConsole.CommandTree;
using plog;
using UnityEngine;

namespace GameConsole.Commands
{

	internal class Buffs : CommandRoot, IConsoleLogger
	{
		public plog.Logger Log { get; } = new plog.Logger("Buffs");


		public override string Name => "Buffs";

		public override string Description => "Modify buffs for enemies";

		public Buffs(Console con)
			: base(con)
		{
		}

		protected override Branch BuildTree(Console con)
		{
			return CommandRoot.Branch("buffs", BoolMenu("forceradiance", () => OptionsManager.forceRadiance, delegate (bool value)
			{
				OptionsManager.forceRadiance = value;
				EnemyIdentifier[] array5 = Object.FindObjectsOfType<EnemyIdentifier>();
				for (int m = 0; m < array5.Length; m++)
				{
					array5[m].UpdateBuffs();
				}
			}, inverted: false, requireCheats: true), BoolMenu("forcesand", () => OptionsManager.forceSand, delegate (bool value)
			{
				OptionsManager.forceSand = value;
				EnemyIdentifier[] array4 = Object.FindObjectsOfType<EnemyIdentifier>();
				for (int l = 0; l < array4.Length; l++)
				{
					array4[l].Sandify();
				}
			}, inverted: false, requireCheats: true), BoolMenu("forcepuppet", () => OptionsManager.forcePuppet, delegate (bool value)
			{
				OptionsManager.forcePuppet = value;
				EnemyIdentifier[] array3 = Object.FindObjectsOfType<EnemyIdentifier>();
				for (int k = 0; k < array3.Length; k++)
				{
					array3[k].PuppetSpawn();
				}
			}, inverted: false, requireCheats: true), BoolMenu("forcebossbars", () => OptionsManager.forceBossBars, delegate (bool value)
			{
				OptionsManager.forceBossBars = value;
				EnemyIdentifier[] array2 = Object.FindObjectsOfType<EnemyIdentifier>();
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].BossBar(value);
				}
			}, inverted: false, requireCheats: true), CommandRoot.Branch("radiancetier", CommandRoot.Leaf("get", delegate
			{
				Log.Info($"Current radiance tier is {OptionsManager.radianceTier}");
			}), CommandRoot.Leaf("set", delegate (float amt)
			{
				Log.Info($"Set current radiance tier to {amt}");
				OptionsManager.radianceTier = amt;
				EnemyIdentifier[] array = Object.FindObjectsOfType<EnemyIdentifier>();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].UpdateBuffs();
				}
			}, requireCheats: true)));
		}
	}
}
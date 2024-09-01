using System;
using System.Collections.Generic;

public static class EnemyTypes
{
	public static HashSet<Type> types = new HashSet<Type>
	{
		typeof(Zombie),
		typeof(ZombieMelee),
		typeof(Stalker),
		typeof(Statue),
		typeof(StatueBoss),
		typeof(Mass),
		typeof(Drone),
		typeof(DroneFlesh),
		typeof(Machine),
		typeof(V2),
		typeof(SpiderBody),
		typeof(Gutterman),
		typeof(Guttertank),
		typeof(Sisyphus),
		typeof(MortarLauncher)
	};

	public static string GetEnemyName(EnemyType type)
	{
		return type switch
		{
			EnemyType.Gabriel => "Gabriel, Judge of Hell", 
			EnemyType.GabrielSecond => "Gabriel, Apostate of Hate", 
			EnemyType.Mandalore => "Mysterious Druid Knight (& Owl)", 
			EnemyType.Sisyphus => "Sisyphean Insurrectionist", 
			EnemyType.Turret => "Sentry", 
			EnemyType.V2Second => "V2", 
			_ => Enum.GetName(typeof(EnemyType), type), 
		};
	}
}

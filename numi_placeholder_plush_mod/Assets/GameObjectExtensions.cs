using System;
using UnityEngine;

public static class GameObjectExtensions
{
	public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
	{
		if (gameObject.TryGetComponent<T>(out var component))
		{
			return component;
		}
		return gameObject.AddComponent<T>();
	}

	public static Component GetOrAddComponent(this GameObject gameObject, Type componentType)
	{
		if (gameObject.TryGetComponent(componentType, out var component))
		{
			return component;
		}
		return gameObject.AddComponent(componentType);
	}
}

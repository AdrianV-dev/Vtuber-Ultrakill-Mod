using System;
using UnityEngine;

public static class ComponentExtensions
{
	public static T GetOrAddComponent<T>(this Component component) where T : Component
	{
		return component.gameObject.GetOrAddComponent<T>();
	}

	public static Component GetOrAddComponent(this Component component, Type componentType)
	{
		return component.gameObject.GetOrAddComponent(componentType);
	}
}

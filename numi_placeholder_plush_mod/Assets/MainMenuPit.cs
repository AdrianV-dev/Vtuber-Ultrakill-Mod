using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class MainMenuPit : MonoSingleton<MainMenuPit>
{
	protected override void Awake()
	{
		if (MonoSingleton<MainMenuPit>.Instance != null && MonoSingleton<MainMenuPit>.Instance != this)
		{
			Object.Destroy(MonoSingleton<MainMenuPit>.Instance.gameObject);
		}
		base.Awake();
	}
}

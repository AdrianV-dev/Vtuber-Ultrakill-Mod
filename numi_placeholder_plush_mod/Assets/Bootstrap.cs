using UnityEngine;
using UnityEngine.AddressableAssets;

public class Bootstrap : MonoBehaviour
{
	private void Start()
	{
		Debug.Log(Addressables.RuntimePath);
		GameBuildSettings instance = GameBuildSettings.GetInstance();
		if (!instance.noTutorial && (!GameProgressSaver.GetTutorial() || !GameProgressSaver.GetIntro()))
		{
			MonoSingleton<PrefsManager>.Instance.SetInt("weapon.arm0", 1);
			SceneHelper.LoadScene("Tutorial", noBlocker: true);
		}
		else if (instance.startScene != null)
		{
			SceneHelper.LoadScene(instance.startScene, noBlocker: true);
		}
		else
		{
			SceneHelper.LoadScene("Intro", noBlocker: true);
		}
	}
}

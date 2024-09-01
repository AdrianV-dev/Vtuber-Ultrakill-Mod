using System.Collections;
using System.Linq;
using Logic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[ConfigureSingleton(SingletonFlags.NoAutoInstance | SingletonFlags.PersistAutoInstance | SingletonFlags.DestroyDuplicates)]
public class SceneHelper : MonoSingleton<SceneHelper>
{
	[SerializeField]
	private AssetReference finalRoomPit;

	[SerializeField]
	private GameObject loadingBlocker;

	[SerializeField]
	private TMP_Text loadingBar;

	[SerializeField]
	private GameObject preloadingBadge;

	[SerializeField]
	private GameObject eventSystem;

	[Space]
	[SerializeField]
	private AudioMixerGroup masterMixer;

	[SerializeField]
	private AudioMixerGroup musicMixer;

	[SerializeField]
	private AudioMixer allSound;

	[SerializeField]
	private AudioMixer goreSound;

	[SerializeField]
	private AudioMixer musicSound;

	[SerializeField]
	private AudioMixer doorSound;

	[SerializeField]
	private AudioMixer unfreezeableSound;

	[Space]
	[SerializeField]
	private EmbeddedSceneInfo embeddedSceneInfo;

	public static bool IsPlayingCustom => GameStateManager.Instance.currentCustomGame != null;

	public static bool IsSceneRankless => MonoSingleton<SceneHelper>.Instance.embeddedSceneInfo.ranklessScenes.Contains(CurrentScene);

	public static int CurrentLevelNumber
	{
		get
		{
			if (!IsPlayingCustom)
			{
				return MonoSingleton<StatsManager>.Instance.levelNumber;
			}
			return GameStateManager.Instance.currentCustomGame.levelNumber;
		}
	}

	public static string CurrentScene { get; private set; }

	public static string LastScene { get; private set; }

	public static string PendingScene { get; private set; }

	protected override void OnEnable()
	{
		base.OnEnable();
		Object.DontDestroyOnLoad(base.gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
		if (string.IsNullOrEmpty(CurrentScene))
		{
			CurrentScene = SceneManager.GetActiveScene().name;
		}
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public bool IsSceneSpecial(string sceneName)
	{
		sceneName = SanitizeLevelPath(sceneName);
		if (embeddedSceneInfo == null)
		{
			return false;
		}
		return embeddedSceneInfo.specialScenes.Contains(sceneName);
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (EventSystem.current != null)
		{
			Object.Destroy(EventSystem.current.gameObject);
		}
		Object.Instantiate(eventSystem);
		if (mode == LoadSceneMode.Single)
		{
			GameStateManager.Instance.ResetGravity();
		}
	}

	public static string SanitizeLevelPath(string scene)
	{
		if (scene.StartsWith("Assets/Scenes/"))
		{
			scene = scene.Substring("Assets/Scenes/".Length);
		}
		if (scene.EndsWith(".unity"))
		{
			scene = scene.Substring(0, scene.Length - ".unity".Length);
		}
		return scene;
	}

	public static void ShowLoadingBlocker()
	{
		MonoSingleton<SceneHelper>.Instance.loadingBlocker.SetActive(value: true);
	}

	public static void DismissBlockers()
	{
		MonoSingleton<SceneHelper>.Instance.loadingBlocker.SetActive(value: false);
		MonoSingleton<SceneHelper>.Instance.loadingBar.gameObject.SetActive(value: false);
	}

	public static void LoadScene(string sceneName, bool noBlocker = false)
	{
		MonoSingleton<SceneHelper>.Instance.StartCoroutine(MonoSingleton<SceneHelper>.Instance.LoadSceneAsync(sceneName, noBlocker));
	}

	private IEnumerator LoadSceneAsync(string sceneName, bool noSplash = false)
	{
		if (PendingScene == null)
		{
			PendingScene = sceneName;
			sceneName = SanitizeLevelPath(sceneName);
			switch (sceneName)
			{
			default:
				_ = sceneName == "Custom Content";
				break;
			case "Main Menu":
			case "Tutorial":
			case "Credits":
			case "Endless":
				break;
			}
			Debug.Log("(LoadSceneAsync) Loading scene " + sceneName);
			loadingBlocker.SetActive(!noSplash);
			yield return null;
			if (CurrentScene != sceneName)
			{
				LastScene = CurrentScene;
			}
			CurrentScene = sceneName;
			if (MonoSingleton<MapVarManager>.Instance != null)
			{
				MonoSingleton<MapVarManager>.Instance.ReloadMapVars();
			}
			yield return Addressables.LoadSceneAsync(sceneName);
			if ((bool)GameStateManager.Instance)
			{
				GameStateManager.Instance.currentCustomGame = null;
			}
			if ((bool)preloadingBadge)
			{
				preloadingBadge.SetActive(value: false);
			}
			if ((bool)loadingBlocker)
			{
				loadingBlocker.SetActive(value: false);
			}
			if ((bool)loadingBar)
			{
				loadingBar.gameObject.SetActive(value: false);
			}
			PendingScene = null;
		}
	}

	public static void RestartScene()
	{
		MonoBehaviour[] array = Object.FindObjectsOfType<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in array)
		{
			if (!(monoBehaviour == null) && !(monoBehaviour.gameObject.scene.name == "DontDestroyOnLoad"))
			{
				monoBehaviour.CancelInvoke();
				monoBehaviour.enabled = false;
			}
		}
		if (string.IsNullOrEmpty(CurrentScene))
		{
			CurrentScene = SceneManager.GetActiveScene().name;
		}
		Addressables.LoadSceneAsync(CurrentScene).WaitForCompletion();
		if (MonoSingleton<MapVarManager>.Instance != null)
		{
			MonoSingleton<MapVarManager>.Instance.ReloadMapVars();
		}
	}

	public static void LoadPreviousScene()
	{
		string text = LastScene;
		if (string.IsNullOrEmpty(text))
		{
			text = "Main Menu";
		}
		LoadScene(text);
	}

	public static void SpawnFinalPitAndFinish()
	{
		FinalRoom finalRoom = Object.FindObjectOfType<FinalRoom>();
		if (finalRoom != null)
		{
			if ((bool)finalRoom.doorOpener)
			{
				finalRoom.doorOpener.SetActive(value: true);
			}
			MonoSingleton<NewMovement>.Instance.transform.position = finalRoom.dropPoint.position;
		}
		else
		{
			GameObject obj = Object.Instantiate(AssetHelper.LoadPrefab(MonoSingleton<SceneHelper>.Instance.finalRoomPit));
			finalRoom = obj.GetComponent<FinalRoom>();
			obj.transform.position = new Vector3(50000f, -1000f, 50000f);
			MonoSingleton<NewMovement>.Instance.transform.position = finalRoom.dropPoint.position;
		}
	}

	public static void SetLoadingSubtext(string text)
	{
		if ((bool)MonoSingleton<SceneHelper>.Instance.loadingBlocker)
		{
			MonoSingleton<SceneHelper>.Instance.loadingBar.gameObject.SetActive(value: true);
			MonoSingleton<SceneHelper>.Instance.loadingBar.text = text;
		}
	}

	public int? GetLevelIndexAfterIntermission(string intermissionScene)
	{
		if (embeddedSceneInfo == null)
		{
			return null;
		}
		IntermissionRelation[] intermissions = embeddedSceneInfo.intermissions;
		for (int i = 0; i < intermissions.Length; i++)
		{
			IntermissionRelation intermissionRelation = intermissions[i];
			if (intermissionRelation.intermissionScene == intermissionScene)
			{
				return intermissionRelation.nextLevelIndex;
			}
		}
		return null;
	}
}

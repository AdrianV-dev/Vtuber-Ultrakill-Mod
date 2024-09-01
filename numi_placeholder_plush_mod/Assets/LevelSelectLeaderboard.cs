using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelSelectLeaderboard : MonoBehaviour
{
	public string layerLevelNumber;

	[SerializeField]
	private GameObject scrollRectContainer;

	[SerializeField]
	private GameObject template;

	[SerializeField]
	private TMP_Text templateUsername;

	[SerializeField]
	private TMP_Text templateTime;

	[SerializeField]
	private TMP_Text templateDifficulty;

	[Space]
	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private Transform container;

	[Space]
	[SerializeField]
	private UnityEngine.UI.Image anyPercentButton;

	[SerializeField]
	private UnityEngine.UI.Image pRankButton;

	[SerializeField]
	private TMP_Text anyPercentLabel;

	[SerializeField]
	private TMP_Text pRankLabel;

	[Space]
	[SerializeField]
	private GameObject loadingPanel;

	[SerializeField]
	private GameObject noItemsPanel;

	[Space]
	[SerializeField]
	private InputActionAsset inputActionAsset;

	[SerializeField]
	private float controllerScrollSpeed = 250f;

	private bool pRankSelected;

	private LevelSelectPanel levelSelect;

	private InputAction scrollSublistAction;

	public void RefreshAnyPercent()
	{
		anyPercentButton.fillCenter = true;
		pRankButton.fillCenter = false;
		anyPercentLabel.color = UnityEngine.Color.black;
		pRankLabel.color = UnityEngine.Color.white;
		container.gameObject.SetActive(value: false);
		scrollRectContainer.SetActive(value: false);
		loadingPanel.SetActive(value: true);
		noItemsPanel.SetActive(value: false);
		ResetEntries();
		pRankSelected = false;
		StopAllCoroutines();
		StartCoroutine(Fetch("Level " + layerLevelNumber));
	}

	public void RefreshPRank()
	{
		anyPercentButton.fillCenter = false;
		pRankButton.fillCenter = true;
		anyPercentLabel.color = UnityEngine.Color.white;
		pRankLabel.color = UnityEngine.Color.black;
		container.gameObject.SetActive(value: false);
		scrollRectContainer.SetActive(value: false);
		loadingPanel.SetActive(value: true);
		noItemsPanel.SetActive(value: false);
		ResetEntries();
		pRankSelected = true;
		StopAllCoroutines();
		StartCoroutine(Fetch("Level " + layerLevelNumber));
	}

	private void OnEnable()
	{
		RefreshAnyPercent();
	}

	private void ResetEntries()
	{
		foreach (Transform item in container)
		{
			if (!(item.gameObject == template))
			{
				Object.Destroy(item.gameObject);
			}
		}
	}

	private bool IsLayerSelected()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		if (currentSelectedGameObject == null)
		{
			return false;
		}
		LevelSelectPanel componentInParent = currentSelectedGameObject.GetComponentInParent<LevelSelectPanel>();
		if (componentInParent == null)
		{
			return false;
		}
		return levelSelect == componentInParent;
	}

	private IEnumerator Fetch(string levelName)
	{
		if (string.IsNullOrEmpty(levelName))
		{
			yield break;
		}
		Task<LeaderboardEntry[]> entryTask = MonoSingleton<LeaderboardController>.Instance.GetLevelScores(levelName, pRankSelected);
		while (!entryTask.IsCompleted)
		{
			yield return null;
		}
		if (entryTask.Result == null)
		{
			yield break;
		}
		LeaderboardEntry[] result = entryTask.Result;
		LeaderboardEntry[] array = result;
		for (int i = 0; i < array.Length; i++)
		{
			LeaderboardEntry leaderboardEntry = array[i];
			TMP_Text tMP_Text = templateUsername;
			Friend user = leaderboardEntry.User;
			tMP_Text.text = user.Name;
			int score = leaderboardEntry.Score;
			int num = score / 60000;
			float num2 = (float)(score - num * 60000) / 1000f;
			templateTime.text = $"{num}:{num2:00.000}";
			int? num3 = null;
			if (leaderboardEntry.Details.Length != 0)
			{
				num3 = leaderboardEntry.Details[0];
			}
			if (LeaderboardProperties.Difficulties.Length <= num3)
			{
				Debug.LogWarning($"Difficulty {num3} is out of range for {levelName}");
				continue;
			}
			templateDifficulty.text = ((!num3.HasValue) ? "UNKNOWN" : LeaderboardProperties.Difficulties[num3.Value].ToUpper());
			GameObject obj = Object.Instantiate(template, container);
			obj.SetActive(value: true);
			SteamController.FetchAvatar(obj.GetComponentInChildren<RawImage>(), leaderboardEntry.User);
		}
		if (result.Length == 0)
		{
			noItemsPanel.SetActive(value: true);
		}
		loadingPanel.SetActive(value: false);
		container.gameObject.SetActive(value: true);
		scrollRectContainer.SetActive(value: true);
	}

	private void Update()
	{
		if (MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad)
		{
			UpdateLeaderboardScroll();
		}
	}

	public void SwitchLeaderboardType(bool pRank)
	{
		if (pRank)
		{
			RefreshPRank();
		}
		else
		{
			RefreshAnyPercent();
		}
	}

	private void UpdateLeaderboardScroll()
	{
		Vector2 vector = scrollSublistAction.ReadValue<Vector2>();
		if (!(vector == Vector2.zero) && IsLayerSelected())
		{
			if (vector.y > 0f)
			{
				scrollRect.verticalNormalizedPosition += 0.01f * Time.deltaTime * controllerScrollSpeed;
			}
			else if (vector.y < 0f)
			{
				scrollRect.verticalNormalizedPosition -= 0.01f * Time.deltaTime * controllerScrollSpeed;
			}
		}
	}

	private void Start()
	{
		scrollSublistAction = inputActionAsset.FindAction("ScrollSublist");
		if (!scrollSublistAction.enabled)
		{
			Debug.Log("Enabling scroll sublist action");
			scrollSublistAction.Enable();
		}
	}

	private void Awake()
	{
		levelSelect = GetComponentInParent<LevelSelectPanel>();
	}
}

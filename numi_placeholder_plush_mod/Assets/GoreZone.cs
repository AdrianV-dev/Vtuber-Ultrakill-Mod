using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoreZone : MonoBehaviour
{
	[Header("Optional")]
	public Transform goreZone;

	public Transform gibZone;

	[HideInInspector]
	public CheckPoint checkpoint;

	[HideInInspector]
	public float maxGore;

	[HideInInspector]
	public List<GameObject> outsideGore = new List<GameObject>();

	private bool endlessMode;

	private int maxGibs;

	public float goreRenderDistance;

	private bool goreUnrendered;

	public List<GameObject> toDestroy = new List<GameObject>();

	public Queue<Bloodsplatter> splatterQueue = new Queue<Bloodsplatter>();

	public Queue<GameObject> stainQueue = new Queue<GameObject>();

	private static GoreZone _globalRootAutomaticGz;

	private BloodsplatterManager bsm;

	public static GoreZone ResolveGoreZone(Transform transform)
	{
		if (!transform.parent)
		{
			if ((bool)_globalRootAutomaticGz)
			{
				transform.SetParent(_globalRootAutomaticGz.transform);
				return _globalRootAutomaticGz;
			}
			GoreZone goreZone = new GameObject("Automated Gore Zone").AddComponent<GoreZone>();
			transform.SetParent(goreZone.transform);
			_globalRootAutomaticGz = goreZone;
			return goreZone;
		}
		GoreZone componentInParent = transform.GetComponentInParent<GoreZone>();
		if ((bool)componentInParent)
		{
			return componentInParent;
		}
		GoreZone componentInChildren = transform.parent.GetComponentInChildren<GoreZone>();
		if ((bool)componentInChildren)
		{
			transform.SetParent(componentInChildren.transform);
			return componentInChildren;
		}
		GoreZone obj = new GameObject("Automated Gore Zone").AddComponent<GoreZone>();
		Transform transform2 = obj.transform;
		transform2.SetParent(transform.parent);
		transform.SetParent(transform2);
		return obj;
	}

	private void Awake()
	{
		if (goreZone == null)
		{
			GameObject gameObject = new GameObject("Gore Zone");
			goreZone = gameObject.transform;
			goreZone.SetParent(base.transform, worldPositionStays: true);
		}
		if (gibZone == null)
		{
			GameObject gameObject2 = new GameObject("Gib Zone");
			gibZone = gameObject2.transform;
			gibZone.SetParent(base.transform, worldPositionStays: true);
		}
	}

	private void Start()
	{
		bsm = MonoSingleton<BloodsplatterManager>.Instance;
		maxGore = MonoSingleton<OptionsManager>.Instance.maxGore;
		endlessMode = MonoSingleton<EndlessGrid>.Instance != null;
		if (endlessMode)
		{
			maxGibs = Mathf.RoundToInt(maxGore / 40f);
		}
		else
		{
			maxGibs = Mathf.RoundToInt(maxGore / 20f);
		}
		SlowUpdate();
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 1f);
		if (bsm.forceGibs)
		{
			maxGore = 3000f;
			maxGibs = 150;
		}
		for (int num = Mathf.FloorToInt((float)goreZone.childCount - maxGore) - 1; num >= 0; num--)
		{
			Transform child = goreZone.GetChild(num);
			GameObject gameObject = child.gameObject;
			if (gameObject.activeSelf && !child.TryGetComponent<Bloodsplatter>(out var _) && gameObject.layer != 1)
			{
				Object.Destroy(child.gameObject);
			}
		}
		for (int num2 = Mathf.FloorToInt(gibZone.childCount - maxGibs) - 1; num2 >= 0; num2--)
		{
			GameObject gameObject2 = gibZone.GetChild(num2).gameObject;
			if (!gameObject2.GetComponentInChildren<Bloodsplatter>())
			{
				Object.Destroy(gameObject2);
			}
		}
		for (int num3 = Mathf.FloorToInt((float)outsideGore.Count - maxGore / 5f) - 1; num3 >= 0; num3--)
		{
			GameObject gameObject3 = outsideGore[num3];
			if (gameObject3 != null)
			{
				if (!gameObject3.GetComponentInChildren<Bloodsplatter>())
				{
					Object.Destroy(gameObject3);
					outsideGore.RemoveAt(num3);
				}
			}
			else
			{
				outsideGore.RemoveAt(num3);
			}
		}
		if (toDestroy.Count > 0)
		{
			StartCoroutine(DestroyNextFrame());
		}
	}

	private IEnumerator DestroyNextFrame()
	{
		yield return null;
		for (int num = toDestroy.Count - 1; num >= 0; num--)
		{
			Object.Destroy(toDestroy[num]);
		}
		toDestroy.Clear();
		yield return null;
	}

	public void SetGoreZone(GameObject gib)
	{
		gib.transform.SetParent(gibZone, worldPositionStays: true);
	}

	private void Update()
	{
		if (goreRenderDistance != 0f)
		{
			CheckRenderDistance();
		}
	}

	private void CheckRenderDistance()
	{
		if (Vector3.Distance(MonoSingleton<CameraController>.Instance.transform.position, base.transform.position) > goreRenderDistance)
		{
			if (!goreUnrendered)
			{
				goreUnrendered = true;
				goreZone.gameObject.SetActive(value: false);
				gibZone.gameObject.SetActive(value: false);
			}
		}
		else if (goreUnrendered)
		{
			goreUnrendered = false;
			goreZone.gameObject.SetActive(value: true);
			gibZone.gameObject.SetActive(value: true);
		}
	}

	public void Combine()
	{
		StaticBatchingUtility.Combine(goreZone.gameObject);
	}

	public void AddDeath()
	{
		checkpoint.restartKills++;
	}

	public void AddKillHitterTarget(int id)
	{
		if ((bool)checkpoint && !checkpoint.succesfulHitters.Contains(id))
		{
			checkpoint.succesfulHitters.Add(id);
		}
	}

	public void ResetGibs()
	{
		for (int num = gibZone.childCount - 1; num > 0; num--)
		{
			Transform child = gibZone.GetChild(num);
			GoreSplatter component2;
			if (child.TryGetComponent<Bloodsplatter>(out var component))
			{
				component.Repool();
			}
			else if (child.TryGetComponent<GoreSplatter>(out component2))
			{
				component2.Repool();
			}
			else
			{
				Object.Destroy(child.gameObject);
			}
		}
	}

	public void UpdateMaxGore(float amount)
	{
		maxGore = amount;
		if (endlessMode)
		{
			maxGibs = Mathf.RoundToInt(maxGore / 40f);
		}
		else
		{
			maxGibs = Mathf.RoundToInt(maxGore / 20f);
		}
	}
}

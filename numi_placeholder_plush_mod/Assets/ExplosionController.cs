using UnityEngine;

public class ExplosionController : MonoBehaviour
{
	public bool forceSimple;

	public bool tryIgniteGasoline = true;

	public GameObject[] toActivate;

	public string playerPref;

	private void Start()
	{
		string text = playerPref;
		if (!(text == "SimFir"))
		{
			if (text == "SimExp")
			{
				playerPref = "simpleExplosions";
			}
		}
		else
		{
			playerPref = "simpleFire";
		}
		if (!MonoSingleton<PrefsManager>.Instance.GetBoolLocal(playerPref) && !forceSimple)
		{
			GameObject[] array = toActivate;
			foreach (GameObject gameObject in array)
			{
				if ((bool)gameObject)
				{
					gameObject.SetActive(value: true);
				}
			}
		}
		if (tryIgniteGasoline)
		{
			MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(base.transform.position);
		}
	}
}

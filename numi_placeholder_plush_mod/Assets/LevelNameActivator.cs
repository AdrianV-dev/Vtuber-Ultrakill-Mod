using UnityEngine;

public class LevelNameActivator : MonoBehaviour
{
	private Collider col;

	private bool activateOnCollision;

	private void Start()
	{
		col = GetComponent<Collider>();
		if (col == null || !col.isTrigger)
		{
			GoTime();
		}
		else
		{
			activateOnCollision = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (activateOnCollision && other.gameObject.CompareTag("Player"))
		{
			GoTime();
		}
	}

	private void GoTime()
	{
		MonoSingleton<LevelNamePopup>.Instance.NameAppear();
		Object.Destroy(this);
	}
}

using UnityEngine;

public class Shockwave : MonoBehaviour
{
	public bool groundSlam;

	public float lifeTime;

	private void Start()
	{
		Invoke("TimeToDie", lifeTime);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Breakable"))
		{
			Breakable component = other.gameObject.GetComponent<Breakable>();
			if (component != null && ((component.weak && !component.precisionOnly) || (groundSlam && component.forceGroundSlammable)))
			{
				component.Break();
			}
		}
	}

	private void TimeToDie()
	{
		Object.Destroy(this);
	}
}

using UnityEngine;

public class WeaponIdentifier : MonoBehaviour
{
	public float delay;

	public float speedMultiplier;

	public bool duplicate;

	public Vector3 duplicateOffset;

	private void Start()
	{
		if (speedMultiplier == 0f)
		{
			speedMultiplier = 1f;
		}
	}
}

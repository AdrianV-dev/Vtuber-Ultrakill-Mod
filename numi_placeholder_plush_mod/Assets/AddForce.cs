using UnityEngine;

public class AddForce : MonoBehaviour
{
	private Rigidbody rb;

	private bool valuesSet;

	public Vector3 force;

	public bool relative;

	public bool onEnable;

	public bool oneTime;

	private bool beenActivated;

	private void OnEnable()
	{
		if (onEnable)
		{
			Push();
		}
	}

	private void SetValues()
	{
		if (!valuesSet)
		{
			valuesSet = true;
			rb = GetComponent<Rigidbody>();
		}
	}

	private void Push()
	{
		if (!oneTime || !beenActivated)
		{
			if (!valuesSet)
			{
				SetValues();
			}
			if (relative)
			{
				rb.AddRelativeForce(force, ForceMode.VelocityChange);
			}
			else
			{
				rb.AddForce(force, ForceMode.VelocityChange);
			}
			beenActivated = true;
		}
	}
}

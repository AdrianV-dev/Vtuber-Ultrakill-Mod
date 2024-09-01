using UnityEngine;

public class Lerp : MonoBehaviour
{
	[SerializeField]
	private Vector3 position;

	[SerializeField]
	private Vector3 rotation;

	[SerializeField]
	private float moveSpeed;

	[SerializeField]
	private float rotateSpeed;

	private Quaternion qRot;

	[SerializeField]
	private bool onEnable = true;

	[SerializeField]
	private bool inFixedUpdate;

	[SerializeField]
	private bool inLocalSpace;

	private bool moving;

	[SerializeField]
	private UltrakillEvent onComplete;

	private void Start()
	{
		if (onEnable)
		{
			Activate();
		}
	}

	private void OnEnable()
	{
		if (onEnable)
		{
			Activate();
		}
	}

	private void Update()
	{
		if (moving && !inFixedUpdate)
		{
			Move(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (moving && inFixedUpdate)
		{
			Move(Time.fixedDeltaTime);
		}
	}

	private void Move(float amount)
	{
		if (!inLocalSpace)
		{
			Vector3 vector = Vector3.MoveTowards(base.transform.position, position, moveSpeed * amount);
			Quaternion quaternion = Quaternion.RotateTowards(base.transform.rotation, qRot, rotateSpeed * amount);
			base.transform.SetPositionAndRotation(vector, quaternion);
		}
		else
		{
			base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, position, moveSpeed * amount);
			base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, qRot, rotateSpeed * amount);
		}
		if (base.transform.position == position && base.transform.rotation == qRot)
		{
			moving = false;
			onComplete?.Invoke();
		}
	}

	public void Activate()
	{
		if (!moving)
		{
			qRot = Quaternion.Euler(rotation);
			moving = true;
		}
	}

	public void Skip()
	{
		Activate();
		Move(99999f);
	}
}

using UnityEngine;

public class EndlessScrollingPiece : MonoBehaviour
{
	private Rigidbody rb;

	public Vector3 velocity;

	public float maxDistance;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		if (Vector3.Distance(base.transform.position, base.transform.parent.position) > maxDistance)
		{
			base.transform.position += velocity.normalized * -2f * maxDistance;
		}
		rb.MovePosition(base.transform.position + velocity * Time.fixedDeltaTime);
	}
}

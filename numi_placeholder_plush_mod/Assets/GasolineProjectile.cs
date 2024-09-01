using UnityEngine;

public class GasolineProjectile : MonoBehaviour
{
	[SerializeField]
	private GasolineStain stain;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private SphereCollider col;

	private bool hitSomething;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == 10 || other.gameObject.layer == 11)
		{
			if (!hitSomething && other.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out var component) && (bool)component.eid && !component.eid.dead)
			{
				hitSomething = true;
				component.eid.AddFlammable(0.1f);
				Object.Destroy(base.gameObject);
			}
		}
		else if (other.gameObject.layer == 8 || other.gameObject.layer == 24)
		{
			Vector3 position = base.transform.position;
			Vector3 vector = -rb.velocity;
			Ray ray = new Ray(base.transform.position - rb.velocity.normalized * Mathf.Max(2.5f, rb.velocity.magnitude * Time.fixedDeltaTime), rb.velocity.normalized);
			if (other.Raycast(ray, out var hitInfo, 10f) && (hitInfo.transform.gameObject.layer == 8 || hitInfo.transform.gameObject.layer == 24))
			{
				position = hitInfo.point + hitInfo.normal * 0.1f;
				vector = hitInfo.normal;
				GasolineStain gasolineStain = Object.Instantiate(stain, position, base.transform.rotation);
				Transform obj = gasolineStain.transform;
				obj.forward = vector * -1f;
				obj.Rotate(Vector3.forward * Random.Range(0f, 360f));
				gasolineStain.AttachTo(other);
				Object.Destroy(base.gameObject);
			}
		}
	}
}

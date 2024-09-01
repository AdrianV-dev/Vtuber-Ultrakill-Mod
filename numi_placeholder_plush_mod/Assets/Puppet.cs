using UnityEngine;
using UnityEngine.AI;

public class Puppet : MonoBehaviour
{
	private NavMeshAgent nma;

	[SerializeField]
	private SwingCheck2 sc;

	private Animator anim;

	private EnemyIdentifier eid;

	private Machine mach;

	private Rigidbody rb;

	private bool inAction;

	private bool moving;

	private void Start()
	{
		nma = GetComponent<NavMeshAgent>();
		anim = GetComponent<Animator>();
		eid = GetComponent<EnemyIdentifier>();
		mach = GetComponent<Machine>();
		rb = GetComponent<Rigidbody>();
		SlowUpdate();
	}

	private void SlowUpdate()
	{
		Invoke("SlowUpdate", 0.25f);
		if (eid.target != null && !inAction && nma.enabled && nma.isOnNavMesh && Physics.Raycast(eid.target.position, Vector3.down, out var hitInfo, 120f, LayerMaskDefaults.Get(LMD.Environment)))
		{
			nma.SetDestination(hitInfo.point);
		}
	}

	private void Update()
	{
		Vector3 b = ((eid.target == null) ? (base.transform.position + base.transform.forward) : new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		if (!inAction && eid.target != null)
		{
			if (Vector3.Distance(base.transform.position, b) < 5f)
			{
				Swing();
			}
		}
		else if (moving)
		{
			rb.MovePosition(base.transform.position + base.transform.forward * Time.deltaTime * 15f);
		}
		anim.SetBool("Walking", !inAction && nma.velocity.magnitude > 1.5f);
	}

	private void Swing()
	{
		inAction = true;
		nma.enabled = false;
		anim.Play("Swing", -1, 0f);
		if (eid.target != null)
		{
			base.transform.LookAt(new Vector3(eid.target.position.x, base.transform.position.y, eid.target.position.z));
		}
	}

	private void DamageStart()
	{
		sc.DamageStart();
		moving = true;
	}

	private void DamageStop()
	{
		sc.DamageStop();
		moving = false;
	}

	private void StopAction()
	{
		inAction = false;
		if (mach.gc.onGround)
		{
			nma.enabled = true;
		}
	}
}

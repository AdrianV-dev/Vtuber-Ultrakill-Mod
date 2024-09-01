using UnityEngine;

public class EnemyIdentifierIdentifier : MonoBehaviour
{
	[HideInInspector]
	public EnemyIdentifier eid;

	private bool deactivated;

	private Vector3 startPos;

	public int bloodAbsorberCount;

	private void Awake()
	{
		if (!eid)
		{
			eid = GetComponentInParent<EnemyIdentifier>();
		}
	}

	private void Start()
	{
		startPos = base.transform.position;
		SlowCheck();
	}

	private void DestroyLimb()
	{
		eid.DestroyLimb(base.transform);
	}

	private void DestroyLimbIfNotTouchedBloodAbsorber()
	{
		if (eid == null || !eid.dead)
		{
			return;
		}
		int num = bloodAbsorberCount;
		if (eid == GetComponentInParent<EnemyIdentifier>())
		{
			num = 0;
			EnemyIdentifierIdentifier[] componentsInChildren = eid.GetComponentsInChildren<EnemyIdentifierIdentifier>();
			foreach (EnemyIdentifierIdentifier enemyIdentifierIdentifier in componentsInChildren)
			{
				num += enemyIdentifierIdentifier.bloodAbsorberCount;
			}
		}
		if (num <= 0 && TryGetComponent<Collider>(out var component))
		{
			GibDestroyer.LimbBegone(component);
		}
		else if (StockMapInfo.Instance.removeGibsWithoutAbsorbers)
		{
			Invoke("DestroyLimbIfNotTouchedBloodAbsorber", StockMapInfo.Instance.gibRemoveTime);
		}
	}

	private void SlowCheck()
	{
		if (eid == null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (base.gameObject.activeInHierarchy)
		{
			Vector3 position = base.transform.position;
			if (position.y > 0f)
			{
				position.y = startPos.y;
			}
			if (eid == null || Vector3.Distance(position, startPos) > 9999f || (Vector3.Distance(position, startPos) > 999f && eid.dead))
			{
				deactivated = true;
				MonoSingleton<FireObjectPool>.Instance.RemoveAllFiresFromObject(base.gameObject);
				base.gameObject.SetActive(value: false);
				base.transform.position = new Vector3(-100f, -100f, -100f);
				base.transform.localScale = Vector3.zero;
				if (eid != null && !eid.dead)
				{
					eid.InstaKill();
				}
			}
		}
		if (!deactivated)
		{
			Invoke("SlowCheck", 3f);
		}
	}
}

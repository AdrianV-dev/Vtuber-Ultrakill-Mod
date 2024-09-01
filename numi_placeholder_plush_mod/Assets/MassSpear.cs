using UnityEngine;

public class MassSpear : MonoBehaviour
{
	public EnemyTarget target;

	private LineRenderer lr;

	private Rigidbody rb;

	public bool hittingPlayer;

	public bool hitPlayer;

	public bool beenStopped;

	private bool returning;

	private bool deflected;

	public Transform originPoint;

	private NewMovement nmov;

	public float spearHealth;

	private int difficulty;

	public GameObject breakMetalSmall;

	private AudioSource aud;

	public AudioClip hit;

	public AudioClip stop;

	private Mass mass;

	public float speedMultiplier = 1f;

	public float damageMultiplier = 1f;

	private void Start()
	{
		difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		lr = GetComponentInChildren<LineRenderer>();
		rb = GetComponent<Rigidbody>();
		aud = GetComponent<AudioSource>();
		mass = originPoint.GetComponentInParent<Mass>();
		Invoke("CheckForDistance", 3f / speedMultiplier);
		if (difficulty == 1)
		{
			rb.AddForce(base.transform.forward * 75f * speedMultiplier, ForceMode.VelocityChange);
		}
		if (difficulty == 2)
		{
			rb.AddForce(base.transform.forward * 200f * speedMultiplier, ForceMode.VelocityChange);
		}
		else if (difficulty >= 3)
		{
			rb.AddForce(base.transform.forward * 250f * speedMultiplier, ForceMode.VelocityChange);
		}
	}

	private void OnDisable()
	{
		if (!returning)
		{
			Return();
		}
	}

	private void Update()
	{
		if (originPoint != null && !originPoint.gameObject.activeInHierarchy)
		{
			lr.SetPosition(0, originPoint.position);
			lr.SetPosition(1, lr.transform.position);
			if (returning)
			{
				if (!originPoint || !originPoint.parent || !originPoint.parent.gameObject.activeInHierarchy)
				{
					Object.Destroy(base.gameObject);
					return;
				}
				base.transform.rotation = Quaternion.LookRotation(base.transform.position - originPoint.position, Vector3.up);
				rb.velocity = base.transform.forward * -100f * speedMultiplier;
				if (Vector3.Distance(base.transform.position, originPoint.position) < 1f)
				{
					if (mass != null)
					{
						mass.SpearReturned();
					}
					Object.Destroy(base.gameObject);
				}
			}
			else if (deflected)
			{
				base.transform.LookAt(originPoint.position);
				rb.velocity = base.transform.forward * 100f * speedMultiplier;
				if (!(Vector3.Distance(base.transform.position, originPoint.position) < 1f) || !(mass != null))
				{
					return;
				}
				mass.SpearReturned();
				BloodsplatterManager instance = MonoSingleton<BloodsplatterManager>.Instance;
				EnemyIdentifier component = mass.GetComponent<EnemyIdentifier>();
				Transform child = mass.tailEnd.GetChild(0);
				HurtEnemy(child.gameObject, component);
				for (int i = 0; i < 3; i++)
				{
					GameObject gore = instance.GetGore(GoreType.Head, component);
					gore.transform.position = child.position;
					GoreZone goreZone = GoreZone.ResolveGoreZone(base.transform);
					if ((bool)goreZone)
					{
						gore.transform.SetParent(goreZone.goreZone);
					}
				}
				mass.SpearParried();
				Object.Destroy(base.gameObject);
			}
			else if (hitPlayer && !returning)
			{
				if (nmov.hp <= 0)
				{
					Return();
					Object.Destroy(base.gameObject);
				}
				if (spearHealth > 0f)
				{
					spearHealth = Mathf.MoveTowards(spearHealth, 0f, Time.deltaTime);
				}
				else if (spearHealth <= 0f)
				{
					Return();
				}
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void HurtEnemy(GameObject target, EnemyIdentifier eid = null)
	{
		if (eid == null)
		{
			eid = target.GetComponent<EnemyIdentifier>();
			if (!eid)
			{
				EnemyIdentifierIdentifier component = target.GetComponent<EnemyIdentifierIdentifier>();
				if ((bool)component)
				{
					eid = component.eid;
				}
			}
		}
		if (eid != null && target == null)
		{
			target = eid.gameObject;
		}
		if ((bool)eid)
		{
			eid.DeliverDamage(target, Vector3.zero, originPoint.position, 30f * damageMultiplier, tryForExplode: false);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (beenStopped)
		{
			return;
		}
		if (!hitPlayer && !hittingPlayer && other.gameObject.CompareTag("Player"))
		{
			hittingPlayer = true;
			beenStopped = true;
			rb.isKinematic = true;
			rb.useGravity = false;
			rb.velocity = Vector3.zero;
			base.transform.position = MonoSingleton<CameraController>.Instance.GetDefaultPos();
			Invoke("DelayedPlayerCheck", 0.05f);
		}
		else if (other.gameObject.layer == 8 || other.gameObject.layer == 24)
		{
			beenStopped = true;
			rb.velocity = Vector3.zero;
			rb.useGravity = false;
			base.transform.position += base.transform.forward * 2f;
			Invoke("Return", 2f / speedMultiplier);
			aud.pitch = 1f;
			aud.clip = stop;
			aud.Play();
		}
		else if (target != null && target.isEnemy && (other.gameObject.CompareTag("Head") || other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Limb") || other.gameObject.CompareTag("EndLimb")) && !other.gameObject.CompareTag("Armor"))
		{
			EnemyIdentifierIdentifier componentInParent = other.gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
			EnemyIdentifier enemyIdentifier = null;
			if (componentInParent != null && componentInParent.eid != null)
			{
				enemyIdentifier = componentInParent.eid;
			}
			if (!(enemyIdentifier == null) && !(enemyIdentifier != target.enemyIdentifier) && enemyIdentifier != null)
			{
				HurtEnemy(other.gameObject, enemyIdentifier);
				Return();
			}
		}
	}

	private void DelayedPlayerCheck()
	{
		if (!deflected)
		{
			hittingPlayer = false;
			hitPlayer = true;
			nmov = MonoSingleton<NewMovement>.Instance;
			nmov.GetHurt(Mathf.RoundToInt(25f * damageMultiplier), invincible: true);
			nmov.slowMode = true;
			base.transform.position = nmov.transform.position;
			base.transform.SetParent(nmov.transform, worldPositionStays: true);
			rb.velocity = Vector3.zero;
			rb.useGravity = false;
			rb.isKinematic = true;
			beenStopped = true;
			GetComponent<CapsuleCollider>().radius *= 0.1f;
			aud.pitch = 1f;
			aud.clip = hit;
			aud.Play();
		}
	}

	public void GetHurt(float damage)
	{
		Object.Instantiate(breakMetalSmall, base.transform.position, Quaternion.identity);
		spearHealth -= ((difficulty >= 4) ? (damage / 1.5f) : damage);
	}

	public void Deflected()
	{
		deflected = true;
		rb.isKinematic = false;
		GetComponent<Collider>().enabled = false;
	}

	private void Return()
	{
		if (hitPlayer)
		{
			nmov.slowMode = false;
			base.transform.SetParent(null, worldPositionStays: true);
			rb.isKinematic = false;
		}
		if (base.gameObject.activeInHierarchy)
		{
			aud.clip = stop;
			aud.pitch = 1f;
			aud.Play();
		}
		returning = true;
		beenStopped = true;
	}

	private void CheckForDistance()
	{
		if (!returning && !beenStopped && !hitPlayer && !deflected)
		{
			returning = true;
			beenStopped = true;
			base.transform.position = originPoint.position;
		}
	}
}

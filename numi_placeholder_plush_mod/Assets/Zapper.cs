using System.Collections;
using Train;
using UnityEngine;

public class Zapper : MonoBehaviour
{
	private LineRenderer lr;

	private Rigidbody rb;

	private AudioSource aud;

	[HideInInspector]
	public float damage = 10f;

	[HideInInspector]
	public GameObject sourceWeapon;

	public Transform lineStartTransform;

	public Rigidbody connectedRB;

	private ConfigurableJoint joint;

	[SerializeField]
	private GameObject openProngs;

	[SerializeField]
	private GameObject closedProngs;

	public float maxDistance;

	[HideInInspector]
	public float distance;

	[HideInInspector]
	public float charge;

	[HideInInspector]
	public float breakTimer;

	[HideInInspector]
	public bool raycastBlocked;

	private bool broken;

	public bool attached;

	public EnemyIdentifier attachedEnemy;

	public EnemyIdentifierIdentifier hitLimb;

	[SerializeField]
	private GameObject attachSound;

	[SerializeField]
	private Transform lightningPulseOrb;

	private LineRenderer pulseLine;

	[SerializeField]
	private GameObject zapParticle;

	[SerializeField]
	private AudioSource[] distanceWarningSounds;

	[SerializeField]
	private AudioSource cableSnap;

	[SerializeField]
	private AudioSource boostSound;

	[SerializeField]
	private GameObject breakParticle;

	private void Awake()
	{
		lr = GetComponent<LineRenderer>();
		joint = GetComponent<ConfigurableJoint>();
		rb = GetComponent<Rigidbody>();
		aud = GetComponent<AudioSource>();
		pulseLine = lightningPulseOrb.GetComponent<LineRenderer>();
	}

	private void Start()
	{
		if ((bool)joint)
		{
			joint.connectedBody = connectedRB;
			SoftJointLimit linearLimit = joint.linearLimit;
			linearLimit.limit = maxDistance - 5f;
			joint.linearLimit = linearLimit;
		}
	}

	private void OnDisable()
	{
		if ((bool)attachedEnemy && !broken)
		{
			attachedEnemy.StartCoroutine(ZapNextFrame());
		}
	}

	private IEnumerator ZapNextFrame()
	{
		yield return null;
		Zap();
	}

	private void Update()
	{
		lr.SetPosition(0, lineStartTransform.position);
		lr.SetPosition(1, base.transform.position);
		distance = Vector3.Distance(base.transform.position, connectedRB.position);
		Color color = new Color(0.5f, 0.5f, 0.5f);
		if (breakTimer > 0f)
		{
			color = ((breakTimer % 0.1f > 0.05f) ? Color.black : Color.white);
		}
		else if (distance > maxDistance - 10f)
		{
			color = Color.Lerp(Color.red, color, (maxDistance - distance) / 10f);
		}
		lr.startColor = color;
		lr.endColor = color;
		if (attached)
		{
			charge = Mathf.MoveTowards(charge, 5f, Time.deltaTime);
			aud.pitch = 1f + charge / 5f;
			lightningPulseOrb.position = Vector3.Lerp(lineStartTransform.position, base.transform.position, charge % (0.25f / (charge / 4f)) * charge);
			pulseLine.SetPosition(0, lightningPulseOrb.position);
			pulseLine.SetPosition(1, lineStartTransform.position);
			if (charge >= 5f || attachedEnemy.dead)
			{
				Zap();
			}
		}
	}

	private void FixedUpdate()
	{
		if (!attached)
		{
			return;
		}
		raycastBlocked = Physics.Raycast(base.transform.position, connectedRB.position - base.transform.position, distance, LayerMaskDefaults.Get(LMD.Environment));
		if (distance > maxDistance || raycastBlocked)
		{
			AudioSource[] array = distanceWarningSounds;
			foreach (AudioSource audioSource in array)
			{
				if (breakTimer == 0f)
				{
					audioSource.Play();
				}
				audioSource.pitch = ((!raycastBlocked) ? 1 : 2);
			}
			breakTimer = Mathf.MoveTowards(breakTimer, 1f, Time.fixedDeltaTime * (float)((!raycastBlocked) ? 1 : 2));
			if (breakTimer >= 1f)
			{
				Break();
			}
			return;
		}
		if (breakTimer != 0f)
		{
			AudioSource[] array = distanceWarningSounds;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
		breakTimer = 0f;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!attached && !broken)
		{
			CheckAttach(other, Vector3.zero);
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (attached || broken)
		{
			return;
		}
		if (other.gameObject.layer == 8 || other.gameObject.layer == 24)
		{
			if (other.gameObject.CompareTag("Moving") && other.gameObject.TryGetComponent<Tram>(out var component) && (bool)component.controller)
			{
				component.controller.Zap();
			}
			Break();
		}
		else
		{
			CheckAttach(other.collider, other.contacts[0].point);
		}
	}

	private void CheckAttach(Collider other, Vector3 position)
	{
		if (other.gameObject.layer != 10 && other.gameObject.layer != 11)
		{
			return;
		}
		AttributeChecker component2;
		if (other.gameObject.TryGetComponent<EnemyIdentifierIdentifier>(out hitLimb) && (bool)hitLimb.eid && !hitLimb.eid.dead)
		{
			attached = true;
			attachedEnemy = hitLimb.eid;
			attachedEnemy.zapperer = this;
			base.transform.SetParent(other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform, worldPositionStays: true);
			if (!attachedEnemy.bigEnemy)
			{
				base.transform.position = other.bounds.center;
			}
			else
			{
				if (position == Vector3.zero)
				{
					position = ((!Physics.Raycast(base.transform.position - (other.bounds.center - base.transform.position).normalized, other.bounds.center - base.transform.position, out var hitInfo, Vector3.Distance(other.bounds.center, base.transform.position) + 1f, LayerMaskDefaults.Get(LMD.Enemies))) ? other.bounds.center : hitInfo.point);
				}
				base.transform.LookAt(position);
				base.transform.position = position;
			}
			rb.useGravity = false;
			rb.isKinematic = true;
			aud.Play();
			Object.Instantiate(attachSound, base.transform.position, Quaternion.identity);
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			lightningPulseOrb.position = lineStartTransform.position;
			lightningPulseOrb.gameObject.SetActive(value: true);
			openProngs.SetActive(value: false);
			closedProngs.SetActive(value: true);
			joint.connectedBody = null;
			Object.Destroy(joint);
			if (attachedEnemy.enemyType == EnemyType.Ferryman && attachedEnemy.TryGetComponent<Ferryman>(out var component) && (bool)component.currentWindup)
			{
				component.GotParried();
			}
		}
		else if (other.gameObject.TryGetComponent<AttributeChecker>(out component2) && component2.targetAttribute == HitterAttribute.Electricity)
		{
			Object.Instantiate(zapParticle, component2.transform.position, Quaternion.identity);
			component2.Activate();
		}
	}

	private void Zap()
	{
		if ((bool)attachedEnemy)
		{
			attachedEnemy.hitter = "zapper";
			attachedEnemy.hitterAttributes.Add(HitterAttribute.Electricity);
			attachedEnemy.DeliverDamage(hitLimb.gameObject, Vector3.up * 100000f, broken ? hitLimb.transform.position : base.transform.position, damage, tryForExplode: true, 0f, sourceWeapon);
			MonoSingleton<WeaponCharges>.Instance.naiZapperRecharge = 0f;
			EnemyIdentifierIdentifier[] componentsInChildren = attachedEnemy.GetComponentsInChildren<EnemyIdentifierIdentifier>();
			foreach (EnemyIdentifierIdentifier enemyIdentifierIdentifier in componentsInChildren)
			{
				if (enemyIdentifierIdentifier != hitLimb && enemyIdentifierIdentifier.gameObject != attachedEnemy.gameObject)
				{
					attachedEnemy.DeliverDamage(enemyIdentifierIdentifier.gameObject, Vector3.zero, enemyIdentifierIdentifier.transform.position, Mathf.Epsilon, tryForExplode: false);
				}
				Object.Instantiate(zapParticle, enemyIdentifierIdentifier.transform.position, Quaternion.identity).transform.localScale *= 0.5f;
			}
		}
		Break(successful: true);
	}

	public void Break(bool successful = false)
	{
		if (!broken)
		{
			broken = true;
			Object.Instantiate(breakParticle, base.transform.position, Quaternion.identity);
			if (attached && !successful)
			{
				Object.Instantiate(cableSnap, base.transform.position, Quaternion.identity);
			}
			if ((bool)attachedEnemy)
			{
				attachedEnemy.zapperer = this;
			}
			Object.Destroy(base.gameObject);
		}
	}

	public void ChargeBoost(float amount)
	{
		charge += amount;
		LineRenderer lineRenderer = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.electricLine, base.transform.position, Quaternion.identity);
		lineRenderer.SetPosition(0, base.transform.position);
		lineRenderer.SetPosition(1, lineStartTransform.position);
		Object.Instantiate(boostSound, base.transform.position, Quaternion.identity);
		if (lineRenderer.TryGetComponent<ElectricityLine>(out var component))
		{
			component.minWidth = 8f;
			component.maxWidth = 15f;
		}
	}
}

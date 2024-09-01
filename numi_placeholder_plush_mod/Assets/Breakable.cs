using Sandbox;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Breakable : MonoBehaviour, IAlter, IAlterOptions<bool>
{
	public bool unbreakable;

	public bool weak;

	public bool precisionOnly;

	public bool interrupt;

	public bool breakOnThrown;

	[HideInInspector]
	public EnemyIdentifier interruptEnemy;

	public bool playerOnly;

	public bool accurateExplosionsOnly;

	public bool forceGroundSlammable;

	[Space(10f)]
	public GameObject breakParticle;

	public AssetReference breakParticleFallback;

	public bool particleAtBoundsCenter;

	public Transform customPositionRotation;

	[Space(10f)]
	public bool crate;

	public int bounceHealth;

	[HideInInspector]
	public int originalBounceHealth;

	public GameObject crateCoin;

	public int coinAmount;

	private float defaultHeight;

	public bool protectorCrate;

	[Space(10f)]
	public GameObject[] activateOnBreak;

	public GameObject[] destroyOnBreak;

	public UltrakillEvent destroyEvent;

	private bool broken;

	private Collider col;

	private TimeSince? timeSinceBurn;

	private ItemIdentifier itid;

	private Rigidbody rb;

	public string alterKey => "breakable";

	public string alterCategoryName => "Breakable";

	public AlterOption<bool>[] options => new AlterOption<bool>[2]
	{
		new AlterOption<bool>
		{
			name = "Weak",
			key = "weak",
			value = weak,
			callback = delegate(bool value)
			{
				weak = value;
			}
		},
		new AlterOption<bool>
		{
			name = "Unbreakable",
			key = "unbreakable",
			value = unbreakable,
			callback = delegate(bool value)
			{
				unbreakable = value;
			}
		}
	};

	private void Start()
	{
		defaultHeight = base.transform.localScale.y;
		originalBounceHealth = bounceHealth;
		if ((breakParticle == null || breakParticle.Equals(null) || SceneHelper.IsPlayingCustom) && breakParticleFallback != null && breakParticleFallback.RuntimeKeyIsValid())
		{
			breakParticle = breakParticleFallback.ToAsset();
		}
		if (breakOnThrown)
		{
			rb = GetComponent<Rigidbody>();
			itid = GetComponent<ItemIdentifier>();
		}
	}

	public void Bounce()
	{
		if (originalBounceHealth > 0 && (bool)crateCoin && ((bool)col || TryGetComponent<Collider>(out col)))
		{
			Object.Instantiate(crateCoin, col.bounds.center, Quaternion.identity, GoreZone.ResolveGoreZone(base.transform).transform);
		}
		if (bounceHealth > 1)
		{
			base.transform.localScale = new Vector3(base.transform.localScale.x, defaultHeight / 4f, base.transform.localScale.z);
			bounceHealth--;
		}
		else
		{
			Break();
		}
	}

	private void Update()
	{
		if ((float?)timeSinceBurn > 3f)
		{
			Break();
		}
		if (crate && base.transform.localScale.y != defaultHeight)
		{
			base.transform.localScale = new Vector3(base.transform.localScale.x, Mathf.MoveTowards(base.transform.localScale.y, defaultHeight, Time.deltaTime * 10f), base.transform.localScale.z);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (breakOnThrown && (bool)itid && !itid.pickedUp && itid.beenPickedUp && (rb == null || !rb.isKinematic) && collision.gameObject != MonoSingleton<NewMovement>.Instance.gameObject)
		{
			Break();
		}
	}

	private void HitWith(GameObject target)
	{
		if (breakOnThrown && (bool)itid && (target.layer == 10 || target.layer == 11))
		{
			if (MonoSingleton<FistControl>.Instance.heldObject == itid)
			{
				MonoSingleton<FistControl>.Instance.currentPunch.ResetHeldState();
			}
			base.transform.position = target.transform.position;
			Break();
		}
	}

	public void Burn()
	{
		if (weak)
		{
			Break();
		}
		else if (!unbreakable && !broken && !timeSinceBurn.HasValue)
		{
			timeSinceBurn = 0f;
		}
	}

	public void ForceBreak()
	{
		unbreakable = false;
		Break();
	}

	public void Break()
	{
		Debug.Log("Break");
		timeSinceBurn = null;
		if (unbreakable || broken)
		{
			return;
		}
		if (TryGetComponent<SandboxProp>(out var component) && TryGetComponent<Rigidbody>(out var component2) && component2.isKinematic && (bool)MonoSingleton<SandboxNavmesh>.Instance)
		{
			MonoSingleton<SandboxNavmesh>.Instance.MarkAsDirty(component);
		}
		broken = true;
		if (breakParticle != null)
		{
			Vector3 position = base.transform.position;
			if (particleAtBoundsCenter && ((bool)col || TryGetComponent<Collider>(out col)))
			{
				position = col.bounds.center;
			}
			GameObject gameObject = Object.Instantiate(breakParticle, position, base.transform.rotation);
			if (customPositionRotation != null)
			{
				gameObject.transform.SetPositionAndRotation(customPositionRotation.position, customPositionRotation.rotation);
			}
		}
		if (crate)
		{
			MonoSingleton<CrateCounter>.Instance.AddCrate();
			if ((bool)crateCoin && coinAmount > 0 && ((bool)col || TryGetComponent<Collider>(out col)))
			{
				for (int i = 0; i < coinAmount; i++)
				{
					Object.Instantiate(crateCoin, col.bounds.center + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)), Quaternion.identity, GoreZone.ResolveGoreZone(base.transform).transform);
				}
			}
			if (protectorCrate && MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer)
			{
				MonoSingleton<PlatformerMovement>.Instance.AddExtraHit();
			}
		}
		Rigidbody[] componentsInChildren = GetComponentsInChildren<Rigidbody>();
		if (componentsInChildren.Length != 0)
		{
			Rigidbody[] array = componentsInChildren;
			foreach (Rigidbody obj in array)
			{
				obj.transform.SetParent(base.transform.parent, worldPositionStays: true);
				obj.isKinematic = false;
				obj.useGravity = true;
			}
		}
		if (activateOnBreak.Length != 0)
		{
			GameObject[] array2 = activateOnBreak;
			foreach (GameObject gameObject2 in array2)
			{
				if (gameObject2 != null)
				{
					gameObject2.SetActive(value: true);
				}
			}
		}
		if (destroyOnBreak.Length != 0)
		{
			GameObject[] array2 = destroyOnBreak;
			foreach (GameObject gameObject3 in array2)
			{
				if (gameObject3 != null)
				{
					Object.Destroy(gameObject3);
				}
			}
		}
		destroyEvent.Invoke();
		Object.Destroy(base.gameObject);
	}
}

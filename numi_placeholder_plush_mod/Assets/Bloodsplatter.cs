using Unity.Burst;
using UnityEngine;

public class Bloodsplatter : MonoBehaviour
{
	public BSType bloodSplatterType;

	[HideInInspector]
	public ParticleSystem part;

	private int i;

	private AudioSource aud;

	private int eidID;

	private SpriteRenderer sr;

	private MeshRenderer mr;

	private NewMovement nmov;

	public int hpAmount;

	private SphereCollider col;

	public bool hpOnParticleCollision;

	[HideInInspector]
	public bool beenPlayed;

	public bool halfChance;

	public bool ready;

	private GoreZone gz;

	public bool underwater;

	private MaterialPropertyBlock propertyBlock;

	private bool canCollide = true;

	public BloodsplatterManager bsm;

	[HideInInspector]
	public bool fromExplosion;

	private ComponentsDatabase cdatabase;

	[HideInInspector]
	public EnemyIdentifier eid
	{
		set
		{
			if (value != null)
			{
				eidID = value.GetInstanceID();
			}
		}
	}

	private void Awake()
	{
		if (propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
		if (!part)
		{
			part = GetComponent<ParticleSystem>();
		}
		if (part == null)
		{
			part = GetComponentInChildren<ParticleSystem>();
		}
		if (!aud)
		{
			aud = GetComponent<AudioSource>();
		}
		if (!col)
		{
			col = GetComponent<SphereCollider>();
		}
		cdatabase = MonoSingleton<ComponentsDatabase>.Instance;
		ParticleSystem.MainModule main = part.main;
		main.stopAction = ParticleSystemStopAction.Callback;
		part.AddListener<ParticleSystemStoppedMessage>(Repool);
	}

	private void OnEnable()
	{
		if (beenPlayed)
		{
			return;
		}
		beenPlayed = true;
		if (bsm == null)
		{
			bsm = MonoSingleton<BloodsplatterManager>.Instance;
		}
		bool flag = bsm.forceOn || MonoSingleton<PrefsManager>.Instance.GetBoolLocal("bloodEnabled");
		if ((bool)part)
		{
			part.Clear();
			if (flag)
			{
				part.Play();
			}
		}
		canCollide = true;
		if (aud != null)
		{
			aud.pitch = Random.Range(0.75f, 1.5f);
			aud.Play();
		}
		if ((bool)col)
		{
			col.enabled = true;
		}
		if (underwater)
		{
			Invoke("DisableCollider", 2.5f);
		}
		else
		{
			Invoke("DisableCollider", 0.25f);
		}
	}

	private void OnDisable()
	{
		CancelInvoke("DisableCollider");
		ready = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		Collide(other);
	}

	private void Collide(Collider other)
	{
		if (ready && !(bsm == null))
		{
			if (bsm.hasBloodFillers && ((bsm.bloodFillers.Contains(other.gameObject) && other.gameObject.TryGetComponent<BloodFiller>(out var component)) || ((bool)other.attachedRigidbody && bsm.bloodFillers.Contains(other.attachedRigidbody.gameObject) && other.attachedRigidbody.TryGetComponent<BloodFiller>(out component))))
			{
				component.FillBloodSlider(hpAmount, base.transform.position, eidID);
			}
			else if (canCollide && other.gameObject.CompareTag("Player"))
			{
				MonoSingleton<NewMovement>.Instance.GetHealth(hpAmount, silent: false, fromExplosion);
				DisableCollider();
			}
		}
	}

	public void Repool()
	{
		if (bloodSplatterType == BSType.dontpool)
		{
			Object.Destroy(base.gameObject);
		}
		if (bloodSplatterType == BSType.unknown)
		{
			Debug.LogWarning(string.Concat(base.gameObject, "has an unknown BSType, this shouldn't happen!"));
			Object.Destroy(base.gameObject);
		}
		gz = null;
		eid = null;
		fromExplosion = false;
		ready = false;
		beenPlayed = false;
		base.transform.localScale = Vector3.one;
		if ((bool)bsm)
		{
			bsm.RepoolGore(this, bloodSplatterType);
		}
	}

	private void PlayBloodSound(Vector3 position)
	{
		if (Random.value < 0.1f)
		{
			bsm.splatterClip.PlayClipAtPoint(bsm.goreAudioGroup, position, 256, 1f, 1f, 0.5f, AudioRolloffMode.Logarithmic);
		}
	}

	[BurstCompile]
	public void CreateBloodstain(ref RaycastHit hit, BloodsplatterManager bsman)
	{
		bsm = bsman;
		Collider collider = hit.collider;
		if (collider == null)
		{
			return;
		}
		Rigidbody rigidbody = hit.rigidbody;
		GameObject gameObject = (rigidbody ? rigidbody.gameObject : collider.gameObject);
		Vector3 point = hit.point;
		if (StockMapInfo.Instance.continuousGibCollisions && gameObject.TryGetComponent<IBloodstainReceiver>(out var component) && component.HandleBloodstainHit(ref hit))
		{
			PlayBloodSound(point);
			return;
		}
		if (ready && hpOnParticleCollision && gameObject.CompareTag("Player"))
		{
			MonoSingleton<NewMovement>.Instance.GetHealth(3, silent: false, fromExplosion);
			return;
		}
		Transform transform = gameObject.transform;
		float bloodstainChance = bsm.GetBloodstainChance();
		bloodstainChance = (halfChance ? (bloodstainChance / 2f) : bloodstainChance);
		if (!((float)Random.Range(0, 100) < bloodstainChance) || (!gameObject.CompareTag("Wall") && !gameObject.CompareTag("Floor") && !gameObject.CompareTag("Moving") && ((!gameObject.CompareTag("Glass") && !gameObject.CompareTag("GlassFloor")) || transform.childCount <= 0)))
		{
			return;
		}
		Vector3 normal = hit.normal;
		if (!gz)
		{
			gz = GoreZone.ResolveGoreZone(base.transform);
		}
		PlayBloodSound(point);
		Vector3 vector = point + normal * 0.2f;
		if (gameObject.CompareTag("Moving") || ((bool)cdatabase && cdatabase.scrollers.Contains(transform)))
		{
			if ((bool)cdatabase && cdatabase.scrollers.Contains(transform) && transform.TryGetComponent<ScrollingTexture>(out var component2))
			{
				component2.parent.CreateChild(vector, normal);
			}
			else
			{
				gameObject.GetOrAddComponent<BloodstainParent>().CreateChild(vector, normal, fromStep: true);
			}
		}
		else if (gameObject.CompareTag("Glass") || gameObject.CompareTag("GlassFloor"))
		{
			gameObject.GetOrAddComponent<BloodstainParent>().CreateChild(vector, normal, fromStep: true);
		}
		else
		{
			bsm.CreateBloodstain(vector, normal);
		}
	}

	private void DisableCollider()
	{
		canCollide = false;
		if (!part.isPlaying)
		{
			Repool();
		}
	}

	public void GetReady()
	{
		ready = true;
	}
}

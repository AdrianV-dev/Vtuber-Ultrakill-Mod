using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class BloodsplatterManager : MonoSingleton<BloodsplatterManager>
{
	public struct InstanceProperties
	{
		public float3 pos;

		public float3 norm;

		public float isOil;

		public int parentIndex;

		public const int SIZE = 32;
	}

	public struct ClearJob : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<InstanceProperties> props;

		public void Execute(int index)
		{
			props[index] = default(InstanceProperties);
		}
	}

	public bool forceOn;

	public bool forceGibs;

	public bool neverFreezeGibs;

	public bool overrideBloodstainChance;

	public float bloodstainChance;

	public GameObject head;

	public GameObject limb;

	public GameObject body;

	public GameObject small;

	public GameObject smallest;

	public GameObject splatter;

	public GameObject underwater;

	public GameObject sand;

	public GameObject blessing;

	public GameObject chestExplosion;

	public GameObject brainChunk;

	public GameObject skullChunk;

	public GameObject eyeball;

	public GameObject jawChunk;

	public GameObject[] gib;

	public GameObject bloodStain;

	private Dictionary<BSType, Queue<GameObject>> gorePool = new Dictionary<BSType, Queue<GameObject>>();

	private Queue<GameObject> stainPool = new Queue<GameObject>();

	private Dictionary<BSType, int> defaultHPValues = new Dictionary<BSType, int>();

	private int order;

	private Transform goreStore;

	public bool hasBloodFillers;

	public HashSet<GameObject> bloodFillers = new HashSet<GameObject>();

	private WaitForSeconds slowUpdate = new WaitForSeconds(2f);

	public AudioMixerGroup goreAudioGroup;

	public AudioClip splatterClip;

	[HideInInspector]
	public int bloodDestroyers;

	[HideInInspector]
	public int bloodAbsorbers;

	[HideInInspector]
	public int bloodAbsorberChildren;

	public NativeArray<InstanceProperties> checkpointProps;

	public NativeArray<InstanceProperties> props;

	public NativeArray<float4x4> parents;

	public ComputeBuffer instanceBuffer;

	public ComputeBuffer parentBuffer;

	private int checkpointPropIndex;

	private int propIndex;

	private int parentIndex = 1;

	public Mesh stainMesh;

	public Material stainMat;

	public Bounds bloodstainBounds;

	public ClearJob clearJob;

	public const float PARTICLE_COLLISION_STEP_DT = 0.128f;

	public TimeSince sinceLastStep;

	private OptionsManager opm;

	public bool goreOn
	{
		get
		{
			if (!forceOn && !forceGibs)
			{
				return MonoSingleton<PrefsManager>.Instance.GetBoolLocal("bloodEnabled");
			}
			return true;
		}
	}

	public event Action<int> reuseParentIndex;

	public event Action<int> reuseStainIndex;

	public event Action StainsCleared;

	public event Action<float> ParticleCollisionStep;

	public event Action PostCollisionStep;

	public void SaveBloodstains()
	{
		checkpointPropIndex = propIndex;
		checkpointProps.CopyFrom(props);
	}

	public void LoadBloodstains()
	{
		props.CopyFrom(checkpointProps);
		propIndex = checkpointPropIndex;
		instanceBuffer.SetData(props);
	}

	public int CreateBloodstain(float3 pos, float3 norm, int parent = 0)
	{
		propIndex = (propIndex + 1) % props.Length;
		props[propIndex] = new InstanceProperties
		{
			pos = pos,
			norm = norm,
			parentIndex = parent
		};
		this.reuseStainIndex?.Invoke(propIndex);
		instanceBuffer.SetData(props, propIndex, propIndex, 1);
		return propIndex;
	}

	public void DeleteBloodstain(int index)
	{
		props[index] = default(InstanceProperties);
		this.reuseStainIndex?.Invoke(index);
		instanceBuffer.SetData(props, index, index, 1);
	}

	public int CreateParent(float4x4 initialMatrix)
	{
		int num = parentIndex++;
		if (num >= parents.Length)
		{
			num = (parentIndex = 1);
		}
		this.reuseParentIndex?.Invoke(num);
		parents[num] = initialMatrix;
		return num;
	}

	public float GetBloodstainChance()
	{
		if (overrideBloodstainChance)
		{
			return bloodstainChance;
		}
		return opm.bloodstainChance;
	}

	protected override void Awake()
	{
		base.Awake();
		bloodstainBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
		props = new NativeArray<InstanceProperties>((int)MonoSingleton<PrefsManager>.Instance.GetFloatLocal("bloodStainMax", 100000f), Allocator.Persistent);
		checkpointProps = new NativeArray<InstanceProperties>(props.Length, Allocator.Persistent);
		instanceBuffer = new ComputeBuffer(props.Length, 32, ComputeBufferType.Structured);
		instanceBuffer.SetData(props);
		clearJob.props = props;
		parents = new NativeArray<float4x4>(512, Allocator.Persistent);
		parentBuffer = new ComputeBuffer(parents.Length, 64, ComputeBufferType.Structured);
		parents[0] = float4x4.identity;
		stainMat.SetBuffer("instanceBuffer", instanceBuffer);
		stainMat.SetBuffer("parentBuffer", parentBuffer);
		goreStore = base.transform.GetChild(0);
		float num = 0f;
		foreach (BSType value in Enum.GetValues(typeof(BSType)))
		{
			if (value != BSType.dontpool && value != BSType.unknown)
			{
				gorePool.Add(value, new Queue<GameObject>());
				num += 1f;
			}
		}
		opm = MonoSingleton<OptionsManager>.Instance;
		StartCoroutine(InitPools());
	}

	private void Update()
	{
		if ((float)sinceLastStep >= 0.128f)
		{
			sinceLastStep = 0f;
			this.ParticleCollisionStep?.Invoke(0.128f);
			this.PostCollisionStep?.Invoke();
		}
	}

	public void ClearStains()
	{
		clearJob.Schedule(props.Length, 512).Complete();
		instanceBuffer.SetData(props);
		this.StainsCleared?.Invoke();
	}

	private void LateUpdate()
	{
		parentBuffer.SetData(parents);
		Graphics.DrawMeshInstancedProcedural(stainMesh, 0, stainMat, bloodstainBounds, props.Length);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		checkpointProps.Dispose();
		props.Dispose();
		parents.Dispose();
		instanceBuffer.Release();
		parentBuffer.Release();
	}

	private GameObject GetPrefabByBSType(BSType bloodType)
	{
		return bloodType switch
		{
			BSType.head => head, 
			BSType.limb => limb, 
			BSType.body => body, 
			BSType.small => small, 
			BSType.smallest => smallest, 
			BSType.splatter => splatter, 
			BSType.underwater => underwater, 
			BSType.sand => sand, 
			BSType.blessing => blessing, 
			BSType.chestExplosion => chestExplosion, 
			BSType.brainChunk => brainChunk, 
			BSType.skullChunk => skullChunk, 
			BSType.eyeball => eyeball, 
			BSType.jawChunk => jawChunk, 
			BSType.gib => gib[UnityEngine.Random.Range(0, gib.Length)], 
			_ => null, 
		};
	}

	private IEnumerator InitPools()
	{
		InitPool(BSType.head);
		yield return null;
		InitPool(BSType.limb);
		yield return null;
		InitPool(BSType.body);
		yield return null;
		InitPool(BSType.small);
		yield return null;
		InitPool(BSType.splatter);
		yield return null;
		InitPool(BSType.underwater);
		yield return null;
		InitPool(BSType.smallest);
		yield return null;
		InitPool(BSType.sand);
		yield return null;
		InitPool(BSType.blessing);
		yield return null;
		InitPool(BSType.brainChunk);
		yield return null;
		InitPool(BSType.skullChunk);
		yield return null;
		InitPool(BSType.eyeball);
		yield return null;
		InitPool(BSType.jawChunk);
		yield return null;
		InitPool(BSType.gib);
		yield return null;
		InitPool(BSType.chestExplosion);
		yield return null;
	}

	private void InitPool(BSType bloodSplatterType)
	{
		Queue<GameObject> queue = gorePool[bloodSplatterType];
		GameObject prefabByBSType = GetPrefabByBSType(bloodSplatterType);
		if (prefabByBSType.TryGetComponent<Bloodsplatter>(out var component))
		{
			defaultHPValues.Add(bloodSplatterType, component.hpAmount);
		}
		int num = ((bloodSplatterType == BSType.body) ? 200 : 100);
		if (bloodSplatterType == BSType.gib || bloodSplatterType == BSType.brainChunk || bloodSplatterType == BSType.skullChunk || bloodSplatterType == BSType.eyeball || bloodSplatterType == BSType.jawChunk)
		{
			num = 200;
		}
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(prefabByBSType, goreStore);
			queue.Enqueue(gameObject);
			if (gameObject.TryGetComponent<Bloodsplatter>(out component))
			{
				component.bsm = this;
			}
		}
	}

	private void InitStains()
	{
		for (int i = 0; i < 500; i++)
		{
			stainPool.Enqueue(UnityEngine.Object.Instantiate(bloodStain, goreStore));
		}
	}

	public GameObject GetStain(Vector3 position, Quaternion rotation)
	{
		GameObject gameObject = null;
		while (gameObject == null && stainPool.Count > 0)
		{
			gameObject = stainPool.Dequeue();
		}
		if (gameObject == null)
		{
			gameObject = UnityEngine.Object.Instantiate(bloodStain, position, rotation);
		}
		gameObject.transform.SetPositionAndRotation(position, rotation);
		gameObject.SetActive(value: true);
		return gameObject;
	}

	public void RepoolStain(GameObject stain)
	{
		if (stain != null)
		{
			stain.SetActive(value: false);
			stainPool.Enqueue(stain);
		}
	}

	public void RepoolGore(Bloodsplatter bs, BSType type)
	{
		if (type != BSType.dontpool && defaultHPValues.TryGetValue(type, out var value))
		{
			bs.hpAmount = value;
		}
		RepoolGore(bs.gameObject, type);
	}

	public void RepoolGore(GameObject go, BSType type)
	{
		if ((bool)go)
		{
			if (type != BSType.dontpool)
			{
				ReturnToQueue(go, type);
			}
			else
			{
				UnityEngine.Object.Destroy(go);
			}
		}
	}

	private void ReturnToQueue(GameObject go, BSType type)
	{
		if (type == BSType.unknown || type == BSType.dontpool)
		{
			UnityEngine.Object.Destroy(go);
		}
		go.SetActive(value: false);
		gorePool[type].Enqueue(go);
		go.transform.SetParent(goreStore);
		go.transform.localScale = Vector3.one;
	}

	public GameObject GetFromQueue(BSType type)
	{
		GameObject gameObject = null;
		Queue<GameObject> queue = gorePool[type];
		while (gameObject == null && queue.Count > 0)
		{
			gameObject = queue.Dequeue();
		}
		if (gameObject == null)
		{
			gameObject = UnityEngine.Object.Instantiate(GetPrefabByBSType(type), goreStore);
		}
		if (gameObject == null)
		{
			return null;
		}
		gameObject.SetActive(value: true);
		return gameObject;
	}

	public GameObject GetGore(GoreType got, EnemyIdentifier eid, bool fromExplosion = false)
	{
		return GetGore(got, eid.underwater, eid.sandified, eid.blessed, eid, fromExplosion);
	}

	public GameObject GetGore(GoreType got, bool isUnderwater = false, bool isSandified = false, bool isBlessed = false, EnemyIdentifier eid = null, bool fromExplosion = false)
	{
		if (isBlessed)
		{
			GameObject fromQueue = GetFromQueue(BSType.blessing);
			AudioSource component = fromQueue.GetComponent<AudioSource>();
			float splatterWeight = GetSplatterWeight(got);
			component.pitch = 1.15f + UnityEngine.Random.Range(-0.15f, 0.15f);
			component.volume = splatterWeight * 0.9f + 0.1f;
			fromQueue.transform.localScale *= splatterWeight * splatterWeight * 3f;
			return fromQueue;
		}
		if (isSandified)
		{
			GameObject fromQueue = GetFromQueue(BSType.sand);
			if (got == GoreType.Head)
			{
				return fromQueue;
			}
			AudioSource component2 = fromQueue.GetComponent<AudioSource>();
			AudioSource component3 = fromQueue.transform.GetChild(0).GetComponent<AudioSource>();
			AudioSource originalAudio = GetOriginalAudio(got);
			if ((bool)originalAudio)
			{
				component2.clip = originalAudio.clip;
				component2.volume = originalAudio.volume - 0.35f;
				component3.volume = originalAudio.volume - 0.2f;
			}
			return fromQueue;
		}
		switch (got)
		{
		case GoreType.Head:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				PrepareGore(fromQueue, -1, eid, fromExplosion);
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.head);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		case GoreType.Limb:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				fromQueue.transform.localScale *= 0.75f;
				PrepareGore(fromQueue, 20, eid, fromExplosion);
				AudioSource component8 = fromQueue.GetComponent<AudioSource>();
				AudioSource component9 = limb.GetComponent<AudioSource>();
				component8.clip = component9.clip;
				component8.volume = component9.volume;
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.limb);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		case GoreType.Body:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				fromQueue.transform.localScale *= 0.5f;
				PrepareGore(fromQueue, 10, eid, fromExplosion);
				AudioSource component12 = fromQueue.GetComponent<AudioSource>();
				AudioSource component13 = body.GetComponent<AudioSource>();
				component12.clip = component13.clip;
				component12.volume = component13.volume;
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.body);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		case GoreType.Small:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				fromQueue.transform.localScale *= 0.25f;
				PrepareGore(fromQueue, 10, eid, fromExplosion);
				AudioSource component6 = fromQueue.GetComponent<AudioSource>();
				AudioSource component7 = small.GetComponent<AudioSource>();
				component6.clip = component7.clip;
				component6.volume = component7.volume;
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.small);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		case GoreType.Smallest:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				fromQueue.transform.localScale *= 0.15f;
				PrepareGore(fromQueue, 5, eid, fromExplosion);
				AudioSource component10 = fromQueue.GetComponent<AudioSource>();
				AudioSource component11 = smallest.GetComponent<AudioSource>();
				component10.clip = component11.clip;
				component10.volume = component11.volume;
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.smallest);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		case GoreType.Splatter:
		{
			GameObject fromQueue;
			if (isUnderwater)
			{
				fromQueue = GetFromQueue(BSType.underwater);
				PrepareGore(fromQueue, -1, eid, fromExplosion);
				AudioSource component4 = fromQueue.GetComponent<AudioSource>();
				AudioSource component5 = splatter.GetComponent<AudioSource>();
				component4.clip = component5.clip;
				component4.volume = component5.volume;
				return fromQueue;
			}
			fromQueue = GetFromQueue(BSType.splatter);
			PrepareGore(fromQueue, -1, eid, fromExplosion);
			return fromQueue;
		}
		default:
			return null;
		}
	}

	private void PrepareGore(GameObject gob, int healthChange = -1, EnemyIdentifier eid = null, bool fromExplosion = false)
	{
		if ((healthChange >= 0 || !(eid == null) || fromExplosion) && gob.TryGetComponent<Bloodsplatter>(out var component))
		{
			if (healthChange >= 0)
			{
				component.hpAmount = healthChange;
			}
			if ((bool)eid)
			{
				component.eid = eid;
			}
			if (fromExplosion)
			{
				component.fromExplosion = true;
			}
		}
	}

	public GameObject GetGib(BSType type)
	{
		Queue<GameObject> queue = gorePool[type];
		GameObject gameObject = null;
		while (queue.Count > 0 && gameObject == null)
		{
			gameObject = queue.Dequeue();
		}
		if (gameObject == null)
		{
			gameObject = UnityEngine.Object.Instantiate(GetPrefabByBSType(type));
		}
		return gameObject;
	}

	private AudioSource GetOriginalAudio(GoreType got)
	{
		return got switch
		{
			GoreType.Limb => limb.GetComponent<AudioSource>(), 
			GoreType.Body => body.GetComponent<AudioSource>(), 
			GoreType.Small => small.GetComponent<AudioSource>(), 
			GoreType.Smallest => smallest.GetComponent<AudioSource>(), 
			_ => null, 
		};
	}

	private float GetSplatterWeight(GoreType got)
	{
		return got switch
		{
			GoreType.Limb => 0.75f, 
			GoreType.Body => 0.5f, 
			GoreType.Small => 0.125f, 
			GoreType.Smallest => 0.075f, 
			_ => 1f, 
		};
	}
}

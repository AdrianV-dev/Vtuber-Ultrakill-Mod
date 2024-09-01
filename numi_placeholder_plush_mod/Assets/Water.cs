using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Water : MonoBehaviour
{
	[HideInInspector]
	public Dictionary<Rigidbody, int> rbs = new Dictionary<Rigidbody, int>();

	private HashSet<Collider> contSplashables = new HashSet<Collider>();

	private HashSet<Rigidbody> rbsToRemove = new HashSet<Rigidbody>();

	private List<Collider> colsToRemove = new List<Collider>();

	private Dictionary<Collider, int> enemiesToCheck = new Dictionary<Collider, int>();

	private HashSet<Collider> enemiesToRemove = new HashSet<Collider>();

	private HashSet<GameObject> bubblesEffects = new HashSet<GameObject>();

	private Dictionary<Rigidbody, Collider> onDisableRemove = new Dictionary<Rigidbody, Collider>();

	public GameObject bubblesParticle;

	public GameObject splash;

	public GameObject smallSplash;

	private Dictionary<Collider, GameObject> colliderToSplashMap = new Dictionary<Collider, GameObject>();

	private Collider[] colliders;

	public Color clr = new Color(0f, 0.5f, 1f);

	[HideInInspector]
	public bool inWater;

	[HideInInspector]
	public bool playerTouchingWater;

	private int waterRequests;

	public bool notWet;

	private UnderwaterController currentUwc;

	public List<Collider> enteredColliders = new List<Collider>();

	[Header("Optional, for fishing")]
	public FishDB fishDB;

	public Transform overrideFishingPoint;

	public FishObject[] attractFish;

	public bool simplifyWaterProcessing;

	private DryZoneController dzc;

	private void Start()
	{
		dzc = MonoSingleton<DryZoneController>.Instance;
		Invoke("SlowUpdate", 5f);
		colliders = GetComponentsInChildren<Collider>();
		dzc.waters.Add(this);
		if ((bool)fishDB)
		{
			fishDB.SetupWater(this);
		}
	}

	private void OnDestroy()
	{
		Shader.DisableKeyword("ISUNDERWATER");
		if (base.gameObject.scene.isLoaded)
		{
			UnderwaterController instance = MonoSingleton<UnderwaterController>.Instance;
			if ((bool)instance && inWater)
			{
				instance.OutWater();
			}
			dzc.waters.Remove(this);
		}
	}

	private void FixedUpdate()
	{
		CheckEnemies();
		RigidBodyForces();
		CleanSplashables();
		UpdateSplashes();
	}

	private void CheckEnemies()
	{
		enemiesToRemove.Clear();
		foreach (Collider key in enemiesToCheck.Keys)
		{
			if (!key || !key.attachedRigidbody)
			{
				enemiesToRemove.Add(key);
				continue;
			}
			bool flag = false;
			Vector3 position = key.transform.position;
			Bounds bounds = key.bounds;
			Vector3 min = bounds.min;
			Vector3 max = bounds.max;
			for (int i = 0; i < colliders.Length; i++)
			{
				Collider collider = colliders[i];
				if (Vector3.Distance(collider.ClosestPoint(position), position) < 1f)
				{
					Vector3 origin = new Vector3(position.x, collider.bounds.max.y + 0.1f, position.z);
					if (Physics.Raycast(origin, Vector3.down, out var hitInfo, Mathf.Abs(origin.y - min.y), 16, QueryTriggerInteraction.Collide) && max.y - (max.y - min.y) / 3f < hitInfo.point.y)
					{
						flag = true;
						break;
					}
				}
			}
			Rigidbody attachedRigidbody = key.attachedRigidbody;
			int value;
			bool flag2 = rbs.TryGetValue(attachedRigidbody, out value);
			if (flag && !flag2)
			{
				AddRigidbody(attachedRigidbody, key);
				if (attachedRigidbody.TryGetComponent<EnemyIdentifier>(out var component))
				{
					component.underwater = true;
				}
			}
			else if (!flag && flag2 && value == 1)
			{
				RemoveRigidbody(attachedRigidbody, key);
				if (attachedRigidbody.TryGetComponent<EnemyIdentifier>(out var component2))
				{
					component2.underwater = false;
				}
			}
		}
		foreach (Collider item in enemiesToRemove)
		{
			enemiesToCheck.Remove(item);
		}
	}

	private void RigidBodyForces()
	{
		rbsToRemove.Clear();
		Vector3 gravity = Physics.gravity;
		float num = gravity.y * 0.2f;
		foreach (Rigidbody key in rbs.Keys)
		{
			GameObject gameObject;
			if (!key || !(gameObject = key.gameObject).activeInHierarchy)
			{
				rbsToRemove.Add(key);
			}
			else if (key.useGravity && !key.isKinematic)
			{
				int layer = gameObject.layer;
				Vector3 velocity = key.velocity;
				if (velocity.y < num)
				{
					key.velocity = Vector3.MoveTowards(target: new Vector3(velocity.x, num, velocity.z), current: velocity, maxDistanceDelta: Time.deltaTime * 10f * Mathf.Abs(velocity.y - num + 0.5f));
				}
				else if (layer == 10 || layer == 9)
				{
					key.AddForce(gravity * (key.mass * -0.45f));
				}
				else
				{
					key.AddForce(gravity * (key.mass * -0.75f));
				}
			}
		}
		foreach (Rigidbody item in rbsToRemove)
		{
			if (!item)
			{
				rbs.Remove(item);
				continue;
			}
			Collider componentInChildren = item.GetComponentInChildren<Collider>();
			if ((bool)componentInChildren)
			{
				RemoveRigidbody(item, componentInChildren);
			}
			else
			{
				rbs.Remove(item);
			}
		}
	}

	private void CleanSplashables()
	{
		contSplashables.RemoveWhere((Collider x) => x == null);
		foreach (KeyValuePair<Collider, GameObject> item in colliderToSplashMap)
		{
			Collider key = item.Key;
			if (!contSplashables.Contains(key))
			{
				GameObject value = item.Value;
				if (value != null)
				{
					value.SendMessage("DestroySoon");
				}
				colsToRemove.Add(key);
			}
		}
		foreach (Collider item2 in colsToRemove)
		{
			colliderToSplashMap.Remove(item2);
		}
		colsToRemove.Clear();
	}

	private void UpdateSplashes()
	{
		foreach (Collider contSplashable in contSplashables)
		{
			Bounds bounds = contSplashable.bounds;
			bool didHit;
			Vector3 closestPointOnSurface = GetClosestPointOnSurface(contSplashable, out didHit);
			GameObject value2;
			if (didHit)
			{
				if (!colliderToSplashMap.TryGetValue(contSplashable, out var value))
				{
					GameObject gameObject = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.continuousSplash, closestPointOnSurface, Quaternion.LookRotation(Vector3.up));
					gameObject.transform.localScale = 3f * bounds.size.magnitude * Vector3.one;
					if (enemiesToCheck.ContainsKey(contSplashable) && gameObject.TryGetComponent<SplashContinuous>(out var component) && contSplashable.TryGetComponent<NavMeshAgent>(out var component2))
					{
						component.nma = component2;
					}
					colliderToSplashMap.Add(contSplashable, gameObject);
				}
				else
				{
					value.transform.position = closestPointOnSurface;
				}
			}
			else if (colliderToSplashMap.TryGetValue(contSplashable, out value2))
			{
				if (value2 != null)
				{
					value2.SendMessage("DestroySoon");
				}
				colsToRemove.Add(contSplashable);
			}
		}
	}

	private void SlowUpdate()
	{
		if (bubblesEffects.Count > 0)
		{
			bubblesEffects.RemoveWhere((GameObject GameObject) => GameObject == null);
		}
		if (enteredColliders.Count > 0)
		{
			enteredColliders.RemoveAll((Collider Collider) => Collider == null);
		}
		Invoke("SlowUpdate", Random.Range(0.5f, 1f));
	}

	private void OnDisable()
	{
		if (!base.gameObject.scene.isLoaded)
		{
			return;
		}
		foreach (GameObject bubblesEffect in bubblesEffects)
		{
			if ((bool)bubblesEffect)
			{
				Object.Destroy(bubblesEffect);
			}
		}
		bubblesEffects.Clear();
		onDisableRemove.Clear();
		foreach (Rigidbody key in rbs.Keys)
		{
			if (key != null)
			{
				Collider componentInChildren = key.GetComponentInChildren<Collider>();
				if ((bool)componentInChildren)
				{
					onDisableRemove.Add(key, componentInChildren);
				}
			}
		}
		rbs.Clear();
		contSplashables.Clear();
		enemiesToCheck.Clear();
		foreach (GameObject value in colliderToSplashMap.Values)
		{
			Object.Destroy(value);
		}
		colliderToSplashMap.Clear();
		if (inWater)
		{
			inWater = false;
			waterRequests = 0;
			Shader.DisableKeyword("ISUNDERWATER");
			UnderwaterController instance = MonoSingleton<UnderwaterController>.Instance;
			if (instance != null)
			{
				instance.OutWater();
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!dzc.colliderCalls.ContainsKey(other))
		{
			Enter(other);
		}
		enteredColliders.Add(other);
	}

	private void Enter(Collider other)
	{
		if (other.TryGetComponent<UnderwaterController>(out var _))
		{
			if (currentUwc != null && MonoSingleton<UnderwaterController>.Instance != currentUwc)
			{
				waterRequests = 0;
			}
			Shader.EnableKeyword("ISUNDERWATER");
			inWater = true;
			GameObject gameObject = Object.Instantiate(bubblesParticle, MonoSingleton<PlayerTracker>.Instance.GetPlayer());
			gameObject.transform.forward = Vector3.up;
			bubblesEffects.Add(gameObject);
			waterRequests++;
			MonoSingleton<UnderwaterController>.Instance.InWater(clr);
			currentUwc = MonoSingleton<UnderwaterController>.Instance;
		}
		if (other.isTrigger)
		{
			return;
		}
		Rigidbody component2 = other.GetComponent<Rigidbody>();
		if (!component2)
		{
			return;
		}
		if (other.gameObject.layer == 12 && !rbs.ContainsKey(component2))
		{
			Collider component3 = other.gameObject.GetComponent<Collider>();
			if ((bool)component3 && !enemiesToCheck.ContainsKey(component3))
			{
				enemiesToCheck.Add(component3, 1);
				contSplashables.Add(component3);
				if (component2.TryGetComponent<EnemyIdentifier>(out var component4) && !component4.touchingWaters.Contains(this))
				{
					component4.touchingWaters.Add(this);
				}
			}
			else if ((bool)component3)
			{
				enemiesToCheck[component3]++;
			}
		}
		else if (!rbs.ContainsKey(component2))
		{
			AddRigidbody(component2, other);
		}
		else
		{
			rbs[component2]++;
		}
	}

	private void AddRigidbody(Rigidbody rb, Collider other)
	{
		rbs.Add(rb, 1);
		if (rb.gameObject == MonoSingleton<NewMovement>.Instance.gameObject)
		{
			playerTouchingWater = true;
		}
		Transform transform = other.transform;
		Vector3 position = transform.position;
		GameObject gameObject = other.gameObject;
		Vector3 vector = Vector3.positiveInfinity;
		float num = float.PositiveInfinity;
		for (int i = 0; i < colliders.Length; i++)
		{
			Collider collider = colliders[i];
			Vector3 position2 = new Vector3(position.x, collider.bounds.max.y, position.z);
			Vector3 vector2 = collider.ClosestPoint(position2);
			float num2 = Vector3.Distance(vector2, position);
			if (num2 < num)
			{
				vector = vector2;
				num = num2;
			}
		}
		GameObject gameObject2 = null;
		if (Vector3.Distance(vector, other.ClosestPoint(vector)) < 1f && rb != null)
		{
			if ((rb.velocity.y < -25f || gameObject.layer == 11) && rb.mass >= 1f && gameObject.layer != 10 && gameObject.layer != 9)
			{
				gameObject2 = Object.Instantiate(splash, vector, Quaternion.LookRotation(Vector3.up));
			}
			else if (!rb.isKinematic)
			{
				gameObject2 = Object.Instantiate(smallSplash, vector, Quaternion.LookRotation(Vector3.up));
			}
			if ((bool)gameObject2)
			{
				gameObject2.transform.localScale = 3f * other.bounds.size.magnitude * Vector3.one;
			}
		}
		if (gameObject.CompareTag("Player"))
		{
			if ((bool)gameObject2)
			{
				gameObject2.GetComponent<RandomPitch>().defaultPitch = 0.45f;
			}
			contSplashables.Add(other);
			return;
		}
		if (simplifyWaterProcessing)
		{
			GameObject gameObject3 = Object.Instantiate(bubblesParticle, position, Quaternion.identity);
			gameObject3.transform.SetParent(transform, worldPositionStays: true);
			gameObject3.transform.forward = Vector3.up;
			bubblesEffects.Add(gameObject3);
		}
		BloodUnderwaterChecker component;
		AudioSource[] array = ((gameObject.layer != 0 || !gameObject.TryGetComponent<BloodUnderwaterChecker>(out component)) ? other.GetComponentsInChildren<AudioSource>() : transform.parent.GetComponentsInChildren<AudioSource>());
		AudioSource[] array2 = array;
		foreach (AudioSource audioSource in array2)
		{
			if (!audioSource.TryGetComponent<AudioLowPassFilter>(out var component2))
			{
				component2 = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
			}
			component2.cutoffFrequency = 1000f;
			component2.lowpassResonanceQ = 1f;
		}
		if (notWet)
		{
			return;
		}
		Flammable[] componentsInChildren = other.GetComponentsInChildren<Flammable>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].PutOut();
		}
		if (gameObject.layer != 10 && gameObject.layer != 9)
		{
			if (!other.TryGetComponent<Wet>(out var component3))
			{
				gameObject.AddComponent<Wet>();
			}
			else
			{
				component3.Refill();
			}
		}
		if (gameObject.layer == 12 && gameObject.TryGetComponent<EnemyIdentifier>(out var component4) && component4.enemyType == EnemyType.Streetcleaner && !component4.dead)
		{
			component4.InstaKill();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!dzc.colliderCalls.ContainsKey(other))
		{
			Exit(other);
		}
		enteredColliders.Remove(other);
	}

	public void Exit(Collider other)
	{
		if (other.TryGetComponent<UnderwaterController>(out var _))
		{
			Shader.DisableKeyword("ISUNDERWATER");
			waterRequests--;
			if (waterRequests <= 0)
			{
				waterRequests = 0;
				inWater = false;
			}
			MonoSingleton<UnderwaterController>.Instance.OutWater();
			ParticleSystem[] componentsInChildren = other.transform.parent.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				GameObject gameObject = componentsInChildren[i].gameObject;
				if (bubblesEffects.Remove(gameObject))
				{
					Object.Destroy(gameObject);
				}
			}
		}
		Rigidbody component2 = other.GetComponent<Rigidbody>();
		if (!component2)
		{
			return;
		}
		if (other.gameObject.layer == 12)
		{
			if (!enemiesToCheck.TryGetValue(other, out var value))
			{
				return;
			}
			value--;
			enemiesToCheck[other] = value;
			if (value <= 0)
			{
				enemiesToCheck.Remove(other);
				if (component2.TryGetComponent<EnemyIdentifier>(out var component3) && component3.touchingWaters.Contains(this))
				{
					component3.touchingWaters.Remove(this);
				}
				contSplashables.Remove(other);
				if (rbs.ContainsKey(component2))
				{
					rbs[component2] = 1;
					RemoveRigidbody(component2, other);
				}
			}
		}
		else
		{
			RemoveRigidbody(component2, other);
		}
	}

	private void RemoveRigidbody(Rigidbody rb, Collider other)
	{
		if (!rbs.ContainsKey(rb))
		{
			return;
		}
		rbs[rb]--;
		if (rbs[rb] > 0)
		{
			return;
		}
		rbs.Remove(rb);
		if (rb.gameObject == MonoSingleton<NewMovement>.Instance.gameObject)
		{
			playerTouchingWater = false;
		}
		if ((bool)other)
		{
			ParticleSystem[] componentsInChildren = other.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				GameObject gameObject = componentsInChildren[i].gameObject;
				if (bubblesEffects.Remove(gameObject))
				{
					Object.Destroy(gameObject);
				}
			}
		}
		if ((bool)other && other.gameObject.CompareTag("Player"))
		{
			contSplashables.Remove(other);
			return;
		}
		Transform transform = other.transform;
		GameObject gameObject2 = other.gameObject;
		Vector3 position = transform.position;
		int layer = other.gameObject.layer;
		AudioSource[] array = null;
		if ((bool)other)
		{
			array = ((layer != 0 || !gameObject2.TryGetComponent<BloodUnderwaterChecker>(out var _)) ? other.GetComponentsInChildren<AudioSource>() : transform.parent.GetComponentsInChildren<AudioSource>());
		}
		else if ((bool)rb)
		{
			array = rb.GetComponentsInChildren<AudioSource>();
		}
		AudioSource[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			AudioLowPassFilter component2 = array2[i].GetComponent<AudioLowPassFilter>();
			if ((bool)component2)
			{
				Object.Destroy(component2);
			}
		}
		if ((bool)other)
		{
			Vector3 vector = Vector3.positiveInfinity;
			float num = float.PositiveInfinity;
			for (int j = 0; j < colliders.Length; j++)
			{
				Collider collider = colliders[j];
				Vector3 position2 = new Vector3(position.x, collider.bounds.max.y, position.z);
				Vector3 vector2 = collider.ClosestPointOnBounds(position2);
				float num2 = Vector3.Distance(vector2, position);
				if (num2 < num)
				{
					vector = vector2;
					num = num2;
				}
			}
			if (Vector3.Distance(vector, other.ClosestPoint(vector)) < 1f)
			{
				if (rb.velocity.y > 25f && rb.mass >= 1f && base.gameObject.layer != 10)
				{
					Object.Instantiate(splash, vector + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.up));
				}
				else if (rb.velocity.y > 10f)
				{
					Object.Instantiate(smallSplash, vector + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.up));
				}
			}
			if (layer != 10 && layer != 9 && !notWet)
			{
				Wet component3 = other.GetComponent<Wet>();
				if (!component3)
				{
					gameObject2.AddComponent<Wet>();
				}
				else
				{
					component3.Dry();
				}
			}
		}
		else
		{
			if (!rb || notWet)
			{
				return;
			}
			GameObject gameObject3 = rb.gameObject;
			if (gameObject3.layer != 10 && gameObject3.layer != 9)
			{
				Wet component4 = rb.GetComponent<Wet>();
				if (!component4)
				{
					gameObject3.AddComponent<Wet>();
				}
				else
				{
					component4.Dry();
				}
			}
		}
	}

	public void EnterDryZone(Collider other)
	{
		if (enteredColliders.Contains(other))
		{
			Exit(other);
		}
	}

	public void ExitDryZone(Collider other)
	{
		if (enteredColliders.Contains(other))
		{
			Enter(other);
		}
	}

	public void UpdateColor(Color newColor)
	{
		clr = newColor;
		if (inWater)
		{
			MonoSingleton<UnderwaterController>.Instance.UpdateColor(newColor);
		}
	}

	private Vector3 GetClosestPointOnSurface(Collider target, out bool didHit)
	{
		didHit = false;
		_ = target.bounds;
		float y = target.bounds.min.y;
		float y2 = target.bounds.max.y;
		Vector3 position = target.transform.position;
		for (int i = 0; i < colliders.Length; i++)
		{
			Collider collider = colliders[i];
			if (Vector3.Distance(collider.ClosestPoint(position), position) < 1f)
			{
				Vector3 origin = position;
				origin.y = collider.bounds.max.y + 0.1f;
				if (y2 >= origin.y && Physics.Raycast(origin, Vector3.down, out var hitInfo, Mathf.Abs(origin.y - y), 16, QueryTriggerInteraction.Collide))
				{
					didHit = true;
					return hitInfo.point;
				}
			}
		}
		return Vector3.one * 9999f;
	}
}

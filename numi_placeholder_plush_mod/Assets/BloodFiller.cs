using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BloodFiller : MonoBehaviour
{
	public float fullFillThreshold = 1f;

	public float fillSpeed = 1f;

	public float fillTimePerHit = 0.5f;

	public float fillAmount;

	[HideInInspector]
	public bool fullyFilled;

	private AudioSource aud;

	private Bounds meshBounds;

	private Renderer rend;

	private MaterialPropertyBlock propBlock;

	private MeshFilter mf;

	private Collider col;

	public GameObject bloodIngestParticle;

	private float heartBeatCooldown;

	public UltrakillEvent onFullyFilled;

	private List<int> eids = new List<int>();

	private List<float> eidCooldowns = new List<float>();

	private List<float> eidAmounts = new List<float>();

	private void Start()
	{
		aud = GetComponent<AudioSource>();
		rend = GetComponent<Renderer>();
		propBlock = new MaterialPropertyBlock();
		mf = GetComponent<MeshFilter>();
		col = GetComponent<Collider>();
		meshBounds = mf.mesh.bounds;
		Vector4 value = meshBounds.size;
		Vector4 value2 = meshBounds.center;
		value2.w = 1f;
		propBlock.SetVector("_MeshScale", value);
		propBlock.SetVector("_MeshCenter", value2);
		propBlock.SetFloat("_FillAmount", fillAmount);
		rend.SetPropertyBlock(propBlock);
	}

	private void OnEnable()
	{
		MonoSingleton<BloodsplatterManager>.Instance.bloodFillers.Add(base.gameObject);
		MonoSingleton<BloodsplatterManager>.Instance.hasBloodFillers = true;
	}

	private void OnDisable()
	{
		if (base.gameObject.scene.isLoaded)
		{
			MonoSingleton<BloodsplatterManager>.Instance.bloodFillers.Remove(base.gameObject);
			if (MonoSingleton<BloodsplatterManager>.Instance.bloodFillers.Count == 0)
			{
				MonoSingleton<BloodsplatterManager>.Instance.hasBloodFillers = false;
			}
		}
	}

	private void Update()
	{
		if (fillAmount > 0f && !fullyFilled)
		{
			heartBeatCooldown = Mathf.MoveTowards(heartBeatCooldown, 0f, Time.deltaTime * Mathf.Max(0.25f, 3f * fillAmount));
			if (heartBeatCooldown <= 0f)
			{
				aud.Play();
				aud.pitch = Mathf.Lerp(1f, 1.5f, fillAmount);
				heartBeatCooldown = 1f;
			}
		}
		if (eidCooldowns.Count <= 0)
		{
			return;
		}
		for (int num = eidCooldowns.Count - 1; num >= 0; num--)
		{
			eidCooldowns[num] = Mathf.MoveTowards(eidCooldowns[num], 0f, Time.deltaTime);
			if (eidCooldowns[num] == 0f)
			{
				eidCooldowns.RemoveAt(num);
				eidAmounts.RemoveAt(num);
				eids.RemoveAt(num);
			}
		}
	}

	public void FillBloodSlider(float amount, Vector3 position, int eidID = 0)
	{
		if (fullyFilled || (eidID != 0 && eids.Contains(eidID) && !(eidAmounts[eids.IndexOf(eidID)] < amount)))
		{
			return;
		}
		if ((bool)bloodIngestParticle)
		{
			Vector3 vector = new Vector3(base.transform.position.x, position.y, base.transform.position.z);
			GameObject obj = Object.Instantiate(bloodIngestParticle, vector, Quaternion.LookRotation(position - vector));
			ParticleSystem componentInChildren = obj.GetComponentInChildren<ParticleSystem>();
			if ((bool)componentInChildren && componentInChildren.emission.burstCount > 0)
			{
				ParticleSystem.Burst burst = componentInChildren.emission.GetBurst(0);
				burst.count = new ParticleSystem.MinMaxCurve(Mathf.Max(3f, burst.count.constantMin * (amount / 50f)), Mathf.Max(3f, burst.count.constantMax * (amount / 50f)));
				componentInChildren.emission.SetBurst(0, burst);
			}
			AudioSource component = obj.GetComponent<AudioSource>();
			if ((bool)component)
			{
				component.pitch = Mathf.Lerp(3f, 2f, amount / 50f);
				component.volume = Mathf.Lerp(0.5f, 1f, amount / 50f);
			}
		}
		if (eidID != 0)
		{
			if (!eids.Contains(eidID))
			{
				eids.Add(eidID);
				eidCooldowns.Add(0.5f);
				eidAmounts.Add(amount);
			}
			else
			{
				int index = eids.IndexOf(eidID);
				amount -= eidAmounts[index];
				eidAmounts[index] += amount;
				eidCooldowns[index] = 0.5f;
			}
		}
		StartCoroutine(FillBlood(amount));
	}

	private IEnumerator FillBlood(float amount)
	{
		Vector4 value = meshBounds.size;
		Vector4 value2 = meshBounds.center;
		propBlock.SetVector("_MeshScale", value);
		propBlock.SetVector("_MeshCenter", value2);
		rend.SetPropertyBlock(propBlock);
		float timer = 0f;
		float initialFillAmount = fillAmount;
		amount *= fillSpeed;
		while (timer <= fillTimePerHit)
		{
			float t = timer / fillTimePerHit;
			float num = fillAmount;
			float num2 = Mathf.Lerp(initialFillAmount, initialFillAmount + amount * 0.01f, t);
			fillAmount += Mathf.Clamp01(num2 - num);
			propBlock.SetFloat("_FillAmount", fillAmount);
			rend.SetPropertyBlock(propBlock);
			timer += Time.deltaTime;
			if (fillAmount >= fullFillThreshold)
			{
				FullyFilled();
				timer = fillTimePerHit + 1f;
			}
			yield return null;
		}
	}

	private void FullyFilled()
	{
		fullyFilled = true;
		fillAmount = 1f;
		onFullyFilled?.Invoke();
		MonoSingleton<CameraController>.Instance.CameraShake(1f);
		propBlock.SetFloat("_FillAmount", fillAmount);
		rend.SetPropertyBlock(propBlock);
	}

	public void InstaFill()
	{
		StartCoroutine(FillBlood(9999f));
	}
}

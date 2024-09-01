using Sandbox;
using UnityEngine;

public class HookPoint : MonoBehaviour, IAlter, IAlterOptions<float>
{
	public bool active = true;

	public hookPointType type;

	public float slingShotForce;

	[HideInInspector]
	public bool valuesSet;

	public MeshRenderer[] renderers;

	[HideInInspector]
	public Material[] origMats;

	[HideInInspector]
	public Spin[] spins;

	[HideInInspector]
	public Light lit;

	public Transform outerOrb;

	public Transform innerOrb;

	public Material disabledMaterial;

	public ParticleSystem activeParticle;

	private bool hooked;

	private AudioSource aud;

	public GameObject grabParticle;

	public GameObject reachParticle;

	public float reactivationTime = 6f;

	[HideInInspector]
	public float timer;

	private float tickTimer;

	public AudioSource reactivationTick;

	[Header("Events")]
	public UltrakillEvent onHook;

	public UltrakillEvent onUnhook;

	public UltrakillEvent onReach;

	public string alterKey => "hook-point";

	public string alterCategoryName => "Hook Point";

	public AlterOption<float>[] options
	{
		get
		{
			if (type != hookPointType.Slingshot)
			{
				return null;
			}
			return new AlterOption<float>[1]
			{
				new AlterOption<float>
				{
					key = "force",
					name = "Force",
					value = slingShotForce,
					callback = delegate(float value)
					{
						slingShotForce = value;
					},
					constraints = new SliderConstraints
					{
						step = 5f,
						min = -50f,
						max = 200f
					}
				}
			};
		}
	}

	private void Start()
	{
		aud = GetComponent<AudioSource>();
		if (!valuesSet)
		{
			SetValues();
		}
		if (!active)
		{
			TurnOff();
		}
		else if (activeParticle != null)
		{
			activeParticle.Play();
		}
	}

	private void Update()
	{
		if (timer > 0f)
		{
			timer = Mathf.MoveTowards(timer, 0f, Time.deltaTime);
			tickTimer = Mathf.MoveTowards(tickTimer, 0f, Time.deltaTime);
			if (tickTimer == 0f)
			{
				Object.Instantiate(reactivationTick, base.transform.position, Quaternion.identity);
				if (timer > 3f)
				{
					tickTimer = 1f;
				}
				else if (timer > 1f)
				{
					tickTimer = 0.5f;
				}
				else
				{
					tickTimer = 0.25f;
				}
			}
			if (timer <= 0f)
			{
				TimerStop();
			}
		}
		Vector3 vector;
		Vector3 vector2;
		if (active && Vector3.Distance(MonoSingleton<HookArm>.Instance.transform.position, base.transform.position) < 5f && !hooked)
		{
			vector = Vector3.one * 2.5f;
			vector2 = Vector3.zero;
		}
		else if (active && hooked)
		{
			vector = Vector3.zero;
			vector2 = Vector3.one * 3.5f;
		}
		else
		{
			vector = Vector3.one * 5f;
			vector2 = Vector3.one * 3.5f;
		}
		if (outerOrb.localScale != vector)
		{
			outerOrb.localScale = Vector3.MoveTowards(outerOrb.localScale, vector, Time.deltaTime * 50f);
		}
		if (innerOrb.localScale != vector2)
		{
			innerOrb.localScale = Vector3.MoveTowards(innerOrb.localScale, vector2, Time.deltaTime * 50f);
		}
		if (hooked)
		{
			aud.pitch = 0.75f;
		}
		else
		{
			aud.pitch = Mathf.Max(0.5f, outerOrb.localScale.x / 5f) / 2f;
		}
	}

	public void Hooked()
	{
		if (!valuesSet)
		{
			SetValues();
		}
		hooked = true;
		Spin[] array = spins;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].speed = 450f;
		}
		lit.range = 20f;
		Object.Instantiate(grabParticle, base.transform.position, Quaternion.identity);
		onHook.Invoke();
	}

	public void Unhooked()
	{
		hooked = false;
		Spin[] array = spins;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].speed = 100f;
		}
		lit.range = 10f;
		onUnhook.Invoke();
	}

	public void Reached()
	{
		Reached(Vector3.zero);
	}

	public void Reached(Vector3 direction)
	{
		onReach?.Invoke();
		if ((bool)reachParticle)
		{
			if (direction == Vector3.zero)
			{
				direction = base.transform.position - MonoSingleton<CameraController>.Instance.transform.position;
			}
			Object.Instantiate(reachParticle, base.transform.position, Quaternion.LookRotation(direction));
		}
	}

	public void SwitchPulled()
	{
		onReach.Invoke();
		Deactivate();
		timer = reactivationTime;
		tickTimer = 0f;
	}

	public void Activate()
	{
		if (!valuesSet)
		{
			SetValues();
		}
		if (!active)
		{
			TurnOn();
		}
	}

	public void Deactivate()
	{
		if (!valuesSet)
		{
			SetValues();
		}
		if (active)
		{
			TurnOff();
		}
	}

	private void TurnOn()
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material = origMats[i];
		}
		activeParticle.Play();
		lit.enabled = true;
		aud.Play();
		Spin[] array = spins;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].enabled = true;
		}
		active = true;
	}

	private void TurnOff()
	{
		MeshRenderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].material = disabledMaterial;
		}
		activeParticle.Stop();
		activeParticle.Clear();
		lit.enabled = false;
		aud.Stop();
		Spin[] array2 = spins;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		active = false;
	}

	private void SetValues()
	{
		origMats = new Material[renderers.Length];
		for (int i = 0; i < renderers.Length; i++)
		{
			origMats[i] = new Material(renderers[i].material);
		}
		spins = GetComponentsInChildren<Spin>();
		lit = GetComponent<Light>();
		valuesSet = true;
	}

	public void TimerStop()
	{
		timer = 0f;
		Object.Instantiate(reactivationTick, base.transform.position, Quaternion.identity).pitch = 3f;
		onReach.Revert();
		Activate();
	}
}

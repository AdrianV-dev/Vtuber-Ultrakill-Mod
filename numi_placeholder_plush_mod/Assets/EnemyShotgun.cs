using UnityEngine;

public class EnemyShotgun : MonoBehaviour, IEnemyWeapon
{
	private EnemyTarget target;

	public EnemyType safeEnemyType;

	private AudioSource gunAud;

	public AudioClip shootSound;

	public AudioClip clickSound;

	public AudioClip smackSound;

	private AudioSource heatSinkAud;

	public int variation;

	public GameObject bullet;

	public GameObject grenade;

	public float spread;

	private Animator anim;

	public bool gunReady = true;

	public Transform shootPoint;

	public GameObject muzzleFlash;

	private ParticleSystem[] parts;

	private bool charging;

	private AudioSource chargeSound;

	private float chargeAmount;

	public GameObject warningFlash;

	private int difficulty;

	private EnemyIdentifier eid;

	private float speedMultiplier = 1f;

	private float damageMultiplier = 1f;

	private void Start()
	{
		gunAud = GetComponent<AudioSource>();
		anim = GetComponentInChildren<Animator>();
		parts = GetComponentsInChildren<ParticleSystem>();
		heatSinkAud = shootPoint.GetComponent<AudioSource>();
		chargeSound = base.transform.GetChild(0).GetComponent<AudioSource>();
		difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		eid = GetComponentInParent<EnemyIdentifier>();
		if (difficulty == 1)
		{
			spread *= 0.75f;
		}
		else if (difficulty == 0)
		{
			spread *= 0.5f;
		}
	}

	private void Update()
	{
		if (charging)
		{
			float num = 2f;
			if (difficulty == 1)
			{
				num = 1.5f;
			}
			if (difficulty == 0)
			{
				num = 1f;
			}
			chargeAmount = Mathf.MoveTowards(chargeAmount, 1f, Time.deltaTime * num * speedMultiplier);
			chargeSound.pitch = chargeAmount * 1.25f;
		}
	}

	public void UpdateTarget(EnemyTarget target)
	{
		this.target = target;
	}

	public void Fire()
	{
		if (target == null)
		{
			return;
		}
		gunReady = false;
		int num = 12;
		anim.SetTrigger("Shoot");
		Vector3 position = shootPoint.position;
		if (Vector3.Distance(base.transform.position, eid.transform.position) > Vector3.Distance(target.position, eid.transform.position))
		{
			position = new Vector3(eid.transform.position.x, base.transform.position.y, eid.transform.position.z);
		}
		GameObject gameObject = new GameObject();
		gameObject.AddComponent<ProjectileSpread>();
		gameObject.transform.position = base.transform.position;
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject2;
			if (i == 0)
			{
				gameObject2 = Object.Instantiate(bullet, position, shootPoint.rotation, gameObject.transform);
			}
			else
			{
				Quaternion rotation = shootPoint.rotation * Quaternion.Euler(Random.Range(0f - spread, spread), Random.Range(0f - spread, spread), Random.Range(0f - spread, spread));
				gameObject2 = Object.Instantiate(bullet, position, rotation, gameObject.transform);
			}
			if (gameObject2.TryGetComponent<Projectile>(out var component))
			{
				component.target = target;
				component.safeEnemyType = safeEnemyType;
				if (difficulty == 1)
				{
					component.speed *= 0.75f;
				}
				else if (difficulty == 0)
				{
					component.speed *= 0.5f;
				}
				component.damage *= damageMultiplier;
				component.spreaded = true;
			}
		}
		gunAud.clip = shootSound;
		gunAud.volume = 0.35f;
		gunAud.panStereo = 0f;
		gunAud.pitch = Random.Range(0.95f, 1.05f);
		gunAud.Play();
		Object.Instantiate(muzzleFlash, shootPoint.position, shootPoint.rotation);
	}

	public void AltFire()
	{
		if (target == null)
		{
			CancelAltCharge();
			return;
		}
		gunReady = false;
		float num = 70f;
		if (difficulty == 1)
		{
			num = 50f;
		}
		else if (difficulty == 0)
		{
			num = 30f;
		}
		if (!(shootPoint == null))
		{
			Vector3 position = shootPoint.position;
			if (Vector3.Distance(base.transform.position, eid.transform.position) > Vector3.Distance(target.position, eid.transform.position))
			{
				position = new Vector3(eid.transform.position.x, base.transform.position.y, eid.transform.position.z);
			}
			GameObject obj = Object.Instantiate(grenade, position, Random.rotation);
			obj.GetComponent<Rigidbody>().AddForce(shootPoint.forward * num, ForceMode.VelocityChange);
			Grenade componentInChildren = obj.GetComponentInChildren<Grenade>();
			if (componentInChildren != null)
			{
				componentInChildren.enemy = true;
			}
			anim.SetTrigger("Secondary Fire");
			gunAud.clip = shootSound;
			gunAud.volume = 0.35f;
			gunAud.panStereo = 0f;
			gunAud.pitch = Random.Range(0.75f, 0.85f);
			gunAud.Play();
			Object.Instantiate(muzzleFlash, shootPoint.position, shootPoint.rotation);
			CancelAltCharge();
		}
	}

	public void PrepareFire()
	{
		if (heatSinkAud == null)
		{
			heatSinkAud = shootPoint.GetComponent<AudioSource>();
		}
		heatSinkAud.Play();
		Object.Instantiate(warningFlash, shootPoint.position, shootPoint.rotation).transform.localScale *= 2f;
	}

	public void PrepareAltFire()
	{
		if (chargeSound == null)
		{
			chargeSound = base.transform.GetChild(0).GetComponent<AudioSource>();
		}
		charging = true;
		chargeAmount = 0f;
		chargeSound.pitch = 0f;
	}

	public void CancelAltCharge()
	{
		if (chargeSound == null)
		{
			chargeSound = base.transform.GetChild(0).GetComponent<AudioSource>();
		}
		charging = false;
		chargeAmount = 0f;
		chargeSound.pitch = 0f;
	}

	public void ReleaseHeat()
	{
		ParticleSystem[] array = parts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
	}

	public void ClickSound()
	{
		gunAud.clip = clickSound;
		gunAud.volume = 0.5f;
		gunAud.pitch = Random.Range(0.95f, 1.05f);
		gunAud.Play();
	}

	public void ReadyGun()
	{
		gunReady = true;
	}

	public void Smack()
	{
		gunAud.clip = smackSound;
		gunAud.volume = 0.75f;
		gunAud.pitch = Random.Range(2f, 2.2f);
		gunAud.Play();
	}

	public void UpdateBuffs(EnemyIdentifier eid)
	{
		speedMultiplier = eid.totalSpeedModifier;
		damageMultiplier = eid.totalDamageModifier;
	}
}

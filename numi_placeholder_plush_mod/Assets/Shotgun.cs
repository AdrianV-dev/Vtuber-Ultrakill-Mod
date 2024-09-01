using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shotgun : MonoBehaviour
{
	private InputManager inman;

	private WeaponIdentifier wid;

	private AudioSource gunAud;

	public AudioClip shootSound;

	public AudioClip shootSound2;

	public AudioClip clickSound;

	public AudioClip clickChargeSound;

	public AudioClip smackSound;

	public AudioClip pump1sound;

	public AudioClip pump2sound;

	public int variation;

	public GameObject bullet;

	public GameObject grenade;

	public float spread;

	private bool smallSpread;

	private Animator anim;

	private GameObject cam;

	private CameraController cc;

	private GunControl gc;

	private bool gunReady;

	public Transform[] shootPoints;

	public GameObject muzzleFlash;

	public SkinnedMeshRenderer heatSinkSMR;

	private Color tempColor;

	private bool releasingHeat;

	[SerializeField]
	private ParticleSystem[] heatReleaseParticles;

	private AudioSource heatSinkAud;

	public LayerMask shotgunZoneLayerMask;

	private RaycastHit[] rhits;

	private bool charging;

	private float grenadeForce;

	private Vector3 grenadeVector;

	private Slider chargeSlider;

	public Image sliderFill;

	public GameObject grenadeSoundBubble;

	public GameObject chargeSoundBubble;

	private AudioSource tempChargeSound;

	[HideInInspector]
	public int primaryCharge;

	private bool cockedBack;

	public GameObject explosion;

	public GameObject pumpChargeSound;

	public GameObject warningBeep;

	private float timeToBeep;

	[SerializeField]
	private Chainsaw chainsaw;

	private List<Chainsaw> currentChainsaws = new List<Chainsaw>();

	[SerializeField]
	private Transform chainsawAttachPoint;

	[SerializeField]
	private ScrollingTexture chainsawBladeScroll;

	private MeshRenderer chainsawBladeRenderer;

	private Material chainsawBladeMaterial;

	[SerializeField]
	private Material chainsawBladeMotionMaterial;

	[SerializeField]
	private HurtZone sawZone;

	[SerializeField]
	private ParticleSystem environmentalSawSpark;

	[SerializeField]
	private AudioSource environmentalSawSound;

	private WeaponPos wpos;

	private CameraFrustumTargeter targeter;

	private bool meterOverride;

	private void Start()
	{
		targeter = Camera.main.GetComponent<CameraFrustumTargeter>();
		inman = MonoSingleton<InputManager>.Instance;
		wid = GetComponent<WeaponIdentifier>();
		gunAud = GetComponent<AudioSource>();
		anim = GetComponentInChildren<Animator>();
		cam = MonoSingleton<CameraController>.Instance.gameObject;
		cc = MonoSingleton<CameraController>.Instance;
		gc = GetComponentInParent<GunControl>();
		tempColor = heatSinkSMR.materials[3].GetColor("_TintColor");
		heatSinkAud = heatSinkSMR.GetComponent<AudioSource>();
		chargeSlider = GetComponentInChildren<Slider>();
		sliderFill = chargeSlider.GetComponentInChildren<Image>();
		if (variation == 0)
		{
			chargeSlider.value = chargeSlider.maxValue;
		}
		else if (variation == 1)
		{
			chargeSlider.value = 0f;
		}
		wpos = GetComponent<WeaponPos>();
		if ((bool)chainsawBladeScroll)
		{
			chainsawBladeRenderer = chainsawBladeScroll.GetComponent<MeshRenderer>();
			chainsawBladeMaterial = chainsawBladeRenderer.sharedMaterial;
		}
		if ((bool)sawZone)
		{
			sawZone.sourceWeapon = base.gameObject;
		}
	}

	private void OnEnable()
	{
		if (variation != 2)
		{
			return;
		}
		foreach (Chainsaw currentChainsaw in currentChainsaws)
		{
			currentChainsaw.lineStartTransform = chainsawAttachPoint;
		}
		chainsawAttachPoint.gameObject.SetActive(MonoSingleton<WeaponCharges>.Instance.shoSawCharge == 1f);
	}

	private void OnDisable()
	{
		if (!base.gameObject.scene.isLoaded)
		{
			return;
		}
		if (anim == null)
		{
			anim = GetComponentInChildren<Animator>();
		}
		anim.StopPlayback();
		gunReady = false;
		if (sliderFill != null && (bool)MonoSingleton<ColorBlindSettings>.Instance)
		{
			sliderFill.color = MonoSingleton<ColorBlindSettings>.Instance.variationColors[variation];
		}
		if (chargeSlider == null)
		{
			chargeSlider = GetComponentInChildren<Slider>();
		}
		if (variation == 0)
		{
			chargeSlider.value = chargeSlider.maxValue;
		}
		else if (variation == 1)
		{
			chargeSlider.value = 0f;
		}
		if (sliderFill == null)
		{
			sliderFill = chargeSlider.GetComponentInChildren<Image>();
		}
		primaryCharge = 0;
		charging = false;
		grenadeForce = 0f;
		meterOverride = false;
		if (tempChargeSound != null)
		{
			Object.Destroy(tempChargeSound);
		}
		foreach (Chainsaw currentChainsaw in currentChainsaws)
		{
			currentChainsaw.lineStartTransform = MonoSingleton<NewMovement>.Instance.transform;
		}
		if ((bool)sawZone)
		{
			sawZone.enabled = false;
		}
		if ((bool)environmentalSawSound)
		{
			environmentalSawSound.Stop();
		}
		if ((bool)environmentalSawSpark)
		{
			environmentalSawSpark.Stop();
		}
	}

	private void Update()
	{
		if (!MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && MonoSingleton<InputManager>.Instance.InputSource.Fire1.IsPressed && gunReady && gc.activated && !GameStateManager.Instance.PlayerInputLocked && !charging)
		{
			if (!wid || wid.delay == 0f)
			{
				Shoot();
			}
			else
			{
				gunReady = false;
				Invoke("Shoot", wid.delay);
			}
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.IsPressed && variation == 1 && gunReady && gc.activated && !GameStateManager.Instance.PlayerInputLocked)
		{
			gunReady = false;
			if (!wid || wid.delay == 0f)
			{
				Pump();
			}
			else
			{
				Invoke("Pump", wid.delay);
			}
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.IsPressed && variation != 1 && gunReady && gc.activated && !GameStateManager.Instance.PlayerInputLocked && (variation != 2 || MonoSingleton<WeaponCharges>.Instance.shoSawCharge >= 1f))
		{
			charging = true;
			if (grenadeForce < 60f)
			{
				grenadeForce = Mathf.MoveTowards(grenadeForce, 60f, Time.deltaTime * 60f);
			}
			grenadeVector = new Vector3(cam.transform.forward.x, cam.transform.forward.y, cam.transform.forward.z);
			if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
			{
				grenadeVector = Vector3.Normalize(targeter.CurrentTarget.bounds.center - cam.transform.position);
			}
			grenadeVector += new Vector3(0f, grenadeForce * 0.002f, 0f);
			float num = 3000f;
			if (variation == 2)
			{
				num = 12000f;
			}
			base.transform.localPosition = new Vector3(wpos.currentDefault.x + Random.Range(grenadeForce / num * -1f, grenadeForce / num), wpos.currentDefault.y + Random.Range(grenadeForce / num * -1f, grenadeForce / num), wpos.currentDefault.z + Random.Range(grenadeForce / num * -1f, grenadeForce / num));
			if (tempChargeSound == null)
			{
				GameObject gameObject = Object.Instantiate(chargeSoundBubble);
				tempChargeSound = gameObject.GetComponent<AudioSource>();
				if ((bool)wid && wid.delay > 0f)
				{
					tempChargeSound.volume -= wid.delay * 2f;
					if (tempChargeSound.volume < 0f)
					{
						tempChargeSound.volume = 0f;
					}
				}
			}
			MonoSingleton<RumbleManager>.Instance.SetVibrationTracked(RumbleProperties.ShotgunCharge, tempChargeSound.gameObject).intensityMultiplier = grenadeForce / 60f;
			if (variation == 0)
			{
				tempChargeSound.pitch = grenadeForce / 60f;
			}
			else
			{
				tempChargeSound.pitch = (grenadeForce / 2f + 30f) / 60f;
			}
		}
		if ((MonoSingleton<InputManager>.Instance.InputSource.Fire2.WasCanceledThisFrame || (!MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && !GameStateManager.Instance.PlayerInputLocked && MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame)) && variation != 1 && gunReady && gc.activated && charging)
		{
			charging = false;
			if (variation == 2)
			{
				MonoSingleton<WeaponCharges>.Instance.shoSawCharge = 0f;
			}
			if (!wid || wid.delay == 0f)
			{
				if (variation == 0)
				{
					ShootSinks();
				}
				else
				{
					ShootSaw();
				}
			}
			else
			{
				gunReady = false;
				Invoke((variation == 0) ? "ShootSinks" : "ShootSaw", wid.delay);
			}
			Object.Destroy(tempChargeSound.gameObject);
		}
		if (variation == 2)
		{
			if (charging && chainsawBladeScroll.scrollSpeedX == 0f)
			{
				chainsawBladeRenderer.material = chainsawBladeMotionMaterial;
			}
			else if (!charging && chainsawBladeScroll.scrollSpeedX > 0f)
			{
				chainsawBladeRenderer.material = chainsawBladeMaterial;
			}
			chainsawBladeScroll.scrollSpeedX = grenadeForce / 6f;
			anim.SetBool("Sawing", charging);
			sawZone.enabled = charging;
			if (charging && Physics.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward, out var hitInfo, 3f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
			{
				environmentalSawSpark.transform.position = hitInfo.point;
				if (!environmentalSawSpark.isEmitting)
				{
					environmentalSawSpark.Play();
				}
				if (!environmentalSawSound.isPlaying)
				{
					environmentalSawSound.Play();
				}
				MonoSingleton<CameraController>.Instance.CameraShake(0.1f);
			}
			else
			{
				if (environmentalSawSpark.isEmitting)
				{
					environmentalSawSpark.Stop();
				}
				if (environmentalSawSound.isPlaying)
				{
					environmentalSawSound.Stop();
				}
			}
		}
		if (releasingHeat)
		{
			tempColor.a -= Time.deltaTime * 2.5f;
			heatSinkSMR.sharedMaterials[3].SetColor("_TintColor", tempColor);
		}
		UpdateMeter();
	}

	private void UpdateMeter()
	{
		if (variation == 1)
		{
			if (timeToBeep != 0f)
			{
				timeToBeep = Mathf.MoveTowards(timeToBeep, 0f, Time.deltaTime * 5f);
			}
			if (primaryCharge == 3)
			{
				chargeSlider.value = chargeSlider.maxValue;
				if (timeToBeep == 0f)
				{
					timeToBeep = 1f;
					Object.Instantiate(warningBeep);
					sliderFill.color = Color.red;
				}
				else if (timeToBeep < 0.5f)
				{
					sliderFill.color = Color.black;
				}
			}
			else
			{
				chargeSlider.value = primaryCharge * 20;
				sliderFill.color = Color.Lerp(MonoSingleton<ColorBlindSettings>.Instance.variationColors[1], new Color(1f, 0.25f, 0.25f), (float)primaryCharge / 2f);
			}
		}
		else if (!meterOverride)
		{
			if (variation == 2 && MonoSingleton<WeaponCharges>.Instance.shoSawCharge == 1f && !chainsawAttachPoint.gameObject.activeSelf)
			{
				chainsawAttachPoint.gameObject.SetActive(value: true);
			}
			if (grenadeForce > 0f)
			{
				chargeSlider.value = grenadeForce;
				sliderFill.color = Color.Lerp(MonoSingleton<ColorBlindSettings>.Instance.variationColors[variation], new Color(1f, 0.25f, 0.25f), grenadeForce / 60f);
			}
			else if (variation == 0)
			{
				chargeSlider.value = chargeSlider.maxValue;
				sliderFill.color = MonoSingleton<ColorBlindSettings>.Instance.variationColors[0];
			}
			else
			{
				chargeSlider.value = MonoSingleton<WeaponCharges>.Instance.shoSawCharge * chargeSlider.maxValue;
				sliderFill.color = ((MonoSingleton<WeaponCharges>.Instance.shoSawCharge == 1f) ? MonoSingleton<ColorBlindSettings>.Instance.variationColors[2] : Color.gray);
			}
		}
	}

	private void Shoot()
	{
		gunReady = false;
		int num = 12;
		if (variation == 1)
		{
			switch (primaryCharge)
			{
			case 0:
				num = 10;
				gunAud.pitch = Random.Range(1.15f, 1.25f);
				break;
			case 1:
				num = 16;
				gunAud.pitch = Random.Range(0.95f, 1.05f);
				break;
			case 2:
				num = 24;
				gunAud.pitch = Random.Range(0.75f, 0.85f);
				break;
			case 3:
				num = 0;
				gunAud.pitch = Random.Range(0.75f, 0.85f);
				break;
			}
		}
		MonoSingleton<CameraController>.Instance.StopShake();
		Vector3 direction = cam.transform.forward;
		if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
		{
			direction = (targeter.CurrentTarget.bounds.center - MonoSingleton<CameraController>.Instance.GetDefaultPos()).normalized;
		}
		rhits = Physics.RaycastAll(cam.transform.position, direction, 4f, shotgunZoneLayerMask);
		if (rhits.Length != 0)
		{
			RaycastHit[] array = rhits;
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit = array[i];
				if (!raycastHit.collider.gameObject.CompareTag("Body"))
				{
					continue;
				}
				EnemyIdentifierIdentifier componentInParent = raycastHit.collider.GetComponentInParent<EnemyIdentifierIdentifier>();
				if ((bool)componentInParent && (bool)componentInParent.eid)
				{
					EnemyIdentifier eid = componentInParent.eid;
					if (!eid.dead && !eid.blessed && anim.GetCurrentAnimatorStateInfo(0).IsName("Equip"))
					{
						MonoSingleton<StyleHUD>.Instance.AddPoints(50, "ultrakill.quickdraw", gc.currentWeapon, eid);
					}
					eid.hitter = "shotgunzone";
					if (!eid.hitterWeapons.Contains("shotgun" + variation))
					{
						eid.hitterWeapons.Add("shotgun" + variation);
					}
					eid.DeliverDamage(raycastHit.collider.gameObject, (eid.transform.position - base.transform.position).normalized * 10000f, raycastHit.point, 4f, tryForExplode: false, 0f, base.gameObject);
				}
			}
		}
		MonoSingleton<RumbleManager>.Instance.SetVibrationTracked(RumbleProperties.GunFireProjectiles, base.gameObject);
		if (variation != 1 || primaryCharge != 3)
		{
			for (int j = 0; j < num; j++)
			{
				GameObject gameObject = Object.Instantiate(bullet, cam.transform.position, cam.transform.rotation);
				Projectile component = gameObject.GetComponent<Projectile>();
				component.weaponType = "shotgun" + variation;
				component.sourceWeapon = gc.currentWeapon;
				if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
				{
					gameObject.transform.LookAt(targeter.CurrentTarget.bounds.center);
				}
				if (variation == 1)
				{
					switch (primaryCharge)
					{
					case 0:
						gameObject.transform.Rotate(Random.Range((0f - spread) / 1.5f, spread / 1.5f), Random.Range((0f - spread) / 1.5f, spread / 1.5f), Random.Range((0f - spread) / 1.5f, spread / 1.5f));
						break;
					case 1:
						gameObject.transform.Rotate(Random.Range(0f - spread, spread), Random.Range(0f - spread, spread), Random.Range(0f - spread, spread));
						break;
					case 2:
						gameObject.transform.Rotate(Random.Range((0f - spread) * 2f, spread * 2f), Random.Range((0f - spread) * 2f, spread * 2f), Random.Range((0f - spread) * 2f, spread * 2f));
						break;
					}
				}
				else
				{
					gameObject.transform.Rotate(Random.Range(0f - spread, spread), Random.Range(0f - spread, spread), Random.Range(0f - spread, spread));
				}
			}
		}
		else
		{
			Vector3 position = cam.transform.position + cam.transform.forward;
			if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hitInfo, 1f, LayerMaskDefaults.Get(LMD.Environment)))
			{
				position = hitInfo.point - cam.transform.forward * 0.1f;
			}
			GameObject gameObject2 = Object.Instantiate(explosion, position, cam.transform.rotation);
			if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
			{
				gameObject2.transform.LookAt(targeter.CurrentTarget.bounds.center);
			}
			Explosion[] componentsInChildren = gameObject2.GetComponentsInChildren<Explosion>();
			foreach (Explosion obj in componentsInChildren)
			{
				obj.sourceWeapon = gc.currentWeapon;
				obj.enemyDamageMultiplier = 1f;
				obj.maxSize *= 1.5f;
				obj.damage = 50;
			}
		}
		if (variation != 1)
		{
			gunAud.pitch = Random.Range(0.95f, 1.05f);
		}
		gunAud.clip = shootSound;
		gunAud.volume = 0.45f;
		gunAud.panStereo = 0f;
		gunAud.Play();
		cc.CameraShake(1f);
		if (variation == 1)
		{
			anim.SetTrigger("PumpFire");
		}
		else
		{
			anim.SetTrigger("Fire");
		}
		Transform[] array2 = shootPoints;
		foreach (Transform transform in array2)
		{
			Object.Instantiate(muzzleFlash, transform.transform.position, transform.transform.rotation);
		}
		releasingHeat = false;
		tempColor.a = 1f;
		heatSinkSMR.sharedMaterials[3].SetColor("_TintColor", tempColor);
		if (variation == 1)
		{
			primaryCharge = 0;
		}
	}

	private void ShootSinks()
	{
		gunReady = false;
		base.transform.localPosition = wpos.currentDefault;
		Transform[] array = shootPoints;
		for (int i = 0; i < array.Length; i++)
		{
			_ = array[i];
			GameObject obj = Object.Instantiate(grenade, cam.transform.position + cam.transform.forward * 0.5f, Random.rotation);
			obj.GetComponentInChildren<Grenade>().sourceWeapon = gc.currentWeapon;
			obj.GetComponent<Rigidbody>().AddForce(grenadeVector * (grenadeForce + 10f), ForceMode.VelocityChange);
		}
		Object.Instantiate(grenadeSoundBubble).GetComponent<AudioSource>().volume = 0.45f * Mathf.Sqrt(Mathf.Pow(1f, 2f) - Mathf.Pow(grenadeForce, 2f) / Mathf.Pow(60f, 2f));
		anim.SetTrigger("Secondary Fire");
		gunAud.clip = shootSound;
		gunAud.volume = 0.45f * (grenadeForce / 60f);
		gunAud.panStereo = 0f;
		gunAud.pitch = Random.Range(0.75f, 0.85f);
		gunAud.Play();
		cc.CameraShake(1f);
		meterOverride = true;
		chargeSlider.value = 0f;
		sliderFill.color = Color.black;
		array = shootPoints;
		foreach (Transform transform in array)
		{
			Object.Instantiate(muzzleFlash, transform.transform.position, transform.transform.rotation);
		}
		releasingHeat = false;
		tempColor.a = 0f;
		heatSinkSMR.sharedMaterials[3].SetColor("_TintColor", tempColor);
		grenadeForce = 0f;
	}

	private void ShootSaw()
	{
		gunReady = true;
		base.transform.localPosition = wpos.currentDefault;
		Vector3 vector = cam.transform.forward;
		if ((bool)targeter.CurrentTarget && targeter.IsAutoAimed)
		{
			vector = (targeter.CurrentTarget.bounds.center - MonoSingleton<CameraController>.Instance.GetDefaultPos()).normalized;
		}
		Transform[] array = shootPoints;
		for (int i = 0; i < array.Length; i++)
		{
			_ = array[i];
			Vector3 position = MonoSingleton<CameraController>.Instance.GetDefaultPos() + vector * 0.5f;
			if (Physics.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), vector, out var hitInfo, 5f, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)))
			{
				position = hitInfo.point - vector * 5f;
			}
			Chainsaw chainsaw = Object.Instantiate(this.chainsaw, position, Random.rotation);
			chainsaw.weaponType = "shotgun" + variation;
			chainsaw.CheckMultipleRicochets(onStart: true);
			chainsaw.sourceWeapon = gc.currentWeapon;
			chainsaw.attachedTransform = MonoSingleton<PlayerTracker>.Instance.GetTarget();
			chainsaw.lineStartTransform = chainsawAttachPoint;
			chainsaw.GetComponent<Rigidbody>().AddForce(vector * (grenadeForce + 10f) * 1.5f, ForceMode.VelocityChange);
			currentChainsaws.Add(chainsaw);
		}
		chainsawBladeRenderer.material = chainsawBladeMaterial;
		chainsawBladeScroll.scrollSpeedX = 0f;
		chainsawAttachPoint.gameObject.SetActive(value: false);
		Object.Instantiate(grenadeSoundBubble).GetComponent<AudioSource>().volume = 0.45f * Mathf.Sqrt(Mathf.Pow(1f, 2f) - Mathf.Pow(grenadeForce, 2f) / Mathf.Pow(60f, 2f));
		anim.Play("FireNoReload");
		gunAud.clip = shootSound;
		gunAud.volume = 0.45f * Mathf.Max(0.5f, grenadeForce / 60f);
		gunAud.panStereo = 0f;
		gunAud.pitch = Random.Range(0.75f, 0.85f);
		gunAud.Play();
		cc.CameraShake(1f);
		releasingHeat = false;
		grenadeForce = 0f;
	}

	private void Pump()
	{
		anim.SetTrigger("Pump");
		if (primaryCharge < 3)
		{
			primaryCharge++;
		}
	}

	public void ReleaseHeat()
	{
		releasingHeat = true;
		ParticleSystem[] array = heatReleaseParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		heatSinkAud.Play();
	}

	public void ClickSound()
	{
		if (sliderFill.color != MonoSingleton<ColorBlindSettings>.Instance.variationColors[variation])
		{
			gunAud.clip = clickChargeSound;
		}
		else
		{
			gunAud.clip = clickSound;
		}
		gunAud.volume = 0.5f;
		gunAud.pitch = Random.Range(0.95f, 1.05f);
		gunAud.panStereo = 0.1f;
		gunAud.Play();
	}

	public void ReadyGun()
	{
		gunReady = true;
		meterOverride = false;
	}

	public void Smack()
	{
		gunAud.clip = smackSound;
		gunAud.volume = 0.75f;
		gunAud.pitch = Random.Range(2f, 2.2f);
		gunAud.panStereo = 0.1f;
		gunAud.Play();
	}

	public void SkipShoot()
	{
		anim.ResetTrigger("Fire");
		anim.Play("FireWithReload", -1, 0.05f);
	}

	public void Pump1Sound()
	{
		AudioSource component = Object.Instantiate(grenadeSoundBubble).GetComponent<AudioSource>();
		component.pitch = Random.Range(0.95f, 1.05f);
		component.clip = pump1sound;
		component.volume = 1f;
		component.panStereo = 0.1f;
		component.Play();
		AudioSource component2 = Object.Instantiate(pumpChargeSound).GetComponent<AudioSource>();
		float num = primaryCharge;
		component2.pitch = 1f + num / 5f;
		component2.Play();
	}

	public void Pump2Sound()
	{
		AudioSource component = Object.Instantiate(grenadeSoundBubble).GetComponent<AudioSource>();
		component.pitch = Random.Range(0.95f, 1.05f);
		component.clip = pump2sound;
		component.volume = 1f;
		component.panStereo = 0.1f;
		component.Play();
	}
}

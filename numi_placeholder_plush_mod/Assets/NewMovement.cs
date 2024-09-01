using System;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class NewMovement : MonoSingleton<NewMovement>
{
	[HideInInspector]
	public bool modNoDashSlide;

	[HideInInspector]
	public bool modNoJump;

	[HideInInspector]
	public float modForcedFrictionMultip = 1f;

	private float friction;

	private InputManager inman;

	[HideInInspector]
	public AssistController asscon;

	public float walkSpeed;

	public float jumpPower;

	public float airAcceleration;

	public float wallJumpPower;

	private bool jumpCooldown;

	private bool falling;

	[HideInInspector]
	public Rigidbody rb;

	private Vector3 movementDirection;

	private Vector3 movementDirection2;

	private Vector3 airDirection;

	public float timeBetweenSteps;

	private float stepTime;

	private int currentStep;

	[HideInInspector]
	public Animator anim;

	private Quaternion tempRotation;

	private GameObject forwardPoint;

	public GroundCheck gc;

	public GroundCheck slopeCheck;

	private WallCheck wc;

	private PlayerAnimations pa;

	private Vector3 wallJumpPos;

	public int currentWallJumps;

	private AudioSource aud;

	private AudioSource aud2;

	private AudioSource aud3;

	private int currentSound;

	public AudioClip jumpSound;

	public AudioClip landingSound;

	public AudioClip finalWallJump;

	public bool walking;

	public int hp = 100;

	public float antiHp;

	private float antiHpCooldown;

	public Image hurtScreen;

	private AudioSource hurtAud;

	private Color hurtColor;

	private Color currentColor;

	private bool hurting;

	public bool dead;

	public bool endlessMode;

	public Image blackScreen;

	private Color blackColor;

	public Text youDiedText;

	private Color youDiedColor;

	public FlashImage hpFlash;

	public FlashImage antiHpFlash;

	private AudioSource greenHpAud;

	private float currentAllPitch = 1f;

	private float currentAllVolume;

	public bool boost;

	public Vector3 dodgeDirection;

	private float boostLeft;

	private float dashStorage;

	public float boostCharge = 300f;

	public AudioClip dodgeSound;

	public CameraController cc;

	public GameObject staminaFailSound;

	public GameObject screenHud;

	private Vector3 hudOriginalPos;

	public GameObject dodgeParticle;

	public GameObject scrnBlood;

	private Canvas fullHud;

	public GameObject hudCam;

	private Vector3 camOriginalPos;

	private RigidbodyConstraints defaultRBConstraints;

	private GameObject revolver;

	private StyleHUD shud;

	public GameObject scrapePrefab;

	private GameObject scrapeParticle;

	public LayerMask lmask;

	public StyleCalculator scalc;

	public bool activated;

	public int gamepadFreezeCount;

	private float fallSpeed;

	public bool jumping;

	private float fallTime;

	public GameObject impactDust;

	public GameObject fallParticle;

	private GameObject currentFallParticle;

	[HideInInspector]
	public CapsuleCollider playerCollider;

	public bool sliding;

	private float slideSafety;

	public GameObject slideParticle;

	private GameObject currentSlideParticle;

	private ParticleSystem.TrailModule slideTrail;

	private ParticleSystem.MinMaxGradient normalSlideGradient;

	public ParticleSystem.MinMaxGradient invincibleSlideGradient;

	public GameObject slideScrapePrefab;

	private GameObject slideScrape;

	private Vector3 slideMovDirection;

	public GameObject slideStopSound;

	private bool crouching;

	public bool standing;

	public bool rising;

	private bool slideEnding;

	private Vector3 groundCheckPos;

	public AudioSource oilSlideEffect;

	private GunControl gunc;

	public float currentSpeed;

	private FistControl punch;

	public GameObject dashJumpSound;

	public bool slowMode;

	public Vector3 pushForce;

	private float slideLength;

	[HideInInspector]
	public float longestSlide;

	private float preSlideSpeed;

	private float preSlideDelay;

	public bool quakeJump;

	public GameObject quakeJumpSound;

	[HideInInspector]
	public bool exploded;

	[HideInInspector]
	public float safeExplosionLaunchCooldown;

	private float clingFade;

	public bool stillHolding;

	public float slamForce;

	private bool slamStorage;

	private bool launched;

	private int difficulty;

	[HideInInspector]
	public int sameCheckpointRestarts;

	public CustomGroundProperties groundProperties;

	[HideInInspector]
	public int rocketJumps;

	[HideInInspector]
	public int hammerJumps;

	[HideInInspector]
	public Grenade ridingRocket;

	[HideInInspector]
	public int rocketRides;

	private float ssjMaxFrames = 4f;

	public Light pointLight;

	public TimeSince sinceSlideEnd;

	[HideInInspector]
	public bool levelOver;

	private Vector3Int? lastCheckedGasolineVoxel;

	private int framesSinceSlide;

	private Vector3 velocityAfterSlide;

	protected override void Awake()
	{
		base.Awake();
		rb = GetComponent<Rigidbody>();
		aud = GetComponent<AudioSource>();
		anim = GetComponentInChildren<Animator>();
		wc = GetComponentInChildren<WallCheck>();
		aud2 = gc.GetComponent<AudioSource>();
		pa = GetComponentInChildren<PlayerAnimations>();
		aud3 = wc.GetComponent<AudioSource>();
		cc = GetComponentInChildren<CameraController>();
		playerCollider = GetComponent<CapsuleCollider>();
	}

	private void Start()
	{
		inman = MonoSingleton<InputManager>.Instance;
		asscon = MonoSingleton<AssistController>.Instance;
		if ((bool)hurtScreen)
		{
			hurtColor = hurtScreen.color;
			currentColor = hurtColor;
			currentColor.a = 0f;
			hurtScreen.color = currentColor;
			hurtAud = hurtScreen.GetComponent<AudioSource>();
			blackColor = blackScreen.color;
			youDiedColor = youDiedText.color;
			fullHud = hurtScreen.GetComponentInParent<Canvas>();
		}
		hudOriginalPos = screenHud.transform.localPosition;
		camOriginalPos = hudCam.transform.localPosition;
		currentAllPitch = 1f;
		MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allPitch", 1f);
		MonoSingleton<AudioMixerController>.Instance.doorSound.SetFloat("allPitch", 1f);
		defaultRBConstraints = rb.constraints;
		rb.solverIterations *= 5;
		rb.solverVelocityIterations *= 5;
		groundCheckPos = gc.transform.localPosition;
		scalc = MonoSingleton<StyleCalculator>.Instance;
		difficulty = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty");
		normalSlideGradient = slideParticle.GetComponent<ParticleSystem>().trails.colorOverLifetime;
		if (difficulty == 0 && hp == 100)
		{
			hp = 200;
		}
	}

	public AudioSource DuplicateDetachWhoosh()
	{
		if (!aud3)
		{
			return null;
		}
		float time = aud3.time;
		aud3.enabled = false;
		GameObject obj = UnityEngine.Object.Instantiate(aud3.gameObject, aud3.transform.parent, worldPositionStays: true);
		UnityEngine.Object.Destroy(obj.GetComponent<WallCheck>());
		AudioSource component = obj.GetComponent<AudioSource>();
		component.time = time;
		component.Play();
		return component;
	}

	public AudioSource RestoreWhoosh()
	{
		aud3.enabled = true;
		return aud3;
	}

	private void OnDisable()
	{
		if (sliding)
		{
			StopSlide();
		}
		if ((bool)currentFallParticle)
		{
			UnityEngine.Object.Destroy(currentFallParticle);
		}
		if ((bool)scrapeParticle)
		{
			UnityEngine.Object.Destroy(scrapeParticle);
		}
		Physics.IgnoreLayerCollision(2, 12, ignore: false);
	}

	private void Update()
	{
		if (gc.onGround)
		{
			CheckForGasoline();
		}
		else if (oilSlideEffect.gameObject.activeSelf)
		{
			oilSlideEffect.gameObject.SetActive(value: false);
		}
		Vector2 vector = Vector2.zero;
		if (activated)
		{
			vector = MonoSingleton<InputManager>.Instance.InputSource.Move.ReadValue<Vector2>();
			cc.movementHor = vector.x;
			cc.movementVer = vector.y;
			movementDirection = Vector3.ClampMagnitude(vector.x * base.transform.right + vector.y * base.transform.forward, 1f);
			if (punch == null)
			{
				punch = GetComponentInChildren<FistControl>();
			}
			else if (!punch.enabled)
			{
				punch.YesFist();
			}
		}
		else
		{
			if (currentFallParticle != null)
			{
				UnityEngine.Object.Destroy(currentFallParticle);
			}
			if (currentSlideParticle != null)
			{
				UnityEngine.Object.Destroy(currentSlideParticle);
			}
			else if (slideScrape != null)
			{
				UnityEngine.Object.Destroy(slideScrape);
			}
			if (punch == null)
			{
				punch = GetComponentInChildren<FistControl>();
			}
			else
			{
				punch.NoFist();
			}
		}
		if (MonoSingleton<InputManager>.Instance.LastButtonDevice is Gamepad && gamepadFreezeCount > 0)
		{
			vector = Vector2.zero;
			rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
			cc.movementHor = 0f;
			cc.movementVer = 0f;
			movementDirection = Vector3.zero;
			return;
		}
		if (dead && !endlessMode)
		{
			currentAllPitch -= 0.1f * Time.deltaTime;
			MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allPitch", currentAllPitch);
			MonoSingleton<AudioMixerController>.Instance.doorSound.SetFloat("allPitch", currentAllPitch);
			if (blackColor.a < 0.5f)
			{
				blackColor.a += 0.75f * Time.deltaTime;
				youDiedColor.a += 0.75f * Time.deltaTime;
			}
			else
			{
				blackColor.a += 0.05f * Time.deltaTime;
				youDiedColor.a += 0.05f * Time.deltaTime;
			}
			blackScreen.color = blackColor;
			youDiedText.color = youDiedColor;
		}
		if (gc.onGround != pa.onGround)
		{
			pa.onGround = gc.onGround;
		}
		if (!gc.onGround)
		{
			if (fallTime < 1f)
			{
				fallTime += Time.deltaTime * 5f;
				if (fallTime > 1f)
				{
					falling = true;
				}
			}
			else if (rb.velocity.y < -2f)
			{
				fallSpeed = rb.velocity.y;
			}
		}
		else if (gc.onGround)
		{
			fallTime = 0f;
			clingFade = 0f;
		}
		if (!gc.onGround && rb.velocity.y < -20f)
		{
			aud3.pitch = rb.velocity.y * -1f / 120f;
			if (activated)
			{
				aud3.volume = rb.velocity.y * -1f / 80f;
			}
			else
			{
				aud3.volume = rb.velocity.y * -1f / 240f;
			}
		}
		else if (rb.velocity.y > -20f)
		{
			aud3.pitch = 0f;
			aud3.volume = 0f;
		}
		if (rb.velocity.y < -100f)
		{
			rb.velocity = new Vector3(rb.velocity.x, -100f, rb.velocity.z);
		}
		if (gc.onGround && falling && !jumpCooldown)
		{
			falling = false;
			slamStorage = false;
			if (fallSpeed > -50f)
			{
				aud2.clip = landingSound;
				aud2.volume = 0.5f + fallSpeed * -0.01f;
				aud2.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				aud2.Play();
			}
			else
			{
				UnityEngine.Object.Instantiate(impactDust, gc.transform.position, Quaternion.identity).transform.forward = Vector3.up;
				cc.CameraShake(0.5f);
				MonoSingleton<RumbleManager>.Instance.SetVibration(RumbleProperties.FallImpact);
			}
			fallSpeed = 0f;
			gc.heavyFall = false;
			if (currentFallParticle != null)
			{
				UnityEngine.Object.Destroy(currentFallParticle);
			}
		}
		if (!gc.onGround && activated && MonoSingleton<InputManager>.Instance.InputSource.Slide.WasPerformedThisFrame && !GameStateManager.Instance.PlayerInputLocked)
		{
			if (sliding)
			{
				StopSlide();
			}
			if (boost)
			{
				boostLeft = 0f;
				boost = false;
			}
			if (fallTime > 0.5f && !Physics.Raycast(gc.transform.position + base.transform.up, base.transform.up * -1f, out var _, 3f, lmask) && !gc.heavyFall)
			{
				stillHolding = true;
				rb.velocity = new Vector3(0f, -100f, 0f);
				falling = true;
				fallSpeed = -100f;
				gc.heavyFall = true;
				slamForce = 1f;
				if (currentFallParticle != null)
				{
					UnityEngine.Object.Destroy(currentFallParticle);
				}
				currentFallParticle = UnityEngine.Object.Instantiate(fallParticle, base.transform);
			}
		}
		if (gc.heavyFall && !slamStorage)
		{
			rb.velocity = new Vector3(0f, -100f, 0f);
		}
		if (gc.heavyFall || sliding)
		{
			Physics.IgnoreLayerCollision(2, 12, ignore: true);
		}
		else
		{
			Physics.IgnoreLayerCollision(2, 12, ignore: false);
		}
		if (!slopeCheck.onGround && slopeCheck.forcedOff <= 0 && modForcedFrictionMultip != 0f && !jumping && !boost)
		{
			float num = playerCollider.height / 2f - playerCollider.center.y;
			if (rb.velocity != Vector3.zero && Physics.Raycast(base.transform.position, base.transform.up * -1f, out var hitInfo2, num + 1f, lmask, QueryTriggerInteraction.Ignore))
			{
				Vector3 target = new Vector3(base.transform.position.x, base.transform.position.y - hitInfo2.distance + num, base.transform.position.z);
				base.transform.position = Vector3.MoveTowards(base.transform.position, target, hitInfo2.distance * Time.deltaTime * 10f);
				if (rb.velocity.y > 0f)
				{
					rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
				}
			}
		}
		if (gc.heavyFall)
		{
			slamForce += Time.deltaTime * 5f;
			if (Physics.Raycast(gc.transform.position + base.transform.up, base.transform.up * -1f, out var hitInfo3, 5f, lmask) || Physics.SphereCast(gc.transform.position + base.transform.up, 1f, base.transform.up * -1f, out hitInfo3, 5f, lmask))
			{
				Breakable component = hitInfo3.collider.GetComponent<Breakable>();
				if (component != null && ((component.weak && !component.precisionOnly) || component.forceGroundSlammable) && !component.unbreakable)
				{
					UnityEngine.Object.Instantiate(impactDust, hitInfo3.point, Quaternion.identity);
					component.Break();
				}
				if (hitInfo3.collider.gameObject.TryGetComponent<Bleeder>(out var component2))
				{
					component2.GetHit(hitInfo3.point, GoreType.Head);
				}
				if (hitInfo3.transform.TryGetComponent<Idol>(out var component3))
				{
					component3.Death();
				}
			}
		}
		if (stillHolding && MonoSingleton<InputManager>.Instance.InputSource.Slide.WasCanceledThisFrame)
		{
			stillHolding = false;
		}
		if (activated)
		{
			if (!GameStateManager.Instance.PlayerInputLocked && MonoSingleton<InputManager>.Instance.InputSource.Jump.WasPerformedThisFrame && (!falling || gc.canJump || wc.CheckForEnemyCols()) && !jumpCooldown)
			{
				if (gc.canJump || wc.CheckForEnemyCols())
				{
					currentWallJumps = 0;
					rocketJumps = 0;
					hammerJumps = 0;
					clingFade = 0f;
					rocketRides = 0;
				}
				Jump();
			}
			if (!gc.onGround && wc.onWall)
			{
				if (!sliding && Physics.Raycast(base.transform.position, movementDirection, out var hitInfo4, 1f, lmask))
				{
					if (rb.velocity.y < -1f && !gc.heavyFall)
					{
						rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -1f, 1f), -2f * clingFade, Mathf.Clamp(rb.velocity.z, -1f, 1f));
						if (scrapeParticle == null)
						{
							scrapeParticle = UnityEngine.Object.Instantiate(scrapePrefab, hitInfo4.point, Quaternion.identity);
						}
						scrapeParticle.transform.position = new Vector3(hitInfo4.point.x, hitInfo4.point.y + 1f, hitInfo4.point.z);
						scrapeParticle.transform.forward = hitInfo4.normal;
						clingFade = Mathf.MoveTowards(clingFade, 50f, Time.deltaTime * 4f);
					}
				}
				else if (scrapeParticle != null)
				{
					UnityEngine.Object.Destroy(scrapeParticle);
					scrapeParticle = null;
				}
				if (!GameStateManager.Instance.PlayerInputLocked && MonoSingleton<InputManager>.Instance.InputSource.Jump.WasPerformedThisFrame && !jumpCooldown && currentWallJumps < 3 && (bool)wc && wc.CheckForCols())
				{
					WallJump();
				}
			}
			else if (scrapeParticle != null)
			{
				UnityEngine.Object.Destroy(scrapeParticle);
				scrapeParticle = null;
			}
		}
		if (!GameStateManager.Instance.PlayerInputLocked && !GameStateManager.Instance.IsStateActive("alter-menu"))
		{
			if (MonoSingleton<InputManager>.Instance.InputSource.Slide.WasPerformedThisFrame && (gc.onGround || (float)gc.sinceLastGrounded < 0.03f) && activated && (!slowMode || crouching) && !GameStateManager.Instance.PlayerInputLocked && !sliding)
			{
				StartSlide();
			}
			if (MonoSingleton<InputManager>.Instance.InputSource.Slide.WasPerformedThisFrame && !gc.onGround && !sliding && !jumping && activated && !slowMode && !GameStateManager.Instance.PlayerInputLocked && Physics.Raycast(gc.transform.position + base.transform.up, base.transform.up * -1f, out var _, 2f, lmask, QueryTriggerInteraction.Ignore))
			{
				StartSlide();
			}
		}
		if ((MonoSingleton<InputManager>.Instance.InputSource.Slide.WasCanceledThisFrame || (slowMode && !crouching)) && sliding)
		{
			StopSlide();
		}
		if (sliding && activated)
		{
			standing = false;
			slideLength += Time.deltaTime;
			if (cc.defaultPos.y != cc.originalPos.y - 0.625f)
			{
				Vector3 vector2 = new Vector3(cc.originalPos.x, cc.originalPos.y - 0.625f, cc.originalPos.z);
				cc.defaultPos = Vector3.MoveTowards(cc.defaultPos, vector2, ((cc.defaultPos - vector2).magnitude + 0.5f) * Time.deltaTime * 20f);
			}
			Vector3 normalized = Vector3.ProjectOnPlane(rb.velocity.normalized, Vector3.up).normalized;
			if (currentSlideParticle != null)
			{
				currentSlideParticle.transform.position = base.transform.position + normalized * 10f;
				currentSlideParticle.transform.forward = -dodgeDirection;
				slideTrail.colorOverLifetime = ((boostLeft > 0f && base.gameObject.layer == 15) ? invincibleSlideGradient : normalSlideGradient);
			}
			if (slideSafety > 0f)
			{
				slideSafety -= Time.deltaTime * 5f;
			}
			if (gc.onGround || wc.onWall)
			{
				slideScrape.transform.position = base.transform.position + normalized;
				slideScrape.transform.forward = -normalized;
				cc.CameraShake(0.1f);
			}
			else
			{
				slideScrape.transform.position = Vector3.one * 5000f;
			}
			if (rising)
			{
				if (cc.defaultPos != cc.originalPos - Vector3.up * 0.625f)
				{
					cc.defaultPos = Vector3.MoveTowards(cc.defaultPos, cc.originalPos, ((cc.originalPos - cc.defaultPos).magnitude + 0.5f) * Time.deltaTime * 10f);
				}
				else
				{
					rising = false;
				}
			}
		}
		else if ((bool)groundProperties && groundProperties.forceCrouch)
		{
			playerCollider.height = 1.25f;
			crouching = true;
			if (standing)
			{
				standing = false;
				base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 1.125f, base.transform.position.z);
				gc.transform.localPosition = groundCheckPos + Vector3.up * 1.125f;
			}
			if (cc.defaultPos != cc.originalPos - Vector3.up * 0.625f)
			{
				cc.defaultPos = Vector3.MoveTowards(cc.defaultPos, cc.originalPos - Vector3.up * 0.625f, ((cc.originalPos - Vector3.up * 0.625f - cc.defaultPos).magnitude + 0.5f) * Time.deltaTime * 10f);
			}
		}
		else
		{
			if (activated)
			{
				if ((bool)playerCollider && playerCollider.height != 3.5f)
				{
					if (!Physics.Raycast(base.transform.position, Vector3.up, 2.25f, lmask, QueryTriggerInteraction.Ignore) && !Physics.SphereCast(new Ray(base.transform.position, Vector3.up), 0.5f, 2f, lmask, QueryTriggerInteraction.Ignore))
					{
						playerCollider.height = 3.5f;
						gc.transform.localPosition = groundCheckPos;
						if (Physics.Raycast(base.transform.position, Vector3.up * -1f, 2.25f, lmask, QueryTriggerInteraction.Ignore))
						{
							base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 1.125f, base.transform.position.z);
						}
						else
						{
							base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.625f, base.transform.position.z);
							cc.defaultPos = cc.originalPos;
							standing = true;
						}
						if (crouching)
						{
							crouching = false;
							slowMode = false;
						}
					}
					else
					{
						crouching = true;
						slowMode = true;
					}
				}
				else if (cc.defaultPos.y != cc.originalPos.y)
				{
					cc.defaultPos = Vector3.MoveTowards(cc.defaultPos, cc.originalPos, (cc.originalPos.y - cc.defaultPos.y + 0.5f) * Time.deltaTime * 10f);
				}
				else
				{
					standing = true;
				}
				if (rising)
				{
					if (cc.defaultPos != cc.originalPos)
					{
						cc.defaultPos = Vector3.MoveTowards(cc.defaultPos, cc.originalPos, ((cc.originalPos - cc.defaultPos).magnitude + 0.5f) * Time.deltaTime * 10f);
					}
					else
					{
						rising = false;
					}
				}
			}
			if (currentSlideParticle != null)
			{
				UnityEngine.Object.Destroy(currentSlideParticle);
			}
			if (slideScrape != null)
			{
				UnityEngine.Object.Destroy(slideScrape);
			}
		}
		if (rising && Vector3.Distance(cc.defaultPos, cc.originalPos) > 10f)
		{
			rising = false;
			cc.defaultPos = cc.originalPos;
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Dodge.WasPerformedThisFrame && activated && !slowMode && !GameStateManager.Instance.PlayerInputLocked)
		{
			if (((bool)groundProperties && !groundProperties.canDash) || modNoDashSlide)
			{
				if (modNoDashSlide || !groundProperties.silentDashFail)
				{
					UnityEngine.Object.Instantiate(staminaFailSound);
				}
			}
			else if (boostCharge >= 100f)
			{
				if (sliding)
				{
					StopSlide();
				}
				boostLeft = 100f;
				dashStorage = 1f;
				boost = true;
				dodgeDirection = movementDirection;
				if (dodgeDirection == Vector3.zero)
				{
					dodgeDirection = base.transform.forward;
				}
				Quaternion identity = Quaternion.identity;
				identity.SetLookRotation(dodgeDirection * -1f);
				UnityEngine.Object.Instantiate(dodgeParticle, base.transform.position + dodgeDirection * 10f, identity);
				if (!asscon.majorEnabled || !asscon.infiniteStamina)
				{
					boostCharge -= 100f;
				}
				if (dodgeDirection == base.transform.forward)
				{
					cc.dodgeDirection = 0;
				}
				else if (dodgeDirection == base.transform.forward * -1f)
				{
					cc.dodgeDirection = 1;
				}
				else
				{
					cc.dodgeDirection = 2;
				}
				aud.clip = dodgeSound;
				aud.volume = 1f;
				aud.pitch = 1f;
				aud.Play();
				MonoSingleton<RumbleManager>.Instance.SetVibration(RumbleProperties.Dash);
				if (gc.heavyFall)
				{
					fallSpeed = 0f;
					gc.heavyFall = false;
					if (currentFallParticle != null)
					{
						UnityEngine.Object.Destroy(currentFallParticle);
					}
				}
			}
			else
			{
				UnityEngine.Object.Instantiate(staminaFailSound);
			}
		}
		if (!walking && vector.sqrMagnitude > 0f && !sliding && gc.onGround)
		{
			walking = true;
			anim.SetBool("WalkF", value: true);
		}
		else if ((walking && Mathf.Approximately(vector.sqrMagnitude, 0f)) || !gc.onGround || sliding)
		{
			walking = false;
			anim.SetBool("WalkF", value: false);
		}
		if (hurting && hp > 0)
		{
			currentColor.a -= Time.deltaTime;
			hurtScreen.color = currentColor;
			if (currentColor.a <= 0f)
			{
				hurting = false;
			}
		}
		if (safeExplosionLaunchCooldown > 0f)
		{
			safeExplosionLaunchCooldown = Mathf.MoveTowards(safeExplosionLaunchCooldown, 0f, Time.deltaTime);
		}
		if (boostCharge != 300f && !sliding && !slowMode)
		{
			float num2 = 1f;
			if (difficulty == 1)
			{
				num2 = 1.5f;
			}
			else if (difficulty == 0)
			{
				num2 = 2f;
			}
			boostCharge = Mathf.MoveTowards(boostCharge, 300f, 70f * Time.deltaTime * num2);
		}
		Vector3 vector3 = hudOriginalPos - cc.transform.InverseTransformDirection(rb.velocity) / 1000f;
		float num3 = Vector3.Distance(vector3, screenHud.transform.localPosition);
		screenHud.transform.localPosition = Vector3.MoveTowards(screenHud.transform.localPosition, vector3, Time.deltaTime * 15f * num3);
		Vector3 vector4 = Vector3.ClampMagnitude(camOriginalPos - cc.transform.InverseTransformDirection(rb.velocity) / 350f * -1f, 0.2f);
		float num4 = Vector3.Distance(vector4, hudCam.transform.localPosition);
		hudCam.transform.localPosition = Vector3.MoveTowards(hudCam.transform.localPosition, vector4, Time.deltaTime * 25f * num4);
		int rankIndex = MonoSingleton<StyleHUD>.Instance.rankIndex;
		if (rankIndex == 7 || difficulty <= 1)
		{
			antiHp = 0f;
			antiHpCooldown = 0f;
		}
		else if (antiHpCooldown > 0f)
		{
			if (rankIndex >= 4)
			{
				antiHpCooldown = Mathf.MoveTowards(antiHpCooldown, 0f, Time.deltaTime * (float)(rankIndex / 2));
			}
			else
			{
				antiHpCooldown = Mathf.MoveTowards(antiHpCooldown, 0f, Time.deltaTime);
			}
		}
		else if (antiHp > 0f)
		{
			if (rankIndex >= 4)
			{
				antiHp = Mathf.MoveTowards(antiHp, 0f, Time.deltaTime * (float)rankIndex * 10f);
			}
			else
			{
				antiHp = Mathf.MoveTowards(antiHp, 0f, Time.deltaTime * 15f);
			}
		}
		if (!gc.heavyFall && currentFallParticle != null)
		{
			UnityEngine.Object.Destroy(currentFallParticle);
		}
	}

	private void FixedUpdate()
	{
		friction = modForcedFrictionMultip * (groundProperties ? groundProperties.friction : 1f);
		if (sliding)
		{
			if (slideSafety <= 0f)
			{
				Vector3 vector = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
				float num = 10f;
				if ((bool)groundProperties && groundProperties.speedMultiplier < 1f)
				{
					num *= groundProperties.speedMultiplier;
				}
				if (vector.magnitude < num && !rising)
				{
					slideSafety = Mathf.MoveTowards(slideSafety, -0.1f, Time.deltaTime);
					if (slideSafety <= -0.1f)
					{
						StopSlide();
					}
				}
				else
				{
					slideSafety = 0f;
				}
			}
			if (wc.onWall && rb.velocity.y < 0f)
			{
				rb.AddForce(-Physics.gravity * 0.4f, ForceMode.Acceleration);
			}
		}
		if (!sliding && activated)
		{
			framesSinceSlide++;
			if (gc.heavyFall)
			{
				preSlideDelay = 0.2f;
				preSlideSpeed = slamForce;
				if (Physics.SphereCast(base.transform.position - Vector3.up * 1.5f, 0.35f, Vector3.down, out var hitInfo, Time.fixedDeltaTime * Mathf.Abs(rb.velocity.y), LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
				{
					base.transform.position = hitInfo.point + Vector3.up * 1.5f;
					rb.velocity = Vector3.zero;
				}
			}
			else if (!boost && falling && rb.velocity.magnitude / 24f > preSlideSpeed)
			{
				preSlideSpeed = rb.velocity.magnitude / 24f;
				preSlideDelay = 0.2f;
			}
			else
			{
				preSlideDelay = Mathf.MoveTowards(preSlideDelay, 0f, Time.fixedDeltaTime);
				if (preSlideDelay <= 0f)
				{
					preSlideDelay = 0.2f;
					preSlideSpeed = rb.velocity.magnitude / 24f;
				}
			}
		}
		if (!boost)
		{
			Move();
			return;
		}
		rb.useGravity = true;
		Dodge();
	}

	private void Move()
	{
		slideEnding = false;
		if (!hurting && !levelOver)
		{
			base.gameObject.layer = 2;
			exploded = false;
		}
		if (gc.onGround && !jumping)
		{
			currentWallJumps = 0;
			rocketJumps = 0;
			hammerJumps = 0;
			rocketRides = 0;
		}
		if (gc.onGround && friction > 0f && !jumping)
		{
			float y = rb.velocity.y;
			if (slopeCheck.onGround && movementDirection.x == 0f && movementDirection.z == 0f)
			{
				y = 0f;
				rb.useGravity = false;
			}
			else
			{
				rb.useGravity = true;
			}
			float num = 2.75f;
			if (slowMode)
			{
				num = 1.25f;
			}
			if ((bool)groundProperties)
			{
				num *= groundProperties.speedMultiplier;
			}
			movementDirection2 = new Vector3(movementDirection.x * walkSpeed * Time.deltaTime * num, y, movementDirection.z * walkSpeed * Time.deltaTime * num);
			Vector3 vector = pushForce;
			if ((bool)groundProperties && groundProperties.push)
			{
				Vector3 vector2 = groundProperties.pushForce;
				if (groundProperties.pushDirectionRelative)
				{
					vector2 = groundProperties.transform.rotation * vector2;
				}
				vector += vector2;
			}
			rb.velocity = Vector3.Lerp(rb.velocity, movementDirection2 + vector, 0.25f * friction);
			anim.SetBool("Run", value: false);
		}
		else
		{
			rb.useGravity = true;
			if (slowMode)
			{
				movementDirection2 = new Vector3(movementDirection.x * walkSpeed * Time.deltaTime * 1.25f, rb.velocity.y, movementDirection.z * walkSpeed * Time.deltaTime * 1.25f);
			}
			else
			{
				movementDirection2 = new Vector3(movementDirection.x * walkSpeed * Time.deltaTime * 2.75f, rb.velocity.y, movementDirection.z * walkSpeed * Time.deltaTime * 2.75f);
			}
			airDirection.y = 0f;
			if ((movementDirection2.x > 0f && rb.velocity.x < movementDirection2.x) || (movementDirection2.x < 0f && rb.velocity.x > movementDirection2.x))
			{
				airDirection.x = movementDirection2.x;
			}
			else
			{
				airDirection.x = 0f;
			}
			if ((movementDirection2.z > 0f && rb.velocity.z < movementDirection2.z) || (movementDirection2.z < 0f && rb.velocity.z > movementDirection2.z))
			{
				airDirection.z = movementDirection2.z;
			}
			else
			{
				airDirection.z = 0f;
			}
			rb.AddForce(airDirection.normalized * airAcceleration);
		}
	}

	private void Dodge()
	{
		if (sliding)
		{
			if (!hurting && !levelOver && boostLeft <= 0f)
			{
				base.gameObject.layer = 2;
				exploded = false;
			}
			float num = 1f;
			if (preSlideSpeed > 1f)
			{
				if (preSlideSpeed > 3f)
				{
					preSlideSpeed = 3f;
				}
				num = preSlideSpeed;
				if (gc.onGround && friction != 0f)
				{
					preSlideSpeed -= Time.fixedDeltaTime * preSlideSpeed * friction;
				}
				preSlideDelay = 0f;
			}
			if (modNoDashSlide)
			{
				StopSlide();
				return;
			}
			if ((bool)groundProperties)
			{
				if (!groundProperties.canSlide)
				{
					StopSlide();
					return;
				}
				num *= groundProperties.speedMultiplier;
			}
			Vector3 vector = new Vector3(dodgeDirection.x * walkSpeed * Time.deltaTime * 4f * num, rb.velocity.y, dodgeDirection.z * walkSpeed * Time.deltaTime * 4f * num);
			if ((bool)groundProperties && groundProperties.push)
			{
				Vector3 vector2 = groundProperties.pushForce;
				if (groundProperties.pushDirectionRelative)
				{
					vector2 = groundProperties.transform.rotation * vector2;
				}
				vector += vector2;
			}
			if (boostLeft > 0f)
			{
				dashStorage = Mathf.MoveTowards(dashStorage, 0f, Time.fixedDeltaTime);
				if (dashStorage <= 0f)
				{
					boostLeft = 0f;
				}
			}
			movementDirection = Vector3.ClampMagnitude(MonoSingleton<InputManager>.Instance.InputSource.Move.ReadValue<Vector2>().x * base.transform.right, 1f) * 5f;
			if (!MonoSingleton<HookArm>.Instance || !MonoSingleton<HookArm>.Instance.beingPulled)
			{
				rb.velocity = vector + pushForce + movementDirection;
			}
			else
			{
				StopSlide();
			}
			return;
		}
		float y = 0f;
		if (slideEnding)
		{
			y = rb.velocity.y;
		}
		float num2 = 2.75f;
		movementDirection2 = new Vector3(dodgeDirection.x * walkSpeed * Time.deltaTime * num2, y, dodgeDirection.z * walkSpeed * Time.deltaTime * num2);
		base.gameObject.layer = 15;
		if (slideEnding)
		{
			slideEnding = false;
			if (!gc.onGround || friction == 0f)
			{
				boost = false;
				return;
			}
		}
		if (boostLeft > 0f)
		{
			rb.velocity = movementDirection2 * 3f;
			boostLeft -= 4f;
			return;
		}
		if (!gc.onGround || friction != 0f)
		{
			rb.velocity = movementDirection2;
		}
		boost = false;
	}

	public void Jump()
	{
		float num = 1500f;
		if (modNoJump || (bool)groundProperties)
		{
			if (modNoJump || !groundProperties.canJump)
			{
				if (modNoJump || !groundProperties.silentJumpFail)
				{
					aud.clip = jumpSound;
					aud.volume = 0.75f;
					aud.pitch = 0.25f;
					aud.Play();
				}
				return;
			}
			num *= groundProperties.jumpForceMultiplier;
		}
		jumping = true;
		Invoke("NotJumping", 0.25f);
		falling = true;
		if (quakeJump)
		{
			UnityEngine.Object.Instantiate(quakeJumpSound).GetComponent<AudioSource>().pitch = 1f + UnityEngine.Random.Range(0f, 0.1f);
		}
		aud.clip = jumpSound;
		if (gc.superJumpChance > 0f)
		{
			aud.volume = 0.85f;
			aud.pitch = 2f;
		}
		else
		{
			aud.volume = 0.75f;
			aud.pitch = 1f;
		}
		aud.Play();
		rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		if (sliding)
		{
			if (slowMode)
			{
				rb.AddForce(Vector3.up * jumpPower * num);
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * num * 2f);
			}
			StopSlide();
		}
		else if (boost)
		{
			if (boostCharge >= 100f)
			{
				if (!asscon.majorEnabled || !asscon.infiniteStamina)
				{
					boostCharge -= 100f;
				}
				UnityEngine.Object.Instantiate(dashJumpSound);
			}
			else
			{
				rb.velocity = new Vector3(movementDirection.x * walkSpeed * Time.deltaTime * 2.75f, 0f, movementDirection.z * walkSpeed * Time.deltaTime * 2.75f);
				UnityEngine.Object.Instantiate(staminaFailSound);
			}
			if (slowMode)
			{
				rb.AddForce(Vector3.up * jumpPower * num * 0.75f);
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * num * 1.5f);
			}
		}
		else if (slowMode)
		{
			rb.AddForce(Vector3.up * jumpPower * num * 1.25f);
		}
		else if (gc.superJumpChance > 0f || gc.extraJumpChance > 0f)
		{
			if (slamForce < 5.5f)
			{
				rb.AddForce(Vector3.up * jumpPower * num * (3f + (slamForce - 1f)));
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * num * 12.5f);
			}
			slamForce = 0f;
		}
		else
		{
			rb.AddForce(Vector3.up * jumpPower * num * 2.6f);
		}
		TrySSJ(dodgeDirection.normalized, 0.5f, (int frame) => 1f / Mathf.Pow(2f, frame - 1));
		jumpCooldown = true;
		Invoke("JumpReady", 0.2f);
		boost = false;
	}

	private void TrySSJ(Vector3 direction, float speedMultiplier, Func<int, float> speedLossFormula)
	{
		if (framesSinceSlide > 0 && (float)framesSinceSlide < ssjMaxFrames && !boost)
		{
			float num = speedLossFormula(framesSinceSlide);
			float num2 = speedMultiplier * walkSpeed * 2.75f * 3f * Time.fixedDeltaTime;
			float y = rb.velocity.y;
			float num3 = num * num2;
			rb.velocity = velocityAfterSlide + direction * num3;
			rb.velocity = new Vector3(rb.velocity.x, y, rb.velocity.z);
			rb.velocity = Mathf.Min(rb.velocity.magnitude, 100f) * rb.velocity.normalized;
			if (MonoSingleton<PrefsManager>.Instance.GetBool("ssjIndicator"))
			{
				MonoSingleton<SubtitleController>.Instance.DisplaySubtitle($"SSJ: {rb.velocity.magnitude} (+{num3})u/s, {num * 100f}% speed (Frame {framesSinceSlide}/{ssjMaxFrames - 1f})", null, ignoreSetting: true);
			}
		}
	}

	private void WallJump()
	{
		jumping = true;
		Invoke("NotJumping", 0.25f);
		currentWallJumps++;
		if (gc.heavyFall)
		{
			slamStorage = true;
		}
		if (quakeJump)
		{
			UnityEngine.Object.Instantiate(quakeJumpSound).GetComponent<AudioSource>().pitch = 1.1f + (float)currentWallJumps * 0.05f;
		}
		aud.clip = jumpSound;
		aud.pitch += 0.25f;
		aud.volume = 0.75f;
		aud.Play();
		if (currentWallJumps == 3)
		{
			aud2.clip = finalWallJump;
			aud2.volume = 0.75f;
			aud2.Play();
		}
		wallJumpPos = base.transform.position - wc.poc;
		if (NonConvexJumpDebug.Active)
		{
			for (int i = 0; i < 4; i++)
			{
				NonConvexJumpDebug.CreateBall(Color.white, Vector3.Lerp(base.transform.position, wc.poc, (float)i / 4f), 0.4f);
			}
		}
		if (sliding || framesSinceSlide < MonoSingleton<PrefsManager>.Instance.GetIntLocal("ssjMaxFrames", 4))
		{
			Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
			Vector3 vector = Vector3.Reflect(dodgeDirection.normalized, wallJumpPos.normalized);
			vector = Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
			vector = (dodgeDirection = (vector + wallJumpPos.normalized * 0.35f).normalized);
			rb.velocity = vector.normalized * rb.velocity.magnitude;
			TrySSJ(vector, 0.75f, (int frame) => (ssjMaxFrames - (float)frame + 1f) / ssjMaxFrames);
			rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, 15f), rb.velocity.z);
		}
		else
		{
			boost = false;
			rb.velocity = Vector3.zero;
			Vector3 vector2 = new Vector3(wallJumpPos.normalized.x, 1f, wallJumpPos.normalized.z);
			rb.AddForce(vector2 * 2000f * wallJumpPower);
		}
		jumpCooldown = true;
		Invoke("JumpReady", 0.1f);
	}

	private void OnCollisionEnter(Collision other)
	{
		if (sliding)
		{
			ContactPoint[] contacts = other.contacts;
			foreach (ContactPoint contactPoint in contacts)
			{
				Vector3.Angle(Vector3.ProjectOnPlane(dodgeDirection, contactPoint.normal).normalized, dodgeDirection);
				_ = 60f;
			}
		}
	}

	public void Launch(Vector3 direction, float multiplier = 8f, bool ignoreMass = false)
	{
		if (((bool)groundProperties && !groundProperties.launchable) || (direction == Vector3.down && gc.onGround))
		{
			return;
		}
		jumping = true;
		Invoke("NotJumping", 0.5f);
		jumpCooldown = true;
		Invoke("JumpReady", 0.2f);
		boost = false;
		if (gc.heavyFall)
		{
			fallSpeed = 0f;
			gc.heavyFall = false;
			if (currentFallParticle != null)
			{
				UnityEngine.Object.Destroy(currentFallParticle);
			}
		}
		if (direction.magnitude > 0f)
		{
			rb.velocity = Vector3.zero;
		}
		rb.AddForce(Vector3.ClampMagnitude(direction, 1000f) * multiplier, (!ignoreMass) ? ForceMode.Impulse : ForceMode.VelocityChange);
	}

	public void LaunchFromPoint(Vector3 position, float strength, float maxDistance = 1f)
	{
		if (!groundProperties || groundProperties.launchable)
		{
			Vector3 vector = (base.transform.position - position).normalized;
			if (position == base.transform.position)
			{
				vector = Vector3.up;
			}
			Vector3 direction;
			if (jumping)
			{
				direction = vector * maxDistance * strength;
				direction.y = 0.5f * maxDistance * strength;
			}
			else
			{
				float num = maxDistance - Vector3.Distance(base.transform.position, position);
				direction = vector * num * strength;
				direction.y = 0.5f * num * strength;
			}
			Launch(direction);
		}
	}

	public void LaunchFromPointAtSpeed(Vector3 position, float speed)
	{
		if (!groundProperties || groundProperties.launchable)
		{
			Vector3 vector = (base.transform.position - position).normalized;
			if (position == base.transform.position)
			{
				vector = Vector3.up;
			}
			Vector3 direction = vector * speed;
			direction.y = Mathf.Max(0.5f * speed, direction.y);
			Launch(direction, 1f, ignoreMass: true);
		}
	}

	public void Slamdown(float strength)
	{
		boost = false;
		if (gc.heavyFall)
		{
			fallSpeed = 0f;
			gc.heavyFall = false;
			if (currentFallParticle != null)
			{
				UnityEngine.Object.Destroy(currentFallParticle);
			}
		}
		rb.velocity = Vector3.zero;
		rb.velocity = new Vector3(0f, 0f - strength, 0f);
	}

	private void JumpReady()
	{
		jumpCooldown = false;
	}

	public void FakeHurt(bool silent = false)
	{
		currentColor.a = 0.25f;
		hurting = true;
		cc.CameraShake(0.1f);
		if (!silent)
		{
			hurtAud.pitch = UnityEngine.Random.Range(0.8f, 1f);
			hurtAud.PlayOneShot(hurtAud.clip);
		}
	}

	public void GetHurt(int damage, bool invincible, float scoreLossMultiplier = 1f, bool explosion = false, bool instablack = false, float hardDamageMultiplier = 0.35f, bool ignoreInvincibility = false)
	{
		if (dead || levelOver || !(!invincible || base.gameObject.layer != 15 || ignoreInvincibility) || damage <= 0)
		{
			return;
		}
		if (explosion)
		{
			exploded = true;
		}
		if (asscon.majorEnabled)
		{
			damage = Mathf.RoundToInt((float)damage * asscon.damageTaken);
		}
		if (Invincibility.Enabled)
		{
			damage = 0;
		}
		if (invincible)
		{
			base.gameObject.layer = 15;
		}
		if (damage >= 50)
		{
			currentColor.a = 0.8f;
		}
		else
		{
			currentColor.a = 0.5f;
		}
		hurting = true;
		cc.CameraShake(damage / 20);
		hurtAud.pitch = UnityEngine.Random.Range(0.8f, 1f);
		hurtAud.PlayOneShot(hurtAud.clip);
		if (hp - damage > 0)
		{
			hp -= damage;
		}
		else
		{
			hp = 0;
		}
		if (invincible && scoreLossMultiplier != 0f && difficulty >= 2 && (!asscon.majorEnabled || !asscon.disableHardDamage) && hp <= 100)
		{
			if (antiHp + (float)damage * hardDamageMultiplier < 99f)
			{
				antiHp += (float)damage * hardDamageMultiplier;
			}
			else
			{
				antiHp = 99f;
			}
			if (antiHpCooldown == 0f)
			{
				antiHpCooldown += 1f;
			}
			if (difficulty >= 3)
			{
				antiHpCooldown += 1f;
			}
			antiHpFlash.Flash(1f);
			antiHpCooldown += damage / 20;
		}
		if (shud == null)
		{
			shud = MonoSingleton<StyleHUD>.Instance;
		}
		if (scoreLossMultiplier > 0.5f)
		{
			shud.RemovePoints(0);
			shud.DescendRank();
		}
		else
		{
			shud.RemovePoints(Mathf.RoundToInt(damage));
		}
		StatsManager statsManager = MonoSingleton<StatsManager>.Instance;
		if (damage <= 200)
		{
			statsManager.stylePoints -= Mathf.RoundToInt((float)(damage * 5) * scoreLossMultiplier);
		}
		else
		{
			statsManager.stylePoints -= Mathf.RoundToInt(1000f * scoreLossMultiplier);
		}
		statsManager.tookDamage = true;
		if (hp != 0)
		{
			return;
		}
		if (!endlessMode)
		{
			blackScreen.gameObject.SetActive(value: true);
			MonoSingleton<TimeController>.Instance.controlPitch = false;
			if (instablack)
			{
				blackColor.a = 1f;
			}
			screenHud.SetActive(value: false);
		}
		else
		{
			GetComponentInChildren<FinalCyberRank>().GameOver();
			CrowdReactions crowdReactions = MonoSingleton<CrowdReactions>.Instance;
			if (crowdReactions != null)
			{
				crowdReactions.React(crowdReactions.aww);
			}
		}
		rb.constraints = RigidbodyConstraints.None;
		if ((bool)MonoSingleton<PowerUpMeter>.Instance)
		{
			MonoSingleton<PowerUpMeter>.Instance.juice = 0f;
		}
		cc.enabled = false;
		if (gunc == null)
		{
			gunc = GetComponentInChildren<GunControl>();
		}
		gunc.NoWeapon();
		rb.constraints = RigidbodyConstraints.None;
		dead = true;
		activated = false;
		if (punch == null)
		{
			punch = GetComponentInChildren<FistControl>();
		}
		punch.NoFist();
	}

	public void ForceAntiHP(float amount, bool silent = false, bool dontOverwriteHp = false, bool addToCooldown = true)
	{
		if ((asscon.majorEnabled && asscon.disableHardDamage) || hp > 100)
		{
			return;
		}
		amount = Mathf.Clamp(amount, 0f, 99f);
		float num = antiHp;
		if ((float)hp > 100f - amount)
		{
			if (dontOverwriteHp)
			{
				amount = 100 - hp;
			}
			else
			{
				hp = Mathf.RoundToInt(100f - amount);
			}
		}
		if (MonoSingleton<StyleHUD>.Instance.rankIndex < 7)
		{
			antiHpFlash.Flash(1f);
			if (amount > antiHp)
			{
				FakeHurt(silent);
			}
		}
		antiHp = amount;
		if (addToCooldown)
		{
			if (antiHpCooldown < 1f || (difficulty >= 3 && antiHpCooldown < 2f))
			{
				antiHpCooldown = ((difficulty < 3) ? 1 : 2);
			}
			if (amount - num < 50f)
			{
				antiHpCooldown += (amount - num) / 20f;
			}
			else
			{
				antiHpCooldown += 2.5f;
			}
		}
		else if (antiHpCooldown <= 1f)
		{
			antiHpCooldown = 1f;
		}
	}

	public void ForceAddAntiHP(float amount, bool silent = false, bool dontOverwriteHp = false, bool addToCooldown = true)
	{
		ForceAntiHP(antiHp + amount, silent, dontOverwriteHp, addToCooldown);
	}

	public void GetHealth(int health, bool silent, bool fromExplosion = false)
	{
		if (dead || (exploded && fromExplosion))
		{
			return;
		}
		float num = health;
		float num2 = 100f;
		if (difficulty == 0 || (difficulty == 1 && sameCheckpointRestarts > 2))
		{
			num2 = 200f;
		}
		if (num < 1f)
		{
			num = 1f;
		}
		if ((float)hp <= num2)
		{
			if ((float)hp + num < num2 - (float)Mathf.RoundToInt(antiHp))
			{
				hp += Mathf.RoundToInt(num);
			}
			else if ((float)hp != num2 - (float)Mathf.RoundToInt(antiHp))
			{
				hp = Mathf.RoundToInt(num2) - Mathf.RoundToInt(antiHp);
			}
			hpFlash.Flash(1f);
			if (!silent && health > 5)
			{
				if (greenHpAud == null)
				{
					greenHpAud = hpFlash.GetComponent<AudioSource>();
				}
				greenHpAud.Play();
			}
		}
		if (!silent && health > 5 && MonoSingleton<PrefsManager>.Instance.GetBoolLocal("bloodEnabled"))
		{
			UnityEngine.Object.Instantiate(scrnBlood, fullHud.transform);
		}
	}

	public void Parry(EnemyIdentifier eid = null, string customParryText = "")
	{
		MonoSingleton<TimeController>.Instance.ParryFlash();
		exploded = false;
		GetHealth(999, silent: false);
		FullStamina();
		if (shud == null)
		{
			shud = MonoSingleton<StyleHUD>.Instance;
		}
		if (!eid || !eid.blessed)
		{
			shud.AddPoints(100, (customParryText != "") ? ("<color=green>" + customParryText + "</color>") : "ultrakill.parry");
		}
	}

	public void SuperCharge()
	{
		GetHealth(100, silent: true);
		hp = 200;
	}

	public void Respawn()
	{
		MonoSingleton<CameraController>.Instance.cam.useOcclusionCulling = true;
		if (sliding)
		{
			StopSlide();
		}
		sameCheckpointRestarts++;
		if (difficulty == 0)
		{
			hp = 200;
		}
		else
		{
			hp = 100;
		}
		boostCharge = 299f;
		antiHp = 0f;
		antiHpCooldown = 0f;
		rb.constraints = defaultRBConstraints;
		activated = true;
		blackScreen.gameObject.SetActive(value: false);
		cc.enabled = true;
		if ((bool)MonoSingleton<PowerUpMeter>.Instance)
		{
			MonoSingleton<PowerUpMeter>.Instance.juice = 0f;
		}
		StatsManager statsManager = MonoSingleton<StatsManager>.Instance;
		statsManager.stylePoints = statsManager.stylePoints / 3 * 2;
		if (gunc == null)
		{
			gunc = GetComponentInChildren<GunControl>();
		}
		gunc.YesWeapon();
		screenHud.SetActive(value: true);
		dead = false;
		blackColor.a = 0f;
		youDiedColor.a = 0f;
		currentAllPitch = 1f;
		blackScreen.color = blackColor;
		youDiedText.color = youDiedColor;
		MonoSingleton<TimeController>.Instance.controlPitch = true;
		MonoSingleton<HookArm>.Instance?.Cancel();
		if (punch == null)
		{
			punch = GetComponentInChildren<FistControl>();
		}
		punch.activated = true;
		punch.YesFist();
		slowMode = false;
		MonoSingleton<WeaponCharges>.Instance.MaxCharges();
		if (MonoSingleton<WeaponCharges>.Instance.rocketFrozen)
		{
			MonoSingleton<WeaponCharges>.Instance.rocketLauncher.UnfreezeRockets();
		}
	}

	public void ResetHardDamage()
	{
		antiHp = 0f;
		antiHpCooldown = 0f;
	}

	private void NotJumping()
	{
		jumping = false;
	}

	private void StartSlide()
	{
		if (currentSlideParticle != null)
		{
			UnityEngine.Object.Destroy(currentSlideParticle);
		}
		if (slideScrape != null)
		{
			UnityEngine.Object.Destroy(slideScrape);
		}
		if (modNoDashSlide)
		{
			StopSlide();
		}
		else
		{
			if ((bool)MonoSingleton<HookArm>.Instance && MonoSingleton<HookArm>.Instance.beingPulled)
			{
				return;
			}
			if ((bool)groundProperties && !groundProperties.canSlide)
			{
				if (!groundProperties.silentSlideFail)
				{
					StopSlide();
				}
				return;
			}
			if (!crouching)
			{
				playerCollider.height = 1.25f;
				base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 1.125f, base.transform.position.z);
				gc.transform.localPosition = groundCheckPos + Vector3.up * 1.125f;
			}
			slideSafety = 1f;
			sliding = true;
			boost = true;
			dodgeDirection = movementDirection;
			if (dodgeDirection == Vector3.zero)
			{
				dodgeDirection = base.transform.forward;
			}
			Quaternion identity = Quaternion.identity;
			identity.SetLookRotation(dodgeDirection * -1f);
			currentSlideParticle = UnityEngine.Object.Instantiate(slideParticle, base.transform.position + dodgeDirection * 10f, identity);
			slideTrail = currentSlideParticle.GetComponent<ParticleSystem>().trails;
			slideTrail.colorOverLifetime = ((boostLeft > 0f) ? invincibleSlideGradient : normalSlideGradient);
			slideScrape = UnityEngine.Object.Instantiate(slideScrapePrefab, base.transform.position + dodgeDirection * 2f, identity);
			if (dodgeDirection == base.transform.forward)
			{
				cc.dodgeDirection = 0;
			}
			else if (dodgeDirection == base.transform.forward * -1f)
			{
				cc.dodgeDirection = 1;
			}
			else
			{
				cc.dodgeDirection = 2;
			}
			MonoSingleton<RumbleManager>.Instance.SetVibration(RumbleProperties.Slide);
		}
	}

	private void CheckForGasoline()
	{
		Vector3Int vector3Int = StainVoxelManager.WorldToVoxelPosition(base.transform.position + Vector3.down * 1.8333334f);
		if (!lastCheckedGasolineVoxel.HasValue || lastCheckedGasolineVoxel.Value != vector3Int)
		{
			lastCheckedGasolineVoxel = vector3Int;
			modForcedFrictionMultip = ((!MonoSingleton<StainVoxelManager>.Instance.HasProxiesAt(vector3Int, 3, VoxelCheckingShape.VerticalBox, ProxySearchMode.AnyFloor)) ? 1 : 0);
		}
		if (oilSlideEffect.gameObject.activeSelf != (modForcedFrictionMultip == 0f))
		{
			oilSlideEffect.gameObject.SetActive(modForcedFrictionMultip == 0f);
		}
		if (modForcedFrictionMultip == 0f)
		{
			float num = Mathf.Min(35f, rb.velocity.magnitude) / 35f;
			oilSlideEffect.volume = Mathf.Lerp(0f, 0.85f, num);
			oilSlideEffect.transform.localScale = Vector3.one * num;
			oilSlideEffect.pitch = Mathf.Lerp(1.75f, 2.75f, num);
		}
	}

	public void StopSlide()
	{
		if (currentSlideParticle != null)
		{
			UnityEngine.Object.Destroy(currentSlideParticle);
		}
		if (slideScrape != null)
		{
			UnityEngine.Object.Destroy(slideScrape);
		}
		UnityEngine.Object.Instantiate(slideStopSound);
		cc.ResetToDefaultPos();
		sliding = false;
		slideEnding = true;
		if (slideLength > longestSlide)
		{
			longestSlide = slideLength;
		}
		slideLength = 0f;
		if (!gc.heavyFall)
		{
			Physics.IgnoreLayerCollision(2, 12, ignore: false);
		}
		framesSinceSlide = 0;
		velocityAfterSlide = rb.velocity;
		sinceSlideEnd = 0f;
		MonoSingleton<RumbleManager>.Instance.StopVibration(RumbleProperties.Slide);
	}

	public void EmptyStamina()
	{
		boostCharge = 0f;
	}

	public void FullStamina()
	{
		boostCharge = 300f;
	}

	public void DeactivatePlayer()
	{
		activated = false;
		MonoSingleton<CameraController>.Instance.activated = false;
		MonoSingleton<GunControl>.Instance.NoWeapon();
		MonoSingleton<FistControl>.Instance.NoFist();
		if (sliding)
		{
			StopSlide();
		}
	}

	public void ActivatePlayer()
	{
		activated = true;
		MonoSingleton<CameraController>.Instance.activated = true;
		MonoSingleton<GunControl>.Instance.YesWeapon();
		MonoSingleton<FistControl>.Instance.YesFist();
	}

	public void StopMovement()
	{
		if (sliding)
		{
			StopSlide();
		}
		if (boost)
		{
			boostLeft = 0f;
			boost = false;
		}
		movementDirection = Vector3.zero;
		rb.velocity = Vector3.zero;
	}

	public void DeactivateMovement()
	{
		activated = false;
		movementDirection = Vector3.zero;
	}

	public void ReactivateMovement()
	{
		activated = true;
		punch.YesFist();
	}

	public void LockMovementAxes()
	{
		rb.constraints = (RigidbodyConstraints)122;
	}

	public void UnlockMovementAxes()
	{
		rb.constraints = RigidbodyConstraints.FreezeRotation;
	}
}

using System;
using System.Linq;
using plog;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sandbox.Arm
{

	[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
	public class SandboxArm : MonoSingleton<SandboxArm>
	{
		private static readonly plog.Logger Log = new plog.Logger("SandboxArm");

		[FormerlySerializedAs("onEnableMode")]
		public SpawnableType onEnableType;

		[HideInInspector]
		public CameraController cameraCtrl;

		public LayerMask raycastLayers;

		public GameObject axisPoint;

		[SerializeField]
		private GameObject spawnEffect;

		public Material previewMaterial;

		public Transform holder;

		[FormerlySerializedAs("armAnimator")]
		public Animator animator;

		[Space]
		[SerializeField]
		private WeaponDescriptor genericDescriptor;

		[SerializeField]
		private WeaponDescriptor alterDescriptor;

		[SerializeField]
		private WeaponDescriptor destroyDescriptor;

		[SerializeField]
		private WeaponDescriptor buildOrPlaceDescriptor;

		[Space]
		public AudioSource tickSound;

		public AudioSource jabSound;

		public AudioSource selectSound;

		public AudioSource freezeSound;

		public AudioSource unfreezeSound;

		public AudioSource destroySound;

		public GameObject genericBreakParticles;

		public GameObject manipulateEffect;

		[Space]
		[SerializeField]
		private Image holoIcon;

		[SerializeField]
		private GameObject holoIconContainer;

		[NonSerialized]
		public SpawnMenu menu;

		private GoreZone goreZone;

		private bool debugStarted;

		private TimeSince timeSinceDebug;

		[NonSerialized]
		public bool hitSomething;

		[NonSerialized]
		public RaycastHit hit;

		private WeaponIcon localIcon;

		private bool firstBrushPositionSet;

		private Vector3 firstBlockPos;

		private Vector3 secondBlockPos;

		private Vector3 previousSecondBlockPos;

		public static GoreZone debugZone;

		private ISandboxArmMode currentMode;

		private static readonly int Holding = Animator.StringToHash("Holding");

		private static readonly int Punch = Animator.StringToHash("Punch");

		private static readonly int Manipulating = Animator.StringToHash("Manipulating");

		private static readonly int Pinched = Animator.StringToHash("Pinched");

		private static readonly int Crush = Animator.StringToHash("Crush");

		private static readonly int PushZ = Animator.StringToHash("PushZ");

		private static readonly int Point = Animator.StringToHash("Point");

		private static readonly int Tap = Animator.StringToHash("Tap");

		protected override void Awake()
		{
			base.Awake();
			localIcon = GetComponent<WeaponIcon>();
		}

		public void SetArmMode(ISandboxArmMode mode)
		{
			ResetMode();
			currentMode = mode;
			Log.Info("Setting Arm Mode to: " + currentMode);
			currentMode.OnEnable(this);
			selectSound.Play();
			ReloadIcon();
			ReloadHudIconColor();
		}

		public void ReloadIcon()
		{
			Log.Info("Reloading arm icon");
			if (currentMode == null || string.IsNullOrEmpty(currentMode.Icon))
			{
				holoIconContainer.SetActive(value: false);
				return;
			}
			holoIconContainer.SetActive(value: true);
			if (MonoSingleton<IconManager>.Instance.CurrentIcons.sandboxArmHoloIcons.Select((CheatAssetObject.KeyIcon e) => e.key).Contains(currentMode.Icon))
			{
				holoIcon.sprite = MonoSingleton<IconManager>.Instance.CurrentIcons.sandboxArmHoloIcons.First((CheatAssetObject.KeyIcon e) => e.key == currentMode.Icon).sprite;
			}
			else
			{
				holoIcon.sprite = MonoSingleton<IconManager>.Instance.CurrentIcons.genericSandboxToolIcon;
			}
		}

		private void ReloadHudIconColor()
		{
			if (localIcon == null)
			{
				localIcon = GetComponent<WeaponIcon>();
			}
			if (currentMode is AlterMode)
			{
				localIcon.weaponDescriptor = alterDescriptor;
			}
			else if (currentMode is DestroyMode)
			{
				localIcon.weaponDescriptor = destroyDescriptor;
			}
			else if (currentMode is PlaceMode || currentMode is BuildMode)
			{
				localIcon.weaponDescriptor = buildOrPlaceDescriptor;
			}
			else
			{
				localIcon.weaponDescriptor = genericDescriptor;
			}
			localIcon.UpdateIcon();
		}

		public void ResetAnimator()
		{
			animator.SetBool(Holding, value: false);
			animator.SetBool(Punch, value: false);
			animator.SetBool(Manipulating, value: false);
			animator.SetBool(Pinched, value: false);
			animator.SetBool(Crush, value: false);
			animator.SetBool(Point, value: false);
			animator.SetBool(Tap, value: false);
		}

		public GoreZone GetGoreZone()
		{
			if (!goreZone)
			{
				goreZone = new GameObject("Debug Gore Zone").AddComponent<GoreZone>();
				debugZone = goreZone;
			}
			return goreZone;
		}

		public void SelectObject(SpawnableObject obj)
		{
			ResetMode();
			menu.gameObject.SetActive(value: false);
			MonoSingleton<OptionsManager>.Instance.UnFreeze();
			if (SetArmMode(obj.spawnableType) is ArmModeWithHeldPreview armModeWithHeldPreview)
			{
				armModeWithHeldPreview.SetPreview(obj);
			}
		}

		public ISandboxArmMode SetArmMode(SpawnableType type)
		{
			Log.Info($"Setting arm mode to {type}");
			ISandboxArmMode sandboxArmMode = null;
			switch (type)
			{
				case SpawnableType.MoveHand:
					sandboxArmMode = new MoveMode();
					break;
				case SpawnableType.DestroyHand:
					sandboxArmMode = new DestroyMode();
					break;
				case SpawnableType.AlterHand:
					sandboxArmMode = new AlterMode();
					break;
				case SpawnableType.BuildHand:
					sandboxArmMode = new BuildMode();
					break;
				case SpawnableType.SimpleSpawn:
				case SpawnableType.Prop:
					sandboxArmMode = new PlaceMode();
					break;
			}
			SetArmMode(sandboxArmMode);
			return sandboxArmMode;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (currentMode == null)
			{
				ResetMode();
				SetArmMode(onEnableType);
			}
			else
			{
				currentMode.OnEnable(this);
			}
		}

		private void OnDisable()
		{
			currentMode?.OnDisable();
		}

		private new void OnDestroy()
		{
			ResetMode();
			if ((bool)MonoSingleton<CheatsController>.Instance)
			{
				MonoSingleton<CheatsController>.Instance.arm = null;
			}
		}

		public void ResetMode()
		{
			currentMode?.OnDestroy();
			currentMode = null;
		}

		private void FixedUpdate()
		{
			currentMode?.FixedUpdate();
		}

		private void Update()
		{
			if (Time.timeScale == 0f)
			{
				return;
			}
			if (menu != null && MonoSingleton<InputManager>.Instance.InputSource.Fire2.WasPerformedThisFrame && (currentMode == null || currentMode.CanOpenMenu))
			{
				menu.gameObject.SetActive(value: true);
				MonoSingleton<OptionsManager>.Instance.Freeze();
				return;
			}
			if (menu == null || !menu.gameObject.activeSelf)
			{
				if (!MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame)
				{
					currentMode?.OnPrimaryDown();
				}
				if (MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasCanceledThisFrame)
				{
					currentMode?.OnPrimaryUp();
				}
				if (!MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && MonoSingleton<InputManager>.Instance.InputSource.Fire2.WasPerformedThisFrame)
				{
					currentMode?.OnSecondaryDown();
				}
				if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.WasCanceledThisFrame)
				{
					currentMode?.OnSecondaryUp();
				}
			}
			if (currentMode != null && currentMode.Raycast)
			{
				hitSomething = Physics.Raycast(MonoSingleton<CameraController>.Instance.transform.position, MonoSingleton<CameraController>.Instance.transform.forward, out hit, 75f, raycastLayers);
			}
			currentMode?.Update();
		}

		public Vector2? GetHolderScreenPosition()
		{
			Vector3 vector = MonoSingleton<CameraController>.Instance.cam.WorldToScreenPoint(holder.position);
			if (vector.z < 0f)
			{
				return null;
			}
			return vector;
		}

		private void OnGUI()
		{
			if (!SandboxArmDebug.DebugActive || !(currentMode is ISandboxArmDebugGUI sandboxArmDebugGUI))
			{
				return;
			}
			Vector2? holderScreenPosition = GetHolderScreenPosition();
			if (holderScreenPosition.HasValue)
			{
				float num = holderScreenPosition.Value.x - 150f;
				float num2 = (float)Screen.height - holderScreenPosition.Value.y - 150f;
				GUILayout.BeginArea(new Rect(num, num2, Mathf.Min(300f, (float)Screen.width - num), Mathf.Min(250f, (float)Screen.height - num2)), GUI.skin.box);
				GUI.color = ((currentMode == null) ? Color.red : Color.green);
				GUILayout.Label((currentMode == null) ? "No mode is active" : currentMode.ToString());
				GUI.color = Color.white;
				if (!sandboxArmDebugGUI.OnGUI())
				{
					GUILayout.Label("No debug info is available right now.");
				}
				GUILayout.EndArea();
			}
		}
	}
}
using plog;
using UnityEngine;

namespace Sandbox.Arm
{

	public class AlterMode : ISandboxArmMode
	{
		private static readonly plog.Logger Log = new plog.Logger("AlterMode");

		private SandboxArm hostArm;

		private static readonly int Tap = Animator.StringToHash("Tap");

		private static readonly int Point = Animator.StringToHash("Point");

		private SandboxSpawnableInstance selected;

		public string Name => "Alter";

		public bool CanOpenMenu => selected == null;

		public bool Raycast => true;

		public virtual string Icon => "alter";

		public void EndSession()
		{
			if (!(selected == null))
			{
				if (!selected.frozen)
				{
					selected.Resume();
				}
				if ((bool)selected.attachedParticles)
				{
					Object.Destroy(selected.attachedParticles.gameObject);
				}
				selected = null;
			}
		}

		public void OnEnable(SandboxArm arm)
		{
			arm.ResetAnimator();
			arm.animator.SetBool(Point, value: true);
			hostArm = arm;
		}

		public void OnDisable()
		{
			if ((bool)selected && (bool)MonoSingleton<SandboxAlterMenu>.Instance)
			{
				MonoSingleton<SandboxAlterMenu>.Instance.Close();
			}
		}

		public void OnDestroy()
		{
			if ((bool)selected && (bool)MonoSingleton<SandboxAlterMenu>.Instance)
			{
				MonoSingleton<SandboxAlterMenu>.Instance.Close();
			}
		}

		public void Update()
		{
		}

		public void FixedUpdate()
		{
		}

		public void OnPrimaryDown()
		{
			if (!hostArm.hitSomething)
			{
				return;
			}
			if (!hostArm.hit.collider.TryGetComponent<SandboxSpawnableInstance>(out var component))
			{
				Transform transform = hostArm.hit.collider.transform;
				if (transform.parent == null || !transform.parent.TryGetComponent<SandboxSpawnableInstance>(out component))
				{
					return;
				}
			}
			if (!selected)
			{
				OpenProp(component);
				hostArm.animator.SetTrigger(Tap);
			}
		}

		public void OpenProp(SandboxSpawnableInstance prop)
		{
			Log.Info($"Opening prop {prop}");
			selected = prop;
			MonoSingleton<SandboxAlterMenu>.Instance.editedObject = prop;
			MonoSingleton<SandboxAlterMenu>.Instance.Show(prop, this);
			prop.attachedParticles = Object.Instantiate(hostArm.manipulateEffect, prop.transform, worldPositionStays: true);
			prop.attachedParticles.transform.position = prop.collider.bounds.center;
		}

		public void OnPrimaryUp()
		{
		}

		public void OnSecondaryDown()
		{
			if (!(selected == null))
			{
				MonoSingleton<SandboxAlterMenu>.Instance.Close();
			}
		}

		public void OnSecondaryUp()
		{
		}
	}
}
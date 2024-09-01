using UnityEngine;

namespace ULTRAKILL.Cheats.UnityEditor
{

	public class OverwriteUnlocks : ICheat
	{
		private static OverwriteUnlocks _lastInstance;

		private bool active;

		public static bool Enabled
		{
			get
			{
				if (Application.isEditor && Debug.isDebugBuild && _lastInstance != null)
				{
					return _lastInstance.active;
				}
				return false;
			}
		}

		public string LongName => "Overwrite Unlocks";

		public string Identifier => "ultrakill.editor.overwrite-unlocks";

		public string ButtonEnabledOverride => null;

		public string ButtonDisabledOverride => null;

		public string Icon => null;

		public bool IsActive => active;

		public bool DefaultState => false;

		public StatePersistenceMode PersistenceMode => StatePersistenceMode.Persistent;

		public void Enable()
		{
			active = Application.isEditor;
			_lastInstance = this;
			if ((bool)MonoSingleton<SpawnMenu>.Instance)
			{
				MonoSingleton<SpawnMenu>.Instance.RebuildMenu();
			}
		}

		public void Disable()
		{
			active = false;
		}

		public void Update()
		{
		}
	}
}
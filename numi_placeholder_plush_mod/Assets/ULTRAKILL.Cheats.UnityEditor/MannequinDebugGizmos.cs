using UnityEngine;

namespace ULTRAKILL.Cheats.UnityEditor
{

	public class MannequinDebugGizmos : ICheat
	{
		private static MannequinDebugGizmos _lastInstance;

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

		public string LongName => "Mannequin Debug Gizmos";

		public string Identifier => "ultrakill.editor.debug-gizmos";

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
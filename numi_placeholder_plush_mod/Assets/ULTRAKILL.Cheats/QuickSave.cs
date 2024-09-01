namespace ULTRAKILL.Cheats
{
    public class QuickSave : ICheat
    {
    	private PrefsManager prefsManager;

    	private SandboxSaver saver;

    	public string LongName => "Quick Save";

    	public string Identifier => "ultrakill.sandbox.quick-save";

    	public string ButtonEnabledOverride => "SAVE";

    	public string ButtonDisabledOverride => "NEW SAVE";

    	public string Icon => "save";

    	public bool IsActive
    	{
    		get
    		{
    			if (MonoSingleton<SandboxSaver>.Instance != null)
    			{
    				return !string.IsNullOrEmpty(MonoSingleton<SandboxSaver>.Instance.activeSave);
    			}
    			return false;
    		}
    	}

    	public bool DefaultState => false;

    	public StatePersistenceMode PersistenceMode => StatePersistenceMode.NotPersistent;

    	public void Enable()
    	{
    		prefsManager = MonoSingleton<PrefsManager>.Instance;
    		saver = MonoSingleton<SandboxSaver>.Instance;
    		saver.QuickSave();
    	}

    	public void Disable()
    	{
    		if (prefsManager == null)
    		{
    			prefsManager = MonoSingleton<PrefsManager>.Instance;
    		}
    		if (prefsManager.GetBool("sandboxSaveOverwriteWarnings"))
    		{
    			MonoSingleton<SandboxSaveConfirmation>.Instance.DisplayDialog();
    			return;
    		}
    		if (saver == null)
    		{
    			saver = MonoSingleton<SandboxSaver>.Instance;
    		}
    		saver.Save(MonoSingleton<SandboxSaver>.Instance.activeSave);
    	}

    	public void Update()
    	{
    	}
    }
}

namespace ULTRAKILL.Cheats
{
    public class HideWeapons : ICheat
    {
    	private bool gunControlChanged;

    	public static bool Active
    	{
    		get
    		{
    			if (Instance != null)
    			{
    				return Instance.IsActive;
    			}
    			return false;
    		}
    	}

    	private static HideWeapons Instance { get; set; }

    	public string LongName => "Hide Weapons";

    	public string Identifier => "ultrakill.hide-weapons";

    	public string ButtonEnabledOverride { get; }

    	public string ButtonDisabledOverride { get; }

    	public string Icon { get; }

    	public bool IsActive { get; private set; }

    	public bool DefaultState => false;

    	public StatePersistenceMode PersistenceMode => StatePersistenceMode.NotPersistent;

    	public void Enable()
    	{
    		Instance = this;
    		IsActive = true;
    	}

    	public void Disable()
    	{
    		IsActive = false;
    		if (gunControlChanged && MonoSingleton<GunControl>.Instance != null && !MonoSingleton<GunControl>.Instance.activated)
    		{
    			MonoSingleton<GunControl>.Instance.YesWeapon();
    		}
    	}

    	public void Update()
    	{
    		if (IsActive && MonoSingleton<GunControl>.Instance != null && MonoSingleton<GunControl>.Instance.activated)
    		{
    			gunControlChanged = true;
    			MonoSingleton<GunControl>.Instance.NoWeapon();
    		}
    	}
    }
}

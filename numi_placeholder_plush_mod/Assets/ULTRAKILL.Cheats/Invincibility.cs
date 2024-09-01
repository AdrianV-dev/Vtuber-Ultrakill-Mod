namespace ULTRAKILL.Cheats
{
    public class Invincibility : ICheat
    {
    	private static Invincibility _lastInstance;

    	private bool active;

    	public static bool Enabled
    	{
    		get
    		{
    			if (_lastInstance != null)
    			{
    				return _lastInstance.active;
    			}
    			return false;
    		}
    	}

    	public string LongName => "Invincibility";

    	public string Identifier => "ultrakill.invincibility";

    	public string ButtonEnabledOverride { get; }

    	public string ButtonDisabledOverride { get; }

    	public string Icon => "invincibility";

    	public bool IsActive => active;

    	public bool DefaultState { get; }

    	public StatePersistenceMode PersistenceMode => StatePersistenceMode.Persistent;

    	public void Enable()
    	{
    		active = true;
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

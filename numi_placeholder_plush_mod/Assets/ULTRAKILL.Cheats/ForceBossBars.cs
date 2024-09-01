namespace ULTRAKILL.Cheats
{
    public class ForceBossBars : ICheat
    {
    	private bool active;

    	private static ForceBossBars _lastInstance;

    	public static bool Active
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

    	public string LongName => "Force Enemy Boss Bars";

    	public string Identifier => "ultrakill.debug.force-boss-bars";

    	public string ButtonEnabledOverride { get; }

    	public string ButtonDisabledOverride { get; }

    	public string Icon => null;

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

namespace ULTRAKILL.Cheats
{
    public class EnemyIdentifierDebug : ICheat
    {
    	private bool active;

    	private static EnemyIdentifierDebug _lastInstance;

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

    	public string LongName => "Enemy Identifier Debug";

    	public string Identifier => "ultrakill.debug.enemy-identifier";

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

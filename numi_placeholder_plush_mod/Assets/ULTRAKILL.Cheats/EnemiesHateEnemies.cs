namespace ULTRAKILL.Cheats
{
    public class EnemiesHateEnemies : ICheat
    {
    	private static EnemiesHateEnemies _lastInstance;

    	private bool active;

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

    	public string LongName => "Enemies Attack Each Other";

    	public string Identifier => "ultrakill.enemy-hate-enemy";

    	public string ButtonEnabledOverride { get; }

    	public string ButtonDisabledOverride { get; }

    	public string Icon => "enemy-hate-enemy";

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

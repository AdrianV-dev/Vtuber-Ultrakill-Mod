using UnityEngine;

namespace ULTRAKILL.Cheats
{
    public class FullBright : ICheat
    {
    	private static FullBright _instance;

    	private bool active;

    	private bool lastFogEnabled;

    	private Color lastAmbientColor;

    	private GameObject lightObject;

    	private static Color brightAmbientColor = new Color(0.2f, 0.2f, 0.2f);

    	public static bool Enabled => _instance?.IsActive ?? false;

    	public string LongName => "Fullbright";

    	public string Identifier => "ultrakill.full-bright";

    	public string ButtonEnabledOverride => null;

    	public string ButtonDisabledOverride => null;

    	public string Icon => "light";

    	public bool IsActive => active;

    	public bool DefaultState => false;

    	public StatePersistenceMode PersistenceMode => StatePersistenceMode.Persistent;

    	public void Enable()
    	{
    		_instance = this;
    		active = true;
    		lightObject = Object.Instantiate(MonoSingleton<CheatsController>.Instance.fullBrightLight);
    		lastFogEnabled = RenderSettings.fog;
    		RenderSettings.fog = false;
    		lastAmbientColor = RenderSettings.ambientLight;
    		RenderSettings.ambientLight = brightAmbientColor;
    	}

    	public void Disable()
    	{
    		active = false;
    		Object.Destroy(lightObject);
    		RenderSettings.fog = lastFogEnabled;
    		RenderSettings.ambientLight = lastAmbientColor;
    	}

    	public void Update()
    	{
    		if (active)
    		{
    			if (RenderSettings.fog)
    			{
    				lastFogEnabled = true;
    				RenderSettings.fog = false;
    			}
    			if (RenderSettings.ambientLight != brightAmbientColor)
    			{
    				lastAmbientColor = RenderSettings.ambientLight;
    				RenderSettings.ambientLight = brightAmbientColor;
    			}
    		}
    	}
    }
}

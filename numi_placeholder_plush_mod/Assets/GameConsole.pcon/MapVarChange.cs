using Logic;
using pcon.core.Attributes;
using pcon.core.Interfaces;
using plog;

namespace GameConsole.pcon
{
    [RegisterIncomingMessage("ultrakill.mapvar.update")]
    public class MapVarChange : IReceive
    {
    	private static readonly Logger Log = new Logger("MapVarChange");

    	private const string Type = "ultrakill.mapvar.update";

    	public MapVarField variable;

    	public string type => "ultrakill.mapvar.update";

    	public void Receive()
    	{
    		Log.Fine($"Received change from console <b>{variable.value.type}</b> - <b>{variable.value.value}</b>");
    		switch (variable.value.type)
    		{
    		case "System.Int32":
    		{
    			if (int.TryParse(variable.value.value.ToString(), out var result2))
    			{
    				MonoSingleton<MapVarManager>.Instance.SetInt(variable.name, result2);
    			}
    			else
    			{
    				Log.Warning($"Failed to parse {variable.value.value} as int");
    			}
    			break;
    		}
    		case "System.Boolean":
    			if (variable.value.value is bool value)
    			{
    				MonoSingleton<MapVarManager>.Instance.SetBool(variable.name, value);
    			}
    			else
    			{
    				Log.Warning($"Failed to parse {variable.value.value} as bool");
    			}
    			break;
    		case "System.Single":
    		{
    			if (float.TryParse(variable.value.value.ToString(), out var result))
    			{
    				MonoSingleton<MapVarManager>.Instance.SetFloat(variable.name, result);
    			}
    			else
    			{
    				Log.Warning($"Failed to parse {variable.value.value} as float");
    			}
    			break;
    		}
    		case "System.String":
    			MonoSingleton<MapVarManager>.Instance.SetString(variable.name, variable.value.value.ToString());
    			break;
    		default:
    			Log.Error("Unknown type " + variable.value.type);
    			break;
    		}
    	}
    }
}

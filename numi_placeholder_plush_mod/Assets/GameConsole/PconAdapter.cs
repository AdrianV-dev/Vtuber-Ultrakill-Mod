using System;
using System.Reflection;
using GameConsole.pcon;
using pcon.core;
using pcon.core.Models;
using plog;

namespace GameConsole
{
    public class PconAdapter
    {
    	private static readonly Logger Log = new Logger("PconAdapter");

    	private Assembly pconAssmebly;

    	private Type pconClientType;

    	private bool startCalled;

    	public bool PConLibraryExists()
    	{
    		if (pconAssmebly != null)
    		{
    			return true;
    		}
    		Log.Info("Looking for the pcon.unity library...");
    		string value = "pcon.unity";
    		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    		foreach (Assembly assembly in assemblies)
    		{
    			if (assembly.FullName.StartsWith(value))
    			{
    				Log.Info("Found the pcon.unity library!");
    				pconAssmebly = assembly;
    				pconClientType = pconAssmebly.GetType("pcon.PConClient");
    				return true;
    			}
    		}
    		return false;
    	}

    	public void StartPConClient(Action<string> onExecute, Action onGameModified)
    	{
    		if (PConLibraryExists() && !startCalled)
    		{
    			Log.Info("Starting the pcon.unity client...");
    			startCalled = true;
    			MethodInfo method = pconClientType.GetMethod("StartClient", BindingFlags.Static | BindingFlags.Public);
    			if (method != null)
    			{
    				Log.Info("Starting the pcon.unity client!");
    				PCon.MountHandler(new Handler
    				{
    					onExecute = onExecute,
    					onGameModified = onGameModified
    				});
    				method.Invoke(null, new object[1]);
    				MonoSingleton<MapVarRelay>.Instance.enabled = true;
    				PCon.RegisterFeature("ultrakill");
    			}
    			else
    			{
    				Log.Info("Could not find the pcon.unity client's StartClient method!");
    			}
    		}
    	}
    }
}

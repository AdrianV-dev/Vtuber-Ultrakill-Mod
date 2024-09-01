using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using plog;
using Sandbox;
using UnityEngine;
using UnityEngine.SceneManagement;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class SandboxSaver : MonoSingleton<SandboxSaver>
{
	private static readonly plog.Logger Log = new plog.Logger("SandboxSaver");

	public const string SaveExtension = ".pitr";

	[SerializeField]
	private SpawnableObjectsDatabase objects;

	private Dictionary<string, SpawnableObject> registeredObjects;

	public string activeSave;

	public static string SavePath => Path.Combine(GameProgressSaver.BaseSavePath, "Sandbox");

	private static void SetupDirs()
	{
		if (!Directory.Exists(SavePath))
		{
			Directory.CreateDirectory(SavePath);
		}
	}

	public string[] ListSaves()
	{
		SetupDirs();
		return (from f in new DirectoryInfo(SavePath).GetFileSystemInfos()
			orderby f.LastWriteTime descending
			select Path.GetFileNameWithoutExtension(f.Name)).ToArray();
	}

	public void QuickSave()
	{
		Save($"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}");
	}

	public void QuickLoad()
	{
		string[] array = ListSaves();
		if (array.Length != 0)
		{
			Load(array[0]);
		}
	}

	public void Delete(string name)
	{
		SetupDirs();
		string path = Path.Combine(SavePath, name + ".pitr");
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public void Save(string name)
	{
		SetupDirs();
		activeSave = name;
		MonoSingleton<CheatsManager>.Instance.RefreshCheatStates();
		CreateSaveAndWrite(name);
	}

	public void Load(string name)
	{
		Log.Info("Loading save: " + name);
		SetupDirs();
		Clear();
		activeSave = name;
		MonoSingleton<CheatsManager>.Instance.RefreshCheatStates();
		RebuildObjectList();
		SandboxSaveData sandboxSaveData = JsonConvert.DeserializeObject<SandboxSaveData>(File.ReadAllText(Path.Combine(SavePath, name + ".pitr")));
		Log.Fine($"Loaded {sandboxSaveData.Blocks.Length} blocks\nLoaded {sandboxSaveData.Props.Length} props");
		Log.Fine("Save Version: " + sandboxSaveData.SaveVersion);
		Vector3? vector = null;
		Vector3 position = MonoSingleton<NewMovement>.Instance.transform.position;
		SavedProp[] props = sandboxSaveData.Props;
		foreach (SavedProp savedProp in props)
		{
			RecreateProp(savedProp, sandboxSaveData.SaveVersion > 1);
			if (!(savedProp.ObjectIdentifier != "ultrakill.spawn-point"))
			{
				if (!vector.HasValue)
				{
					vector = savedProp.Position.ToVector3();
				}
				else if (Vector3.Distance(position, savedProp.Position.ToVector3()) < Vector3.Distance(position, vector.Value))
				{
					vector = savedProp.Position.ToVector3();
				}
			}
		}
		if (vector.HasValue)
		{
			MonoSingleton<NewMovement>.Instance.transform.position = vector.Value;
			MonoSingleton<NewMovement>.Instance.rb.velocity = Vector3.zero;
		}
		SavedBlock[] blocks = sandboxSaveData.Blocks;
		foreach (SavedBlock block in blocks)
		{
			RecreateBlock(block);
		}
		MonoSingleton<SandboxNavmesh>.Instance.Rebake();
		List<SandboxEnemy> list = new List<SandboxEnemy>();
		SavedEnemy[] enemies = sandboxSaveData.Enemies;
		foreach (SavedEnemy genericObject in enemies)
		{
			SandboxEnemy sandboxEnemy = RecreateEnemy(genericObject, sandboxSaveData.SaveVersion > 1);
			sandboxEnemy.Pause(freeze: false);
			list.Add(sandboxEnemy);
		}
		StartCoroutine(PostLoadAndBake(list));
	}

	private IEnumerator PostLoadAndBake(List<SandboxEnemy> enemies)
	{
		yield return new WaitForEndOfFrame();
		List<SandboxEnemy> enemiesToFreezeBack = new List<SandboxEnemy>();
		foreach (SandboxEnemy enemy in enemies)
		{
			bool frozen = enemy.frozen;
			enemy.Resume();
			if (frozen)
			{
				enemiesToFreezeBack.Add(enemy);
			}
		}
		yield return new WaitForEndOfFrame();
		foreach (SandboxEnemy item in enemiesToFreezeBack)
		{
			item.Pause();
		}
	}

	public SandboxEnemy RecreateEnemy(SavedGeneric genericObject, bool newSizing)
	{
		if (!registeredObjects.TryGetValue(genericObject.ObjectIdentifier, out var value))
		{
			Log.Error(genericObject.ObjectIdentifier + " missing from registered objects");
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(value.gameObject);
		gameObject.transform.position = genericObject.Position.ToVector3();
		if (!newSizing)
		{
			gameObject.transform.localScale = genericObject.Scale.ToVector3();
		}
		if (gameObject.TryGetComponent<KeepInBounds>(out var component))
		{
			component.ForceApproveNewPosition();
		}
		SandboxEnemy sandboxEnemy = gameObject.AddComponent<SandboxEnemy>();
		sandboxEnemy.sourceObject = registeredObjects[genericObject.ObjectIdentifier];
		sandboxEnemy.enemyId.checkingSpawnStatus = false;
		sandboxEnemy.RestoreRadiance(((SavedEnemy)genericObject).Radiance);
		if (genericObject is SavedPhysical { Kinematic: not false })
		{
			sandboxEnemy.Pause();
		}
		if (newSizing)
		{
			sandboxEnemy.SetSize(genericObject.Scale.ToVector3());
		}
		sandboxEnemy.disallowManipulation = genericObject.DisallowManipulation;
		sandboxEnemy.disallowFreezing = genericObject.DisallowFreezing;
		ApplyData(gameObject, genericObject.Data);
		MonoSingleton<SandboxNavmesh>.Instance.EnsurePositionWithinBounds(gameObject.transform.position);
		return sandboxEnemy;
	}

	private void RecreateProp(SavedProp prop, bool newSizing)
	{
		if (!registeredObjects.TryGetValue(prop.ObjectIdentifier, out var value))
		{
			Log.Error(prop.ObjectIdentifier + " missing from registered objects");
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(value.gameObject);
		gameObject.transform.SetPositionAndRotation(prop.Position.ToVector3(), prop.Rotation.ToQuaternion());
		if (!newSizing)
		{
			gameObject.transform.localScale = prop.Scale.ToVector3();
		}
		SandboxProp component = gameObject.GetComponent<SandboxProp>();
		component.sourceObject = registeredObjects[prop.ObjectIdentifier];
		if (newSizing)
		{
			component.SetSize(prop.Scale.ToVector3());
		}
		if (prop.Kinematic)
		{
			component.Pause();
		}
		else
		{
			component.Resume();
		}
		component.disallowManipulation = prop.DisallowManipulation;
		component.disallowFreezing = prop.DisallowFreezing;
		ApplyData(gameObject, prop.Data);
	}

	private void RecreateBlock(SavedBlock block)
	{
		if (!registeredObjects.TryGetValue(block.ObjectIdentifier, out var value))
		{
			Log.Error(block.ObjectIdentifier + " missing from registered objects");
			return;
		}
		GameObject gameObject = SandboxUtils.CreateFinalBlock(value, block.Position.ToVector3(), block.BlockSize.ToVector3(), value.isWater);
		gameObject.transform.rotation = block.Rotation.ToQuaternion();
		SandboxProp component = gameObject.GetComponent<SandboxProp>();
		component.sourceObject = registeredObjects[block.ObjectIdentifier];
		if (block.Kinematic)
		{
			component.Pause();
		}
		else
		{
			component.Resume();
		}
		component.disallowManipulation = block.DisallowManipulation;
		component.disallowFreezing = block.DisallowFreezing;
		ApplyData(gameObject, block.Data);
	}

	private void ApplyData(GameObject go, SavedAlterData[] data)
	{
		if (data == null)
		{
			return;
		}
		IAlter[] componentsInChildren = go.GetComponentsInChildren<IAlter>();
		foreach (IAlter alterComponent in componentsInChildren)
		{
			if (alterComponent.alterKey == null)
			{
				continue;
			}
			if (!data.Select((SavedAlterData d) => d.Key).Contains(alterComponent.alterKey))
			{
				Log.Warning("No data for " + alterComponent.alterKey + " on " + go.name);
				continue;
			}
			SavedAlterData savedAlterData = data.FirstOrDefault((SavedAlterData d) => d.Key == alterComponent.alterKey);
			if (savedAlterData == null)
			{
				continue;
			}
			SavedAlterOption[] options2 = savedAlterData.Options;
			foreach (SavedAlterOption options in options2)
			{
				if (options.BoolValue.HasValue && alterComponent is IAlterOptions<bool> alterOptions)
				{
					AlterOption<bool> alterOption = alterOptions.options.FirstOrDefault((AlterOption<bool> o) => o.key == options.Key);
					if (alterOption == null)
					{
						continue;
					}
					alterOption.callback?.Invoke(options.BoolValue.Value);
				}
				if (options.FloatValue.HasValue && alterComponent is IAlterOptions<float> alterOptions2)
				{
					AlterOption<float> alterOption2 = alterOptions2.options.FirstOrDefault((AlterOption<float> o) => o.key == options.Key);
					if (alterOption2 == null)
					{
						continue;
					}
					alterOption2.callback?.Invoke(options.FloatValue.Value);
				}
				if (options.IntValue.HasValue && alterComponent is IAlterOptions<int> alterOptions3)
				{
					alterOptions3.options.FirstOrDefault((AlterOption<int> o) => o.key == options.Key)?.callback?.Invoke(options.IntValue.Value);
				}
			}
		}
	}

	public void RebuildObjectList()
	{
		if (registeredObjects == null)
		{
			registeredObjects = new Dictionary<string, SpawnableObject>();
		}
		registeredObjects.Clear();
		RegisterObjects(objects.objects);
		RegisterObjects(objects.enemies);
		RegisterObjects(objects.sandboxTools);
		RegisterObjects(objects.sandboxObjects);
		RegisterObjects(objects.specialSandbox);
	}

	private void RegisterObjects(SpawnableObject[] objs)
	{
		foreach (SpawnableObject spawnableObject in objs)
		{
			if (!string.IsNullOrEmpty(spawnableObject.identifier) && !registeredObjects.ContainsKey(spawnableObject.identifier))
			{
				registeredObjects.Add(spawnableObject.identifier, spawnableObject);
			}
		}
	}

	public static void Clear()
	{
		DefaultSandboxCheckpoint defaultSandboxCheckpoint = MonoSingleton<DefaultSandboxCheckpoint>.Instance;
		if (defaultSandboxCheckpoint == null)
		{
			MonoSingleton<StatsManager>.Instance.currentCheckPoint = null;
		}
		else
		{
			MonoSingleton<StatsManager>.Instance.currentCheckPoint = defaultSandboxCheckpoint.checkpoint;
		}
		SandboxSpawnableInstance[] array = UnityEngine.Object.FindObjectsOfType<SandboxSpawnableInstance>();
		foreach (SandboxSpawnableInstance sandboxSpawnableInstance in array)
		{
			if (sandboxSpawnableInstance.enabled)
			{
				UnityEngine.Object.Destroy(sandboxSpawnableInstance.gameObject);
			}
		}
		Resources.UnloadUnusedAssets();
		MonoSingleton<SandboxNavmesh>.Instance.ResetSizeToDefault();
		MonoSingleton<SandboxSaver>.Instance.activeSave = null;
		MonoSingleton<CheatsManager>.Instance.RefreshCheatStates();
	}

	private static void CreateSaveAndWrite(string name)
	{
		Log.Info("Creating save");
		SandboxProp[] array = UnityEngine.Object.FindObjectsOfType<SandboxProp>();
		Log.Fine($"{array.Length} props found");
		BrushBlock[] array2 = UnityEngine.Object.FindObjectsOfType<BrushBlock>();
		Log.Fine($"{array2.Length} procedural blocks found");
		SandboxEnemy[] array3 = UnityEngine.Object.FindObjectsOfType<SandboxEnemy>();
		Log.Fine($"{array3.Length} sandbox enemies found");
		List<SavedBlock> list = new List<SavedBlock>();
		BrushBlock[] array4 = array2;
		foreach (BrushBlock brushBlock in array4)
		{
			if (brushBlock.enabled)
			{
				Log.Finer($"Position: {brushBlock.transform.position}\nRotation: {brushBlock.transform.rotation}\nSize: {brushBlock.DataSize}\nType: {brushBlock.Type}");
				list.Add(brushBlock.SaveBrushBlock());
			}
		}
		List<SavedProp> list2 = new List<SavedProp>();
		SandboxProp[] array5 = array;
		foreach (SandboxProp sandboxProp in array5)
		{
			if (!sandboxProp.GetComponent<BrushBlock>() && sandboxProp.enabled)
			{
				Log.Finer($"Position: {sandboxProp.transform.position}\nRotation: {sandboxProp.transform.rotation}");
				list2.Add(sandboxProp.SaveProp());
			}
		}
		List<SavedEnemy> list3 = new List<SavedEnemy>();
		SandboxEnemy[] array6 = array3;
		foreach (SandboxEnemy sandboxEnemy in array6)
		{
			if (sandboxEnemy.enabled)
			{
				SavedEnemy savedEnemy = sandboxEnemy.SaveEnemy();
				if (savedEnemy != null)
				{
					list3.Add(savedEnemy);
				}
			}
		}
		string contents = JsonConvert.SerializeObject(new SandboxSaveData
		{
			MapName = SceneManager.GetActiveScene().name,
			Blocks = list.ToArray(),
			Props = list2.ToArray(),
			Enemies = list3.ToArray()
		});
		File.WriteAllText(Path.Combine(SavePath, name + ".pitr"), contents);
	}
}

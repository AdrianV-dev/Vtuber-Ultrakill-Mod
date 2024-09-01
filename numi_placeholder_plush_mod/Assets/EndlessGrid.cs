using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class EndlessGrid : MonoSingleton<EndlessGrid>
{
	public bool customPatternMode;

	public ArenaPattern[] customPatterns;

	public const int ArenaSize = 16;

	[SerializeField]
	private ArenaPattern[] patterns;

	private int[] usedPatterns;

	[SerializeField]
	private List<CyberPooledPrefab> jumpPadPool;

	private int jumpPadSelector;

	[SerializeField]
	private CyberGrindNavHelper nvmhlpr;

	[SerializeField]
	private PrefabDatabase prefabs;

	[SerializeField]
	private GameObject gridCube;

	[SerializeField]
	private LayerMask prefabSpawnLayerCheck;

	[SerializeField]
	private float offset = 5f;

	[HideInInspector]
	public EndlessCube[][] cubes;

	private int incompleteBlocks;

	private ArenaPattern currentPattern;

	public NavMeshSurface nms;

	private ActivateNextWave anw;

	public int enemyAmount = 999;

	public int tempEnemyAmount;

	private int points;

	private int maxPoints = 10;

	public int currentWave;

	private int currentPatternNum = -1;

	private List<Vector2> meleePositions = new List<Vector2>();

	private int usedMeleePositions;

	private List<Vector2> projectilePositions = new List<Vector2>();

	private int usedProjectilePositions;

	private List<GameObject> spawnedEnemies = new List<GameObject>();

	private List<GameObject> spawnedPrefabs = new List<GameObject>();

	private List<EnemyTypeTracker> spawnedEnemyTypes = new List<EnemyTypeTracker>();

	private int incompletePrefabs;

	private GoreZone gz;

	private int specialAntiBuffer;

	private int massAntiBuffer;

	private float uncommonAntiBuffer;

	public Text waveNumberText;

	public Text enemiesLeftText;

	public bool crowdReactions;

	private CrowdReactions crorea;

	private int hideousMasses;

	private NewMovement nmov;

	private WeaponCharges wc;

	private Material[] mats;

	private Color targetColor;

	private bool testMode;

	private bool lastEnemyMode;

	public Transform enemyToTrack;

	private float currentGlow = 0.2f;

	public float glowMultiplier = 1f;

	private GameObject combinedGridStaticObject;

	private MeshRenderer combinedGridStaticMeshRenderer;

	private MeshFilter combinedGridStaticMeshFilter;

	private Mesh combinedGridStaticMesh;

	private static readonly int WorldOffset = Shader.PropertyToID("_WorldOffset");

	private static readonly int GradientSpeed = Shader.PropertyToID("_GradientSpeed");

	private static readonly int GradientFalloff = Shader.PropertyToID("_GradientFalloff");

	private static readonly int GradientScale = Shader.PropertyToID("_GradientScale");

	private static readonly int PcGamerMode = Shader.PropertyToID("_PCGamerMode");

	public int startWave;

	private ArenaPattern[] CurrentPatternPool
	{
		get
		{
			if (!customPatternMode)
			{
				return patterns;
			}
			return customPatterns;
		}
	}

	public void TrySetupStaticGridMesh()
	{
		if (incompleteBlocks == 0 && incompletePrefabs == 0)
		{
			SetupStaticGridMesh();
		}
	}

	public void SetupStaticGridMesh()
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		if (combinedGridStaticObject == null)
		{
			combinedGridStaticObject = new GameObject("Combined Static Mesh");
			combinedGridStaticObject.transform.parent = base.transform;
			combinedGridStaticObject.layer = LayerMask.NameToLayer("Outdoors");
			combinedGridStaticMeshRenderer = combinedGridStaticObject.AddComponent<MeshRenderer>();
			combinedGridStaticMeshFilter = combinedGridStaticObject.AddComponent<MeshFilter>();
		}
		combinedGridStaticObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		combinedGridStaticObject.transform.localScale = Vector3.one;
		if (combinedGridStaticMesh == null)
		{
			combinedGridStaticMesh = new Mesh();
		}
		combinedGridStaticMesh.Clear();
		List<Mesh> list = new List<Mesh>();
		List<Material> list2 = new List<Material>();
		bool flag = false;
		for (int i = 0; i < cubes[0][0].MeshRenderer.sharedMaterials.Length; i++)
		{
			Material item = cubes[0][0].MeshRenderer.sharedMaterials[i];
			if (!list2.Contains(item))
			{
				list2.Add(item);
			}
		}
		for (int j = 0; j < list2.Count; j++)
		{
			Mesh mesh = new Mesh();
			List<CombineInstance> list3 = new List<CombineInstance>();
			for (int k = 0; k < 16; k++)
			{
				for (int l = 0; l < 16; l++)
				{
					EndlessCube endlessCube = cubes[k][l];
					if (!(endlessCube == null))
					{
						list3.Add(new CombineInstance
						{
							transform = endlessCube.MeshRenderer.localToWorldMatrix,
							mesh = endlessCube.MeshFilter.sharedMesh,
							subMeshIndex = j
						});
						endlessCube.MeshRenderer.enabled = false;
					}
				}
			}
			if (j == 1)
			{
				foreach (GameObject spawnedPrefab in spawnedPrefabs)
				{
					if (!spawnedPrefab.TryGetComponent<EndlessStairs>(out var component))
					{
						continue;
					}
					if (component.ActivateFirst)
					{
						if (!flag)
						{
							Material[] sharedMaterials = component.PrimaryMeshRenderer.sharedMaterials;
							foreach (Material item2 in sharedMaterials)
							{
								if (!list2.Contains(item2))
								{
									list2.Add(item2);
								}
							}
							flag = true;
						}
						list3.Add(new CombineInstance
						{
							transform = component.PrimaryMeshRenderer.localToWorldMatrix,
							mesh = component.PrimaryMeshFilter.sharedMesh
						});
						component.PrimaryMeshRenderer.enabled = false;
					}
					if (!component.ActivateSecond)
					{
						continue;
					}
					if (!flag)
					{
						Material[] sharedMaterials = component.SecondaryMeshRenderer.sharedMaterials;
						foreach (Material item3 in sharedMaterials)
						{
							if (!list2.Contains(item3))
							{
								list2.Add(item3);
							}
						}
						flag = true;
					}
					list3.Add(new CombineInstance
					{
						transform = component.SecondaryMeshRenderer.localToWorldMatrix,
						mesh = component.SecondaryMeshFilter.sharedMesh
					});
					component.SecondaryMeshRenderer.enabled = false;
				}
			}
			mesh.CombineMeshes(list3.ToArray(), mergeSubMeshes: true, useMatrices: true);
			list.Add(mesh);
		}
		CombineInstance[] array = new CombineInstance[list.Count];
		for (int n = 0; n < list.Count; n++)
		{
			array[n] = new CombineInstance
			{
				mesh = list[n]
			};
		}
		combinedGridStaticMesh.CombineMeshes(array, mergeSubMeshes: false, useMatrices: false);
		combinedGridStaticMesh.Optimize();
		combinedGridStaticMesh.RecalculateBounds();
		combinedGridStaticMesh.RecalculateNormals();
		combinedGridStaticMesh.UploadMeshData(markNoLongerReadable: false);
		combinedGridStaticMeshRenderer.sharedMaterials = list2.ToArray();
		combinedGridStaticMeshRenderer.enabled = true;
		combinedGridStaticMeshFilter.sharedMesh = combinedGridStaticMesh;
		for (int num = 0; num < list.Count; num++)
		{
			UnityEngine.Object.Destroy(list[num]);
		}
		list.Clear();
		stopwatch.Stop();
		UnityEngine.Debug.Log($"Combined arena mesh in {stopwatch.ElapsedMilliseconds} ms");
	}

	private void Start()
	{
		nms = GetComponent<NavMeshSurface>();
		anw = GetComponent<ActivateNextWave>();
		gz = GoreZone.ResolveGoreZone(base.transform);
		cubes = new EndlessCube[16][];
		for (int i = 0; i < 16; i++)
		{
			cubes[i] = new EndlessCube[16];
			for (int j = 0; j < 16; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(gridCube, base.transform, worldPositionStays: true);
				gameObject.SetActive(value: true);
				gameObject.transform.localPosition = new Vector3((float)i * offset, 0f, (float)j * offset);
				cubes[i][j] = gameObject.GetComponent<EndlessCube>();
				cubes[i][j].positionOnGrid = new Vector2Int(i, j);
			}
		}
		for (int k = 0; k < CurrentPatternPool.Length; k++)
		{
			ArenaPattern arenaPattern = CurrentPatternPool[k];
			int num = UnityEngine.Random.Range(k, CurrentPatternPool.Length);
			CurrentPatternPool[k] = CurrentPatternPool[num];
			CurrentPatternPool[num] = arenaPattern;
		}
		crorea = MonoSingleton<CrowdReactions>.Instance;
		if (crorea != null)
		{
			crowdReactions = true;
		}
		ShuffleDecks();
		PresenceController.UpdateCyberGrindWave(0);
		mats = GetComponentInChildren<MeshRenderer>().sharedMaterials;
		Material[] array = mats;
		foreach (Material obj in array)
		{
			obj.SetColor(UKShaderProperties.EmissiveColor, Color.blue);
			obj.SetFloat(UKShaderProperties.EmissiveIntensity, 0.2f * glowMultiplier);
			obj.SetFloat("_PCGamerMode", 0f);
			obj.SetFloat("_GradientScale", 2f);
			obj.SetFloat("_GradientFalloff", 5f);
			obj.SetFloat("_GradientSpeed", 10f);
			obj.SetVector("_WorldOffset", new Vector4(0f, 0f, 62.5f, 0f));
			targetColor = Color.blue;
		}
		TrySetupStaticGridMesh();
	}

	private void LastEnemyMode()
	{
		lastEnemyMode = true;
		EnemyIdentifier[] componentsInChildren = GetComponentsInChildren<EnemyIdentifier>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].dead)
			{
				enemyToTrack = componentsInChildren[i].transform;
				break;
			}
		}
		Material[] array = mats;
		foreach (Material obj in array)
		{
			if (currentWave < 20)
			{
				currentGlow = 0.5f;
			}
			else
			{
				currentGlow = 1f;
			}
			obj.SetFloat(UKShaderProperties.EmissiveIntensity, currentGlow * glowMultiplier);
			obj.SetFloat(GradientScale, 0.5f);
			obj.SetFloat(GradientSpeed, 25f);
		}
	}

	private void NormalMode()
	{
		lastEnemyMode = false;
		Material[] array = mats;
		foreach (Material obj in array)
		{
			if (currentWave < 20)
			{
				currentGlow = 0.2f;
			}
			else
			{
				currentGlow = 0.5f;
			}
			obj.SetFloat(UKShaderProperties.EmissiveIntensity, currentGlow * glowMultiplier);
			obj.SetFloat(GradientScale, 2f);
			obj.SetFloat(GradientFalloff, 5f);
			obj.SetFloat(GradientSpeed, 10f);
			obj.SetVector(WorldOffset, new Vector4(0f, 0f, 62.5f, 0f));
		}
	}

	public void UpdateGlow()
	{
		Material[] array = mats;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetFloat(UKShaderProperties.EmissiveIntensity, currentGlow * glowMultiplier);
		}
	}

	private void Update()
	{
		Material[] array;
		if (lastEnemyMode)
		{
			if (anw.deadEnemies != enemyAmount - 1)
			{
				NormalMode();
			}
			else if ((bool)enemyToTrack)
			{
				array = mats;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetVector(WorldOffset, new Vector4(enemyToTrack.position.x, enemyToTrack.position.y, enemyToTrack.position.z, 0f));
				}
			}
		}
		else if (!lastEnemyMode && anw.deadEnemies == enemyAmount - 1)
		{
			LastEnemyMode();
		}
		if (anw.deadEnemies >= enemyAmount && !testMode)
		{
			anw.deadEnemies = 0;
			enemyAmount = 999;
			tempEnemyAmount = 0;
			Invoke("NextWave", 1f);
			if (crowdReactions)
			{
				if (crorea == null)
				{
					crorea = MonoSingleton<CrowdReactions>.Instance;
				}
				if (crorea.enabled)
				{
					crorea.React(crorea.cheerLong);
				}
				else
				{
					crowdReactions = false;
				}
			}
		}
		array = mats;
		foreach (Material material in array)
		{
			if (material.GetColor(UKShaderProperties.EmissiveColor) != targetColor)
			{
				material.SetColor(UKShaderProperties.EmissiveColor, Color.Lerp(material.GetColor(UKShaderProperties.EmissiveColor), targetColor, Time.deltaTime));
			}
		}
		if (anw.deadEnemies > tempEnemyAmount)
		{
			anw.deadEnemies = tempEnemyAmount;
		}
		waveNumberText.text = string.Concat(currentWave);
		enemiesLeftText.text = string.Concat(tempEnemyAmount - anw.deadEnemies);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Player"))
		{
			return;
		}
		GetComponent<Collider>().enabled = false;
		if (startWave > 0)
		{
			currentWave = startWave - 1;
			for (int i = 1; i <= currentWave; i++)
			{
				maxPoints += 3 + i / 3;
			}
		}
		waveNumberText.transform.parent.parent.gameObject.SetActive(value: true);
		ShuffleDecks();
		NextWave();
	}

	private void NextWave()
	{
		currentPatternNum++;
		currentWave++;
		maxPoints += 3 + currentWave / 3;
		points = maxPoints;
		if (!nmov)
		{
			nmov = MonoSingleton<NewMovement>.Instance;
		}
		if (nmov.hp > 0)
		{
			nmov.ResetHardDamage();
			nmov.exploded = false;
			nmov.GetHealth(999, silent: true);
			nmov.FullStamina();
		}
		if (!wc)
		{
			wc = MonoSingleton<WeaponCharges>.Instance;
		}
		wc.MaxCharges();
		if ((bool)gz)
		{
			gz.ResetGibs();
		}
		if ((bool)MonoSingleton<ObjectTracker>.Instance && MonoSingleton<ObjectTracker>.Instance.landmineList.Count > 0)
		{
			for (int num = MonoSingleton<ObjectTracker>.Instance.landmineList.Count - 1; num >= 0; num--)
			{
				if (!(MonoSingleton<ObjectTracker>.Instance.landmineList[num] == null))
				{
					UnityEngine.Object.Destroy(MonoSingleton<ObjectTracker>.Instance.landmineList[num].gameObject);
				}
			}
		}
		Projectile[] array = UnityEngine.Object.FindObjectsOfType<Projectile>();
		if (array.Length != 0)
		{
			Projectile[] array2 = array;
			foreach (Projectile projectile in array2)
			{
				if (projectile != null && !projectile.friendly && !projectile.playerBullet)
				{
					UnityEngine.Object.Destroy(projectile.gameObject);
				}
			}
		}
		switch (currentWave)
		{
		case 10:
			targetColor = Color.green;
			break;
		case 15:
			targetColor = Color.yellow;
			break;
		case 20:
		{
			targetColor = Color.red;
			currentGlow = 0.35f;
			Material[] array3 = mats;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].SetFloat(UKShaderProperties.EmissiveIntensity, currentGlow * glowMultiplier);
			}
			break;
		}
		case 25:
		{
			currentGlow = 0.5f;
			Material[] array3 = mats;
			foreach (Material obj in array3)
			{
				obj.SetFloat(PcGamerMode, 1f);
				obj.SetFloat(UKShaderProperties.EmissiveIntensity, currentGlow * glowMultiplier);
			}
			break;
		}
		}
		if (currentPatternNum >= CurrentPatternPool.Length)
		{
			currentPatternNum = 0;
			ShuffleDecks();
		}
		foreach (GameObject spawnedPrefab in spawnedPrefabs)
		{
			spawnedPrefab.GetComponent<EndlessPrefabAnimator>().reverse = true;
		}
		spawnedPrefabs.Clear();
		incompletePrefabs = 0;
		PresenceController.UpdateCyberGrindWave(currentWave);
		if (CurrentPatternPool.Length == 0)
		{
			Invoke("DisplayNoPatternWarning", 2f);
		}
		if (CurrentPatternPool.Length > currentPatternNum)
		{
			LoadPattern(CurrentPatternPool[currentPatternNum]);
		}
	}

	private void DisplayNoPatternWarning()
	{
		MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("NO PATTERNS SELECTED.");
	}

	private void LoadPattern(ArenaPattern pattern)
	{
		if (customPatternMode)
		{
			string text = pattern.name.Split('\\')[^1];
			text = text.Substring(0, text.Length - 4);
			text = text.Replace("CG_", "");
			text = text.Replace("Cg_", "");
			text = text.Replace("cg_", "");
			text = text.Replace('_', ' ');
			text = SplitCamelCase(text);
			text = text.Replace("  ", " ");
			text = text.ToUpper();
			MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage(text, "", "", 0, silent: true);
		}
		string[] array = pattern.heights.Split('\n');
		if (array.Length != 16)
		{
			UnityEngine.Debug.LogError("[Heights] Pattern \"" + pattern.name + "\" has " + array.Length + " rows instead of " + 16);
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int[] array2 = new int[16];
			if (array[i].Length != 16)
			{
				if (array[i].Length < 16)
				{
					UnityEngine.Debug.LogError("[Heights] Pattern \"" + pattern.name + "\" has " + array[i].Length + " elements in row " + i + " instead of " + 16);
					return;
				}
				int num = 0;
				bool flag = false;
				string text2 = "";
				for (int j = 0; j < array[i].Length; j++)
				{
					if (int.TryParse(array[i][j].ToString(), out var result) || array[i][j] == '-')
					{
						if (!flag)
						{
							if (array2.Length <= num)
							{
								throw new Exception("Unable to parse pattern: " + pattern.name + " at row " + i + " and column " + j);
							}
							array2[num] = result;
							num++;
							continue;
						}
						text2 += array[i][j];
					}
					if (array[i][j] == '(')
					{
						if (flag)
						{
							UnityEngine.Debug.LogError("[Heights] Pattern \"" + pattern.name + "\", Error while parsing extended numbers!");
							return;
						}
						flag = true;
					}
					if (array[i][j] == ')')
					{
						if (!flag)
						{
							UnityEngine.Debug.LogError("[Heights] Pattern \"" + pattern.name + "\", Error while parsing extended numbers!");
							return;
						}
						if (array2.Length <= num)
						{
							throw new Exception("Unable to parse pattern: " + pattern.name + " at row " + i + " and column " + j);
						}
						array2[num] = int.Parse(text2);
						flag = false;
						text2 = "";
						num++;
					}
				}
				if (num != 16)
				{
					UnityEngine.Debug.LogError("[Heights] Pattern \"" + pattern.name + "\" has " + array[i].Length + " elements in row " + num + " instead of " + 16);
					return;
				}
			}
			else
			{
				for (int k = 0; k < array[i].Length; k++)
				{
					array2[k] = int.Parse(array[i][k].ToString());
				}
			}
			for (int l = 0; l < array2.Length; l++)
			{
				cubes[i][l].SetTarget((float)array2[l] * offset / 2f);
				cubes[i][l].blockedByPrefab = false;
				incompleteBlocks++;
			}
		}
		currentPattern = pattern;
		MakeGridDynamic();
	}

	public void MakeGridDynamic()
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				EndlessCube endlessCube = cubes[i][j];
				if (!(endlessCube == null))
				{
					endlessCube.MeshRenderer.enabled = true;
				}
			}
		}
		foreach (GameObject spawnedPrefab in spawnedPrefabs)
		{
			if (spawnedPrefab.TryGetComponent<EndlessStairs>(out var component))
			{
				if (component.ActivateFirst)
				{
					component.PrimaryMeshRenderer.enabled = true;
				}
				if (component.ActivateSecond)
				{
					component.SecondaryMeshRenderer.enabled = true;
				}
			}
		}
		combinedGridStaticMeshRenderer.enabled = false;
	}

	private GameObject SpawnOnGrid(GameObject obj, Vector2 position, bool prefab = false, bool enemy = false, CyberPooledType poolType = CyberPooledType.None, bool radiant = false)
	{
		if (Physics.Raycast(base.transform.position + new Vector3(position.x * offset, 200f, position.y * offset), Vector3.down, out var hitInfo, float.PositiveInfinity, prefabSpawnLayerCheck))
		{
			float y = obj.transform.position.y;
			GameObject gameObject = null;
			bool flag = false;
			if (poolType != 0 && poolType == CyberPooledType.JumpPad && jumpPadSelector < jumpPadPool.Count)
			{
				CyberPooledPrefab cyberPooledPrefab = jumpPadPool[jumpPadSelector];
				gameObject = cyberPooledPrefab.gameObject;
				gameObject.transform.position = hitInfo.point + Vector3.up * y;
				cyberPooledPrefab.Animator.Start();
				cyberPooledPrefab.Animator.reverse = false;
				jumpPadSelector++;
				flag = true;
				gameObject.SetActive(value: true);
			}
			if (!flag)
			{
				gameObject = UnityEngine.Object.Instantiate(obj, hitInfo.point + Vector3.up * y, obj.transform.rotation, base.transform);
			}
			if (prefab)
			{
				if (!flag && poolType == CyberPooledType.JumpPad)
				{
					CyberPooledPrefab cyberPooledPrefab2 = gameObject.AddComponent<CyberPooledPrefab>();
					jumpPadPool.Add(cyberPooledPrefab2);
					jumpPadSelector++;
					cyberPooledPrefab2.Index = jumpPadPool.Count - 1;
					cyberPooledPrefab2.Type = CyberPooledType.JumpPad;
					cyberPooledPrefab2.Animator = gameObject.GetComponent<EndlessPrefabAnimator>();
				}
				spawnedPrefabs.Add(gameObject);
				incompletePrefabs++;
			}
			if (enemy)
			{
				if (radiant && (bool)gameObject)
				{
					EnemyIdentifier componentInChildren = gameObject.GetComponentInChildren<EnemyIdentifier>(includeInactive: true);
					if ((bool)componentInChildren)
					{
						componentInChildren.HealthBuff();
						componentInChildren.SpeedBuff();
					}
				}
				spawnedEnemies.Add(gameObject);
			}
			return gameObject;
		}
		return null;
	}

	public GameObject[] GetSpawnedEnemies()
	{
		return spawnedEnemies.ToArray();
	}

	public void OneDone()
	{
		jumpPadSelector = 0;
		incompleteBlocks--;
		if (incompleteBlocks == 0)
		{
			projectilePositions.Clear();
			meleePositions.Clear();
			foreach (GameObject spawnedEnemy in spawnedEnemies)
			{
				if (spawnedEnemy != null)
				{
					spawnedEnemy.transform.SetParent(gz.gibZone, worldPositionStays: true);
					EnemyIdentifier component = spawnedEnemy.GetComponent<EnemyIdentifier>();
					if (component != null && !component.dead)
					{
						UnityEngine.Object.Destroy(spawnedEnemy);
					}
				}
			}
			if ((bool)gz)
			{
				gz.ResetGibs();
			}
			spawnedEnemies.Clear();
			string[] array = currentPattern.prefabs.Split('\n');
			if (array.Length != 16)
			{
				UnityEngine.Debug.LogError("[Prefabs] Pattern \"" + currentPattern.name + "\" has " + array.Length + " rows instead of " + 16);
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Length != 16)
				{
					UnityEngine.Debug.LogError("[Prefabs] Pattern \"" + currentPattern.name + "\" has " + array[i].Length + " elements in row " + i + " instead of " + 16);
					return;
				}
				for (int j = 0; j < array[i].Length; j++)
				{
					if (array[i][j] == '0')
					{
						continue;
					}
					switch (array[i][j])
					{
					case 'n':
						meleePositions.Add(new Vector2(i, j));
						break;
					case 'p':
						projectilePositions.Add(new Vector2(i, j));
						break;
					case 'J':
						cubes[i][j].blockedByPrefab = true;
						SpawnOnGrid(prefabs.jumpPad, new Vector2(i, j), prefab: true, enemy: false, CyberPooledType.JumpPad);
						break;
					case 's':
					{
						cubes[i][j].blockedByPrefab = true;
						if (SpawnOnGrid(prefabs.stairs, new Vector2(i, j), prefab: true).TryGetComponent<EndlessStairs>(out var component2))
						{
							if (component2.PrimaryMeshRenderer != null && component2.ActivateFirst)
							{
								component2.PrimaryMeshRenderer.enabled = true;
							}
							if (component2.SecondaryMeshRenderer != null && component2.ActivateSecond)
							{
								component2.SecondaryMeshRenderer.enabled = true;
							}
						}
						break;
					}
					case 'H':
						if (massAntiBuffer == 0 && currentWave >= (hideousMasses + 1) * 10 && points > 70)
						{
							hideousMasses++;
							SpawnOnGrid(prefabs.hideousMass, new Vector2(i, j), prefab: false, enemy: true);
							points -= 45;
						}
						break;
					}
				}
			}
			if (hideousMasses > 0)
			{
				massAntiBuffer += hideousMasses * 2;
			}
			else if (massAntiBuffer > 0)
			{
				massAntiBuffer--;
			}
			if (spawnedPrefabs.Count == 0)
			{
				GetEnemies();
			}
		}
		TrySetupStaticGridMesh();
	}

	public void OnePrefabDone()
	{
		incompletePrefabs--;
		if (incompletePrefabs == 0)
		{
			GetEnemies();
		}
		TrySetupStaticGridMesh();
	}

	private void GetEnemies()
	{
		nms.BuildNavMesh();
		nvmhlpr.GenerateLinks(cubes);
		for (int i = 0; i < meleePositions.Count; i++)
		{
			Vector2 value = meleePositions[i];
			int index = UnityEngine.Random.Range(i, meleePositions.Count);
			meleePositions[i] = meleePositions[index];
			meleePositions[index] = value;
		}
		for (int j = 0; j < projectilePositions.Count; j++)
		{
			Vector2 value2 = projectilePositions[j];
			int index2 = UnityEngine.Random.Range(j, projectilePositions.Count);
			projectilePositions[j] = projectilePositions[index2];
			projectilePositions[index2] = value2;
		}
		tempEnemyAmount = 0;
		usedMeleePositions = 0;
		usedProjectilePositions = 0;
		spawnedEnemyTypes.Clear();
		tempEnemyAmount += hideousMasses;
		hideousMasses = 0;
		if (currentWave > 11)
		{
			int num = currentWave;
			int num2 = 0;
			while (num >= 10)
			{
				num -= 10;
				num2++;
			}
			if (tempEnemyAmount > 0)
			{
				num2 -= tempEnemyAmount;
			}
			if (uncommonAntiBuffer < 1f && num2 > 0)
			{
				int num3 = UnityEngine.Random.Range(0, currentWave / 10 + 1);
				if (uncommonAntiBuffer <= -0.5f && num3 < 1)
				{
					num3 = 1;
				}
				if (num3 > 0 && meleePositions.Count > 0)
				{
					int num4 = UnityEngine.Random.Range(0, prefabs.uncommonEnemies.Length);
					int num5 = UnityEngine.Random.Range(0, prefabs.uncommonEnemies.Length);
					int num6 = 0;
					while (num4 >= 0 && currentWave < prefabs.uncommonEnemies[num4].spawnWave)
					{
						num4--;
					}
					while (num5 >= 0 && (currentWave < prefabs.uncommonEnemies[num5].spawnWave || num5 == num4))
					{
						if (num5 == 0)
						{
							num6 = -1;
							break;
						}
						num5--;
					}
					if (num4 >= 0)
					{
						if (currentWave > 16)
						{
							if (currentWave < 25)
							{
								num3++;
							}
							else if (num6 != -1)
							{
								num6 = num3;
							}
						}
						bool flag = false;
						bool flag2 = false;
						flag = SpawnUncommons(num4, num3);
						if (num6 > 0)
						{
							flag2 = SpawnUncommons(num5, num6);
						}
						if (flag || flag2)
						{
							if (uncommonAntiBuffer < 0f)
							{
								uncommonAntiBuffer = 0f;
							}
							if (flag)
							{
								uncommonAntiBuffer += ((prefabs.uncommonEnemies[num4].enemyType == EnemyType.Stalker || prefabs.uncommonEnemies[num4].enemyType == EnemyType.Idol) ? 1f : 0.5f);
							}
							if (flag2)
							{
								uncommonAntiBuffer += ((prefabs.uncommonEnemies[num5].enemyType == EnemyType.Stalker || prefabs.uncommonEnemies[num5].enemyType == EnemyType.Idol) ? 1f : 0.5f);
							}
							num2 -= ((!(flag && flag2)) ? 1 : 2);
						}
					}
				}
			}
			else
			{
				uncommonAntiBuffer -= 1f;
			}
			if (currentWave > 15)
			{
				bool flag3 = false;
				if (specialAntiBuffer <= 0 && num2 > 0)
				{
					int num7 = UnityEngine.Random.Range(0, num2 + 1);
					if (specialAntiBuffer <= -2 && num7 < 1)
					{
						num7 = 1;
					}
					if (num7 > 0 && meleePositions.Count > 0)
					{
						for (int k = 0; k < num7; k++)
						{
							int num8 = UnityEngine.Random.Range(0, prefabs.specialEnemies.Length);
							int indexOfEnemyType = GetIndexOfEnemyType(prefabs.specialEnemies[num8].enemyType);
							float num9 = 0f;
							while (num8 >= 0 && usedMeleePositions < meleePositions.Count - 1)
							{
								if (currentWave >= prefabs.specialEnemies[num8].spawnWave && (float)points >= (float)prefabs.specialEnemies[num8].spawnCost + num9)
								{
									bool flag4 = SpawnRadiant(prefabs.specialEnemies[num8], indexOfEnemyType);
									SpawnOnGrid(prefabs.specialEnemies[num8].prefab, meleePositions[usedMeleePositions], prefab: false, enemy: true, CyberPooledType.None, flag4);
									points -= Mathf.RoundToInt((float)(prefabs.specialEnemies[num8].spawnCost * ((!flag4) ? 1 : 3)) + num9);
									num9 += (float)(prefabs.specialEnemies[num8].costIncreasePerSpawn * ((!flag4) ? 1 : 3));
									spawnedEnemyTypes[indexOfEnemyType].amount++;
									usedMeleePositions++;
									tempEnemyAmount++;
									if (specialAntiBuffer < 0)
									{
										specialAntiBuffer = 0;
									}
									specialAntiBuffer++;
									flag3 = true;
									break;
								}
								num8--;
								if (num8 >= 0)
								{
									indexOfEnemyType = GetIndexOfEnemyType(prefabs.specialEnemies[num8].enemyType);
								}
							}
						}
					}
				}
				if (!flag3)
				{
					specialAntiBuffer--;
				}
			}
		}
		GetNextEnemy();
	}

	private int CapUncommonsAmount(int target, int amount)
	{
		switch (prefabs.uncommonEnemies[target].enemyType)
		{
		case EnemyType.Stalker:
			if (amount > currentWave / 8 || amount > 3)
			{
				return Mathf.Min(currentWave / 8, 3);
			}
			break;
		case EnemyType.Idol:
			if (amount > currentWave / 8 || amount > 5)
			{
				return Mathf.Min(currentWave / 8, 5);
			}
			break;
		case EnemyType.Turret:
			if (amount > currentWave / 5 || amount > 6)
			{
				return Mathf.Min(currentWave / 5, 6);
			}
			break;
		case EnemyType.Gutterman:
			if (amount > currentWave / 15 || amount > 5)
			{
				return Mathf.Min(currentWave / 15, 5);
			}
			break;
		case EnemyType.Guttertank:
			if (amount > currentWave / 20 || amount > 3)
			{
				return Mathf.Min(currentWave / 20, 3);
			}
			break;
		case EnemyType.Virtue:
			if (amount > currentWave / 5 || amount > 8)
			{
				return Mathf.Min(currentWave / 5, 8);
			}
			break;
		}
		return amount;
	}

	private int GetIndexOfEnemyType(EnemyType target)
	{
		if (spawnedEnemyTypes.Count > 0)
		{
			for (int i = 0; i < spawnedEnemyTypes.Count; i++)
			{
				if (spawnedEnemyTypes[i].type == target)
				{
					return i;
				}
			}
		}
		spawnedEnemyTypes.Add(new EnemyTypeTracker(target));
		return spawnedEnemyTypes.Count - 1;
	}

	private bool SpawnRadiant(EndlessEnemy target, int indexOf)
	{
		float num = target.spawnWave * 2 + 25;
		float num2 = target.spawnCost;
		if (target.spawnCost < 10)
		{
			num2 += 1f;
		}
		if (target.spawnCost > 10)
		{
			num2 = num2 / 2f + 5f;
		}
		return (float)currentWave >= num + (float)spawnedEnemyTypes[indexOf].amount * num2;
	}

	private bool SpawnUncommons(int target, int amount)
	{
		amount = CapUncommonsAmount(target, amount);
		bool result = false;
		for (int i = 0; i < amount; i++)
		{
			EndlessEnemy endlessEnemy = prefabs.uncommonEnemies[target];
			bool flag = endlessEnemy.enemyType != EnemyType.Stalker && endlessEnemy.enemyType != EnemyType.Guttertank && UnityEngine.Random.Range(0f, 1f) > 0.5f;
			if (flag && usedProjectilePositions >= projectilePositions.Count - 1)
			{
				flag = false;
			}
			if (usedMeleePositions >= meleePositions.Count - 1)
			{
				break;
			}
			int indexOfEnemyType = GetIndexOfEnemyType(endlessEnemy.enemyType);
			int num = endlessEnemy.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType].amount;
			_ = endlessEnemy.spawnCost;
			if (currentWave < endlessEnemy.spawnWave || points / 2 < endlessEnemy.spawnCost + num)
			{
				break;
			}
			bool flag2 = SpawnRadiant(endlessEnemy, indexOfEnemyType);
			SpawnOnGrid(endlessEnemy.prefab, flag ? projectilePositions[usedProjectilePositions] : meleePositions[usedMeleePositions], prefab: false, enemy: true, CyberPooledType.None, flag2);
			points -= endlessEnemy.spawnCost * ((!flag2) ? 1 : 3) + num;
			spawnedEnemyTypes[indexOfEnemyType].amount++;
			if (flag)
			{
				usedProjectilePositions++;
			}
			else
			{
				usedMeleePositions++;
			}
			tempEnemyAmount++;
			result = true;
			if (flag2)
			{
				amount -= 2;
			}
		}
		return result;
	}

	private void GetNextEnemy()
	{
		if (!base.gameObject.scene.isLoaded)
		{
			return;
		}
		if ((points > 0 && usedMeleePositions < meleePositions.Count) || (points > 1 && usedProjectilePositions < projectilePositions.Count))
		{
			if ((UnityEngine.Random.Range(0f, 1f) < 0.5f || usedProjectilePositions >= projectilePositions.Count) && usedMeleePositions < meleePositions.Count)
			{
				int num = UnityEngine.Random.Range(0, prefabs.meleeEnemies.Length);
				bool flag = false;
				for (int num2 = num; num2 >= 0; num2--)
				{
					EndlessEnemy endlessEnemy = prefabs.meleeEnemies[num2];
					int indexOfEnemyType = GetIndexOfEnemyType(endlessEnemy.enemyType);
					int num3 = endlessEnemy.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType].amount;
					int num4 = endlessEnemy.spawnCost + num3;
					if (((float)points >= (float)num4 * 1.5f || (num2 == 0 && points >= num4)) && currentWave >= endlessEnemy.spawnWave)
					{
						bool flag2 = SpawnRadiant(endlessEnemy, indexOfEnemyType);
						flag = true;
						SpawnOnGrid(endlessEnemy.prefab, meleePositions[usedMeleePositions], prefab: false, enemy: true, CyberPooledType.None, flag2);
						points -= endlessEnemy.spawnCost * ((!flag2) ? 1 : 3) + num3;
						spawnedEnemyTypes[indexOfEnemyType].amount++;
						usedMeleePositions++;
						tempEnemyAmount++;
						break;
					}
				}
				if (!flag)
				{
					usedMeleePositions = meleePositions.Count;
				}
			}
			else if (usedProjectilePositions < projectilePositions.Count)
			{
				int num5 = UnityEngine.Random.Range(0, prefabs.projectileEnemies.Length);
				bool flag3 = false;
				for (int num6 = num5; num6 >= 0; num6--)
				{
					EndlessEnemy endlessEnemy2 = prefabs.projectileEnemies[num6];
					int indexOfEnemyType2 = GetIndexOfEnemyType(endlessEnemy2.enemyType);
					int num7 = endlessEnemy2.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType2].amount;
					int num8 = endlessEnemy2.spawnCost + num7;
					if (((float)points >= (float)num8 * 1.5f || (num6 == 0 && points >= num8)) && currentWave >= endlessEnemy2.spawnWave)
					{
						bool flag4 = SpawnRadiant(endlessEnemy2, indexOfEnemyType2);
						flag3 = true;
						SpawnOnGrid(endlessEnemy2.prefab, projectilePositions[usedProjectilePositions], prefab: false, enemy: true, CyberPooledType.None, flag4);
						points -= endlessEnemy2.spawnCost * ((!flag4) ? 1 : 3) + num7;
						spawnedEnemyTypes[indexOfEnemyType2].amount++;
						usedProjectilePositions++;
						tempEnemyAmount++;
						break;
					}
				}
				if (!flag3)
				{
					usedProjectilePositions = projectilePositions.Count;
				}
			}
			Invoke("GetNextEnemy", 0.1f);
		}
		else
		{
			enemyAmount = tempEnemyAmount;
		}
	}

	private void ShuffleDecks()
	{
		int num = Mathf.FloorToInt(CurrentPatternPool.Length / 2);
		for (int i = 0; i < num; i++)
		{
			ArenaPattern arenaPattern = CurrentPatternPool[i];
			int num2 = UnityEngine.Random.Range(i, num);
			CurrentPatternPool[i] = CurrentPatternPool[num2];
			CurrentPatternPool[num2] = arenaPattern;
		}
		for (int j = num; j < CurrentPatternPool.Length; j++)
		{
			ArenaPattern arenaPattern2 = CurrentPatternPool[j];
			int num3 = UnityEngine.Random.Range(j, CurrentPatternPool.Length);
			CurrentPatternPool[j] = CurrentPatternPool[num3];
			CurrentPatternPool[num3] = arenaPattern2;
		}
	}

	private string SplitCamelCase(string str)
	{
		return Regex.Replace(Regex.Replace(str, "(\\P{Ll})(\\P{Ll}\\p{Ll})", "$1 $2"), "(\\p{Ll})(\\P{Ll})", "$1 $2");
	}
}

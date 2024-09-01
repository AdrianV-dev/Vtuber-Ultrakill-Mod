using System.Collections.Generic;
using System.Linq;
using plog;
using UnityEngine;

public class StainVoxelManager : MonoSingleton<StainVoxelManager>
{
	private static readonly plog.Logger Log = new plog.Logger("StainVoxelManager");

	private const int FireSpreadDistance = 5;

	public const float VoxelSize = 2.75f;

	public const float VoxelOverlapSphereRadius = 1.375f;

	public const int ExplosionMargin = 2;

	private readonly Dictionary<Vector3Int, StainVoxel> stainVoxels = new Dictionary<Vector3Int, StainVoxel>();

	private readonly HashSet<Vector3Int> pendingIgnitions = new HashSet<Vector3Int>();

	private TimeSince? lastPropagationTick;

	private readonly HashSet<Vector3Int> explodedVoxels = new HashSet<Vector3Int>();

	public readonly HurtCooldownCollection SharedHurtCooldownCollection = new HurtCooldownCollection();

	public void AcknowledgeNewStain(StainVoxel voxel)
	{
		voxel.AcknowledgeNewStain();
		if (voxel.isBurning)
		{
			return;
		}
		Vector3Int[] array = GetShapeIterator(voxel.VoxelPosition, VoxelCheckingShape.Box, 3).ToArray();
		foreach (Vector3Int key in array)
		{
			if (stainVoxels.TryGetValue(key, out var value) && value.isBurning)
			{
				voxel.TryIgnite();
			}
		}
	}

	private void Update()
	{
		if (!lastPropagationTick.HasValue || (float?)lastPropagationTick < 0.05f || pendingIgnitions == null || pendingIgnitions.Count == 0)
		{
			return;
		}
		lastPropagationTick = 0f - Random.Range(0f, 0.005f);
		List<StainVoxel> list = new List<StainVoxel>();
		foreach (Vector3Int pendingIgnition in pendingIgnitions)
		{
			StainVoxel stainVoxel = CreateOrGetVoxel(VoxelToWorldPosition(pendingIgnition), dontCreate: true);
			if (stainVoxel != null && !stainVoxel.isBurning)
			{
				list.Add(stainVoxel);
			}
		}
		pendingIgnitions.Clear();
		bool flag = false;
		foreach (StainVoxel item in list)
		{
			if (item.TryIgnite())
			{
				flag = true;
				ScheduleFirePropagation(item);
			}
		}
		if (!flag)
		{
			explodedVoxels.Clear();
			lastPropagationTick = null;
		}
	}

	public StainVoxel CreateOrGetVoxel(Vector3 worldPosition, bool dontCreate = false)
	{
		Vector3Int vector3Int = WorldToVoxelPosition(worldPosition);
		if (stainVoxels.TryGetValue(vector3Int, out var value))
		{
			return value;
		}
		if (dontCreate)
		{
			return null;
		}
		value = new StainVoxel(vector3Int);
		stainVoxels.Add(vector3Int, value);
		return value;
	}

	public void RefreshVoxel(StainVoxel voxel)
	{
		if (voxel.isEmpty)
		{
			stainVoxels.Remove(voxel.VoxelPosition);
			voxel.DestroySelf();
		}
	}

	public void UpdateProxyPosition(VoxelProxy proxy, Vector3Int newPosition)
	{
		Vector3Int voxelPosition = proxy.voxel.VoxelPosition;
		if (!(voxelPosition == newPosition))
		{
			if (!stainVoxels.TryGetValue(voxelPosition, out var value))
			{
				Log.Warning($"Failed to find voxel at {voxelPosition}");
				return;
			}
			value.RemoveProxy(proxy, destroy: false);
			CreateOrGetVoxel(VoxelToWorldPosition(newPosition)).AddProxy(proxy);
		}
	}

	public bool ShouldExplodeAt(Vector3Int voxelPosition)
	{
		if (explodedVoxels.Count == 0)
		{
			explodedVoxels.Add(voxelPosition);
			return true;
		}
		foreach (Vector3Int explodedVoxel in explodedVoxels)
		{
			if (Mathf.Abs(voxelPosition.x - explodedVoxel.x) <= 2 && Mathf.Abs(voxelPosition.y - explodedVoxel.y) <= 2 && Mathf.Abs(voxelPosition.z - explodedVoxel.z) <= 2)
			{
				return false;
			}
		}
		explodedVoxels.Add(voxelPosition);
		return true;
	}

	public bool TryIgniteAt(Vector3 worldPosition, int checkSize = 3)
	{
		if (stainVoxels.Count == 0)
		{
			return false;
		}
		Vector3Int voxelPosition = WorldToVoxelPosition(worldPosition);
		return TryIgniteAt(voxelPosition, checkSize);
	}

	public bool TryIgniteAt(Vector3Int voxelPosition, int checkSize = 3)
	{
		Log.Info($"TryIgniteAt {voxelPosition}");
		if (stainVoxels.Count == 0)
		{
			return false;
		}
		if (!TryGetVoxels(voxelPosition, out var voxels, checkSize))
		{
			return false;
		}
		bool result = false;
		foreach (StainVoxel item in voxels)
		{
			if (item.TryIgnite())
			{
				result = true;
			}
		}
		return result;
	}

	public void ScheduleFirePropagation(StainVoxel voxel)
	{
		if (voxel == null)
		{
			Log.Warning("ScheduleFirePropagation called with null voxel");
			return;
		}
		if (!lastPropagationTick.HasValue)
		{
			lastPropagationTick = 0f;
		}
		Vector3Int voxelPosition = voxel.VoxelPosition;
		Vector3Int[] array = IterateBox(voxelPosition, 5).ToArray();
		foreach (Vector3Int vector3Int in array)
		{
			if (!(vector3Int == voxelPosition) && !pendingIgnitions.Contains(vector3Int) && stainVoxels.TryGetValue(vector3Int, out var value) && !value.isBurning)
			{
				pendingIgnitions.Add(vector3Int);
			}
		}
		if (explodedVoxels.Count != 0 && pendingIgnitions.Count == 0)
		{
			explodedVoxels.Clear();
		}
	}

	public void DoneBurning(VoxelProxy proxy)
	{
		if (!(proxy == null))
		{
			proxy.voxel?.RemoveProxy(proxy);
		}
	}

	public bool TryGetVoxelsWorld(Vector3 worldPosition, out List<StainVoxel> voxels, int checkSize = 3, VoxelCheckingShape shape = VoxelCheckingShape.Box, bool returnOnHit = false)
	{
		Vector3Int voxelPosition = WorldToVoxelPosition(worldPosition);
		return TryGetVoxels(voxelPosition, out voxels, checkSize, shape, returnOnHit);
	}

	public bool TryGetVoxels(Vector3Int voxelPosition, out List<StainVoxel> voxels, int checkSize = 3, VoxelCheckingShape shape = VoxelCheckingShape.Box, bool returnOnHit = false)
	{
		voxels = new List<StainVoxel>();
		if (checkSize <= 1)
		{
			if (stainVoxels.TryGetValue(voxelPosition, out var value))
			{
				voxels.Add(value);
				DrawVoxel(voxelPosition, success: true);
				return true;
			}
			DrawVoxel(voxelPosition, success: false);
			return false;
		}
		foreach (Vector3Int item in GetShapeIterator(voxelPosition, shape, checkSize))
		{
			if (stainVoxels.TryGetValue(item, out var value2))
			{
				voxels.Add(value2);
				DrawVoxel(item, success: true);
				if (returnOnHit)
				{
					return true;
				}
			}
			else
			{
				DrawVoxel(item, success: false);
			}
		}
		return voxels.Count > 0;
	}

	public bool HasProxiesAt(Vector3Int voxelPosition, int checkSize = 3, VoxelCheckingShape shape = VoxelCheckingShape.Box, ProxySearchMode searchMode = ProxySearchMode.Any, bool returnOnHit = true)
	{
		if (stainVoxels.Count == 0)
		{
			return false;
		}
		if (checkSize <= 1)
		{
			return ProxyExistsAt(voxelPosition, searchMode);
		}
		foreach (Vector3Int item in GetShapeIterator(voxelPosition, shape, checkSize))
		{
			if (ProxyExistsAt(item, searchMode))
			{
				DrawVoxel(item, success: true);
				if (returnOnHit)
				{
					return true;
				}
			}
			else
			{
				DrawVoxel(item, success: false);
			}
		}
		return false;
	}

	private IEnumerable<Vector3Int> GetShapeIterator(Vector3Int center, VoxelCheckingShape shape, int size)
	{
		return shape switch
		{
			VoxelCheckingShape.Box => IterateBox(center, size), 
			VoxelCheckingShape.VerticalBox => IterateVerticalBox(center, size, 2), 
			VoxelCheckingShape.Cross => IterateCross(center, size, 2), 
			VoxelCheckingShape.Pole => IteratePole(center, size), 
			_ => null, 
		};
	}

	private bool ProxyExistsAt(Vector3Int voxelPosition, ProxySearchMode searchMode = ProxySearchMode.Any)
	{
		if (stainVoxels.Count == 0)
		{
			return false;
		}
		if (!stainVoxels.ContainsKey(voxelPosition))
		{
			return false;
		}
		if (!stainVoxels.TryGetValue(voxelPosition, out var value))
		{
			return false;
		}
		return value.HasStains(searchMode);
	}

	private static IEnumerable<Vector3Int> IterateBox(Vector3Int center, int size)
	{
		int halfSize = size / 2;
		for (int x = -halfSize; x <= halfSize; x++)
		{
			for (int y = -halfSize; y <= halfSize; y++)
			{
				for (int z = -halfSize; z <= halfSize; z++)
				{
					yield return new Vector3Int(center.x + x, center.y + y, center.z + z);
				}
			}
		}
	}

	private static IEnumerable<Vector3Int> IterateVerticalBox(Vector3Int center, int size, int height)
	{
		int halfSize = size / 2;
		for (int x = -halfSize; x <= halfSize; x++)
		{
			for (int z = -halfSize; z <= halfSize; z++)
			{
				for (int y = 0; y < height; y++)
				{
					yield return new Vector3Int(center.x + x, center.y + y, center.z + z);
				}
			}
		}
	}

	private static IEnumerable<Vector3Int> IterateCross(Vector3Int center, int size, int height)
	{
		int halfSize = size / 2;
		for (int y = 0; y < height; y++)
		{
			for (int x = -halfSize; x <= halfSize; x++)
			{
				yield return new Vector3Int(center.x + x, center.y + y, center.z);
			}
			for (int x = -halfSize; x <= halfSize; x++)
			{
				if (x != 0)
				{
					yield return new Vector3Int(center.x, center.y + y, center.z + x);
				}
			}
		}
	}

	private static IEnumerable<Vector3Int> IteratePole(Vector3Int center, int size)
	{
		int halfSize = size / 2;
		for (int i = -halfSize; i <= halfSize; i++)
		{
			yield return new Vector3Int(center.x, center.y + i, center.z);
		}
	}

	public static Vector3Int WorldToVoxelPosition(Vector3 position)
	{
		int x = Mathf.RoundToInt(position.x / 2.75f);
		int y = Mathf.RoundToInt(position.y / 2.75f);
		int z = Mathf.RoundToInt(position.z / 2.75f);
		return new Vector3Int(x, y, z);
	}

	public static Vector3 VoxelToWorldPosition(Vector3Int position)
	{
		return new Vector3((float)position.x * 2.75f, (float)position.y * 2.75f, (float)position.z * 2.75f);
	}

	private static void DrawVoxel(Vector3Int voxelPosition, bool success)
	{
	}

	private static void DrawVoxel(Vector3 roundedWorldPosition, bool success)
	{
	}
}

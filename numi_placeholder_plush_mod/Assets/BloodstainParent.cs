using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BloodstainParent : MonoBehaviour
{
	public int parentIndex;

	public float4x4 matrixAtStep;

	private HashSet<int> children = new HashSet<int>();

	public void OnStep()
	{
		matrixAtStep = GetMatrix();
	}

	public float4x4 GetMatrix()
	{
		return float4x4.TRS(base.transform.position, base.transform.rotation, new float3(1));
	}

	private void Start()
	{
		parentIndex = MonoSingleton<BloodsplatterManager>.Instance.CreateParent(GetMatrix());
		OnStep();
		MonoSingleton<BloodsplatterManager>.Instance.reuseStainIndex += OnStainIndexReuse;
		MonoSingleton<BloodsplatterManager>.Instance.reuseParentIndex += OnParentIndexReuse;
		MonoSingleton<BloodsplatterManager>.Instance.StainsCleared += OnStainsCleared;
		MonoSingleton<BloodsplatterManager>.Instance.PostCollisionStep += OnStep;
	}

	public void OnStainsCleared()
	{
		parentIndex = -1;
		children.Clear();
	}

	public void CreateChild(float3 pos, float3 norm, bool fromStep = false)
	{
		if (parentIndex == -1)
		{
			parentIndex = MonoSingleton<BloodsplatterManager>.Instance.CreateParent(GetMatrix());
		}
		float4x4 a = math.inverse(fromStep ? matrixAtStep : GetMatrix());
		float3 pos2 = math.transform(a, pos);
		float3 norm2 = math.rotate(a, norm);
		int item = MonoSingleton<BloodsplatterManager>.Instance.CreateBloodstain(pos2, norm2, parentIndex);
		children.Add(item);
	}

	private void OnStainIndexReuse(int index)
	{
		children.Remove(index);
	}

	private void OnParentIndexReuse(int index)
	{
		if (index == parentIndex)
		{
			parentIndex = -1;
			ClearChildren();
		}
	}

	private void Update()
	{
		if (parentIndex != -1 && base.transform.hasChanged)
		{
			MonoSingleton<BloodsplatterManager>.Instance.parents[parentIndex] = GetMatrix();
			base.transform.hasChanged = false;
		}
	}

	private void OnDestroy()
	{
		if ((bool)MonoSingleton<BloodsplatterManager>.Instance)
		{
			MonoSingleton<BloodsplatterManager>.Instance.reuseStainIndex -= OnStainIndexReuse;
			MonoSingleton<BloodsplatterManager>.Instance.reuseParentIndex -= OnParentIndexReuse;
			MonoSingleton<BloodsplatterManager>.Instance.PostCollisionStep -= OnStep;
			MonoSingleton<BloodsplatterManager>.Instance.StainsCleared -= OnStainsCleared;
		}
	}

	private void OnDisable()
	{
		ClearChildren();
	}

	public void ClearChildren()
	{
		if (MonoSingleton<BloodsplatterManager>.Instance == null)
		{
			children.Clear();
			return;
		}
		foreach (int item in new HashSet<int>(children))
		{
			MonoSingleton<BloodsplatterManager>.Instance.DeleteBloodstain(item);
		}
		children.Clear();
	}
}

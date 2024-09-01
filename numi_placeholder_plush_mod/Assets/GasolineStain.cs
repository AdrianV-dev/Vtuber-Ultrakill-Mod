using UnityEngine;

public class GasolineStain : MonoBehaviour
{
	private Vector3 initialSize;

	public Transform Parent { get; private set; }

	public bool IsStatic { get; private set; } = true;


	public bool IsFloor { get; private set; }

	private void Awake()
	{
		MeshRenderer component = GetComponent<MeshRenderer>();
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetFloat("_IsOil", 1f);
		materialPropertyBlock.SetFloat("_Index", Random.Range(0, 5));
		component.SetPropertyBlock(materialPropertyBlock);
		initialSize = base.transform.localScale;
	}

	private void Start()
	{
		IsFloor = CalculateDot() > 0.25f;
		initialSize = base.transform.localScale;
	}

	private float CalculateDot()
	{
		return Vector3.Dot(-base.transform.forward, Vector3.up);
	}

	public void AttachTo(Collider other)
	{
		Transform transform = other.transform;
		GameObject gameObject = other.gameObject;
		base.transform.SetParent(transform, worldPositionStays: true);
		Parent = transform;
		if (gameObject.CompareTag("Moving") || ((bool)MonoSingleton<ComponentsDatabase>.Instance && MonoSingleton<ComponentsDatabase>.Instance.scrollers.Contains(transform)) || (gameObject.TryGetComponent<Rigidbody>(out var component) && !component.isKinematic))
		{
			IsStatic = false;
		}
		else
		{
			IsStatic = true;
		}
		Vector3 forward = base.transform.forward;
		Vector3 worldPosition = base.transform.position + forward * -0.5f;
		StainVoxel stainVoxel = MonoSingleton<StainVoxelManager>.Instance.CreateOrGetVoxel(worldPosition);
		VoxelProxy voxelProxy = stainVoxel.CreateOrGetProxyFor(this);
		MonoSingleton<StainVoxelManager>.Instance.AcknowledgeNewStain(stainVoxel);
		if (!IsStatic && (bool)MonoSingleton<ComponentsDatabase>.Instance && MonoSingleton<ComponentsDatabase>.Instance.scrollers.Contains(transform) && transform.TryGetComponent<ScrollingTexture>(out var component2) && !component2.attachedObjects.Contains(voxelProxy.transform))
		{
			component2.attachedObjects.Add(voxelProxy.transform);
		}
	}

	public void OnTransformParentChanged()
	{
		initialSize = base.transform.localScale;
	}

	public void SetSize(float size)
	{
		base.transform.localScale = initialSize * size;
	}
}

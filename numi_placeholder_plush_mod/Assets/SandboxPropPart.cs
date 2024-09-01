using Sandbox;
using UnityEngine;

public class SandboxPropPart : MonoBehaviour
{
	public SandboxSpawnableInstance parent;

	private void Awake()
	{
		if (parent == null)
		{
			parent = GetComponentInParent<SandboxSpawnableInstance>();
		}
	}
}

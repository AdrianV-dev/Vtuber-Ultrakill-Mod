using UnityEngine;

public class MapInfoBase : MonoBehaviour
{
	public static MapInfoBase InstanceAnyType;

	public string layerName = "LAYER /// NUMBER";

	public string levelName = "LEVEL NAME";

	public bool sandboxTools;

	public bool hideStockHUD;

	public bool forceUpdateEnemyRenderers;

	public bool continuousGibCollisions;

	public bool removeGibsWithoutAbsorbers;

	public float gibRemoveTime = 5f;
}

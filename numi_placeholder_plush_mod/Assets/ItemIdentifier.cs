using UnityEngine;

public class ItemIdentifier : MonoBehaviour
{
	public bool infiniteSource;

	public bool pickedUp;

	[HideInInspector]
	public bool beenPickedUp;

	public bool reverseTransformSettings;

	public Vector3 putDownPosition;

	public Vector3 putDownRotation;

	public Vector3 putDownScale = Vector3.one;

	public GameObject pickUpSound;

	public ItemType itemType;

	public bool noHoldingAnimation;

	[HideInInspector]
	public bool hooked;

	[HideInInspector]
	public ItemPlaceZone ipz;

	public UltrakillEvent onPickUp;

	public UltrakillEvent onPutDown;

	public ItemIdentifier CreateCopy()
	{
		if (this == null)
		{
			return null;
		}
		ItemIdentifier itemIdentifier = Object.Instantiate(this);
		itemIdentifier.infiniteSource = false;
		itemIdentifier.pickedUp = false;
		itemIdentifier.beenPickedUp = false;
		itemIdentifier.hooked = false;
		return itemIdentifier;
	}

	private void PickUp()
	{
		onPickUp?.Invoke();
	}

	private void PutDown()
	{
		onPutDown?.Invoke();
	}

	public void ForcePutDown(ItemPlaceZone target)
	{
		if (MonoSingleton<FistControl>.Instance.currentPunch.heldItem == this)
		{
			MonoSingleton<PlayerUtilities>.Instance.PlaceHeldObject(target);
		}
	}
}

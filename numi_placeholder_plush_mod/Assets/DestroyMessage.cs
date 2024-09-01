using UnityEngine;

[DisallowMultipleComponent]
public sealed class DestroyMessage : MessageDispatcher
{
	private void OnDestroy()
	{
		base.Handler.Invoke();
	}
}

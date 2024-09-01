using UnityEngine;

[DisallowMultipleComponent]
public sealed class DisableMessage : MessageDispatcher
{
	private void OnDisable()
	{
		base.Handler.Invoke();
	}
}

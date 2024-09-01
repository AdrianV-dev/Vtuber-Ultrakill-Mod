using System.Linq;
using ULTRAKILL.Cheats;
using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
	public bool oneTime;

	public bool disableOnExit;

	public bool dontActivateOnEnable;

	public bool reactivateOnEnable;

	public bool forEnemies;

	public bool notIfEnemiesDisabled;

	public bool onlyIfPlayerIsAlive;

	public bool dontUseEventsIfEnemiesDisabled;

	[HideInInspector]
	public bool activated;

	[HideInInspector]
	public bool activating;

	public float delay;

	private bool nonCollider;

	private int playerIn;

	[Space(20f)]
	public Collider[] ignoreColliders;

	[Space(20f)]
	public ObjectActivationCheck obac;

	public bool onlyCheckObacOnce;

	public bool disableIfObacOff;

	[Space(10f)]
	public UltrakillEvent events;

	private bool canUseEvents
	{
		get
		{
			if (DisableEnemySpawns.DisableArenaTriggers)
			{
				return !dontUseEventsIfEnemiesDisabled;
			}
			return true;
		}
	}

	private void Start()
	{
		if (!dontActivateOnEnable && GetComponent<Collider>() == null && GetComponent<Rigidbody>() == null)
		{
			nonCollider = true;
			if ((!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead) && (!oneTime || (!activating && !activated)))
			{
				Invoke("Activate", delay);
			}
		}
	}

	private void Update()
	{
		if ((nonCollider || playerIn > 0) && !activating && !activated && (bool)obac && obac.readyToActivate && !onlyCheckObacOnce && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			activating = true;
			Invoke("Activate", delay);
		}
		if (disableIfObacOff && activated && (bool)obac && !obac.readyToActivate)
		{
			Deactivate();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (ignoreColliders != null && ignoreColliders.Contains(other))
		{
			return;
		}
		if ((forEnemies && other.gameObject.CompareTag("Enemy")) || (!forEnemies && other.gameObject.CompareTag("Player")))
		{
			playerIn++;
		}
		if (((!forEnemies && (!oneTime || (!activating && !activated)) && other.gameObject.CompareTag("Player")) || (forEnemies && !activating && !activated && other.gameObject.CompareTag("Enemy"))) && playerIn == 1 && (!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			if (oneTime)
			{
				activating = true;
			}
			Invoke("Activate", delay);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (ignoreColliders == null || !ignoreColliders.Contains(other))
		{
			if ((forEnemies && other.gameObject.CompareTag("Enemy")) || (!forEnemies && other.gameObject.CompareTag("Player")))
			{
				playerIn--;
			}
			if (disableOnExit && ((!forEnemies && (activating || activated) && other.gameObject.CompareTag("Player") && playerIn == 0) || (forEnemies && (activating || activated) && other.gameObject.CompareTag("Enemy"))) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
			{
				Deactivate();
			}
		}
	}

	public void ActivateDelayed(float delay)
	{
		Invoke("Activate", delay);
	}

	public void Activate()
	{
		if (base.gameObject.activeSelf && (!activated || !oneTime) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead) && (!notIfEnemiesDisabled || !DisableEnemySpawns.DisableArenaTriggers) && (!obac || obac.readyToActivate))
		{
			activating = false;
			activated = true;
			if (canUseEvents)
			{
				events.Invoke();
			}
		}
	}

	public void Deactivate()
	{
		if (!oneTime)
		{
			activated = false;
			activating = false;
		}
		if (canUseEvents)
		{
			events.Revert();
		}
		CancelInvoke("Activate");
	}

	private void OnDisable()
	{
		if (base.gameObject.scene.isLoaded)
		{
			if (activated && nonCollider && disableOnExit && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
			{
				Deactivate();
			}
			activating = false;
			playerIn = 0;
			CancelInvoke("Activate");
		}
	}

	private void OnEnable()
	{
		if ((!activated || reactivateOnEnable) && nonCollider && (!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			activating = true;
			Invoke("Activate", delay);
		}
	}
}

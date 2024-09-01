using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HudMessage : MonoBehaviour
{
	private HudMessageReceiver messageHud;

	public InputActionReference actionReference;

	public bool timed;

	public bool deactivating;

	public bool notOneTime;

	public bool dontActivateOnTriggerEnter;

	public bool silent;

	public bool deactiveOnTriggerExit;

	public bool deactiveOnDisable;

	private bool activated;

	public string message;

	public string message2;

	private Image img;

	private TMP_Text text;

	public string playerPref;

	private bool colliderless;

	public float timerTime = 5f;

	private string PlayerPref
	{
		get
		{
			string text = playerPref;
			if (!(text == "SecMisTut"))
			{
				if (text == "ShoUseTut")
				{
					return "hideShotgunPopup";
				}
				return playerPref;
			}
			return "secretMissionPopup";
		}
	}

	private void Start()
	{
		if (GetComponent<Collider>() == null)
		{
			colliderless = true;
			if (PlayerPref == "" || playerPref == null)
			{
				PlayMessage();
			}
			else if (!MonoSingleton<PrefsManager>.Instance.GetBool(PlayerPref))
			{
				MonoSingleton<PrefsManager>.Instance.SetBool(PlayerPref, content: true);
				PlayMessage();
			}
		}
	}

	private void OnEnable()
	{
		if (colliderless && (!activated || notOneTime))
		{
			if (PlayerPref == "")
			{
				PlayMessage();
			}
			else if (!MonoSingleton<PrefsManager>.Instance.GetBool(PlayerPref))
			{
				MonoSingleton<PrefsManager>.Instance.SetBool(PlayerPref, content: true);
				PlayMessage();
			}
		}
	}

	private void OnDisable()
	{
		if (base.gameObject.scene.isLoaded && deactiveOnDisable && activated)
		{
			Done();
		}
	}

	private void Update()
	{
		if (activated && timed)
		{
			img.enabled = true;
			text.enabled = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!dontActivateOnTriggerEnter && other.gameObject.CompareTag("Player") && (!activated || notOneTime))
		{
			if (PlayerPref == "")
			{
				PlayMessage();
			}
			else if (!MonoSingleton<PrefsManager>.Instance.GetBool(PlayerPref))
			{
				MonoSingleton<PrefsManager>.Instance.SetBool(PlayerPref, content: true);
				PlayMessage();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!dontActivateOnTriggerEnter && other.gameObject.CompareTag("Player") && activated && deactiveOnTriggerExit)
		{
			Done();
		}
	}

	private void Done()
	{
		img.enabled = false;
		text.enabled = false;
		activated = false;
		Begone();
	}

	private void Begone()
	{
		if (!notOneTime)
		{
			Object.Destroy(this);
		}
	}

	public void PlayMessage(bool hasToBeEnabled = false)
	{
		if ((!activated || notOneTime) && (!hasToBeEnabled || (base.gameObject.activeInHierarchy && base.enabled)))
		{
			activated = true;
			messageHud = MonoSingleton<HudMessageReceiver>.Instance;
			this.text = messageHud.text;
			if (actionReference == null)
			{
				this.text.text = message;
			}
			else
			{
				string text = "";
				text = MonoSingleton<InputManager>.Instance.GetBindingString(actionReference.action.id);
				this.text.text = message + text + message2;
			}
			this.text.text = this.text.text.Replace('$', '\n');
			this.text.enabled = true;
			if (!img)
			{
				img = messageHud.GetComponent<Image>();
			}
			img.enabled = true;
			if (deactivating)
			{
				Done();
			}
			else if (!silent)
			{
				messageHud.GetComponent<AudioSource>().Play();
			}
			if (timed && notOneTime)
			{
				CancelInvoke("Done");
				Invoke("Done", timerTime);
			}
			else if (timed)
			{
				Invoke("Done", timerTime);
			}
			else if (!deactiveOnTriggerExit && !deactiveOnDisable)
			{
				Invoke("Begone", 1f);
			}
			messageHud.GetComponent<HudOpenEffect>().Force();
		}
	}

	public void ChangeMessage(string newMessage)
	{
		message = newMessage;
		actionReference = null;
		message2 = "";
	}
}

using UnityEngine;

public class Crossfade : MonoBehaviour
{
	public bool multipleTargets;

	public AudioSource from;

	public AudioSource to;

	public AudioSource[] froms;

	public AudioSource[] tos;

	[HideInInspector]
	public float[] fromMaxVolumes;

	[HideInInspector]
	public float[] toOriginalVolumes;

	[HideInInspector]
	public float[] toMaxVolumes;

	[HideInInspector]
	public float[] toMinVolumes;

	[HideInInspector]
	public bool inProgress;

	public float time;

	private float fadeAmount;

	public bool match;

	public bool dontActivateOnStart;

	public bool oneTime;

	private bool activated;

	private bool firstTime = true;

	private void Awake()
	{
		if (!multipleTargets)
		{
			if ((bool)from)
			{
				froms = new AudioSource[1];
				froms[0] = from;
			}
			else
			{
				froms = new AudioSource[0];
			}
			if ((bool)to)
			{
				tos = new AudioSource[1];
				tos[0] = to;
			}
			else
			{
				tos = new AudioSource[0];
			}
		}
		if (fromMaxVolumes == null || fromMaxVolumes.Length == 0)
		{
			fromMaxVolumes = new float[froms.Length];
		}
		if (toOriginalVolumes == null || toOriginalVolumes.Length == 0)
		{
			toOriginalVolumes = new float[tos.Length];
		}
		if (toMaxVolumes == null || toMaxVolumes.Length == 0)
		{
			toMaxVolumes = new float[tos.Length];
		}
		if (toMinVolumes == null || toMinVolumes.Length == 0)
		{
			toMinVolumes = new float[tos.Length];
		}
		if (tos.Length != 0)
		{
			for (int i = 0; i < tos.Length; i++)
			{
				toOriginalVolumes[i] = tos[i].volume;
			}
		}
	}

	private void Start()
	{
		if (!dontActivateOnStart && !inProgress)
		{
			StartFade();
		}
	}

	private void OnEnable()
	{
		if (!dontActivateOnStart && !inProgress)
		{
			StartFade();
		}
	}

	private void Update()
	{
		if (!inProgress)
		{
			return;
		}
		fadeAmount = Mathf.MoveTowards(fadeAmount, 1f, Time.deltaTime / time);
		if (froms.Length != 0)
		{
			for (int i = 0; i < froms.Length; i++)
			{
				if (!(froms[i] == null))
				{
					froms[i].volume = Mathf.Lerp(fromMaxVolumes[i], 0f, fadeAmount);
				}
			}
		}
		if (tos.Length != 0)
		{
			for (int j = 0; j < tos.Length; j++)
			{
				if (!(tos[j] == null))
				{
					tos[j].volume = Mathf.Lerp(toMinVolumes[j], toMaxVolumes[j], fadeAmount);
				}
			}
		}
		if (fadeAmount == 1f)
		{
			StopFade();
		}
	}

	public void StartFade()
	{
		if (!activated)
		{
			activated = true;
		}
		else if (oneTime)
		{
			return;
		}
		if (froms.Length != 0)
		{
			for (int i = 0; i < froms.Length; i++)
			{
				if (froms[i] == null)
				{
					continue;
				}
				if (MonoSingleton<CrossfadeTracker>.Instance.actives.Count > 0)
				{
					for (int num = MonoSingleton<CrossfadeTracker>.Instance.actives.Count - 1; num >= 0; num--)
					{
						if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num] == null))
						{
							if (MonoSingleton<CrossfadeTracker>.Instance.actives[num].froms != null && MonoSingleton<CrossfadeTracker>.Instance.actives[num].froms.Length != 0)
							{
								for (int num2 = MonoSingleton<CrossfadeTracker>.Instance.actives[num].froms.Length - 1; num2 >= 0; num2--)
								{
									if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num].froms[num2] == null) && MonoSingleton<CrossfadeTracker>.Instance.actives[num].froms[num2] == froms[i])
									{
										MonoSingleton<CrossfadeTracker>.Instance.actives[num].StopFade();
									}
								}
							}
							if (MonoSingleton<CrossfadeTracker>.Instance.actives[num].tos != null && MonoSingleton<CrossfadeTracker>.Instance.actives[num].tos.Length != 0)
							{
								for (int num3 = MonoSingleton<CrossfadeTracker>.Instance.actives[num].tos.Length - 1; num3 >= 0; num3--)
								{
									if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num].tos[num3] == null) && MonoSingleton<CrossfadeTracker>.Instance.actives[num].tos[num3] == froms[i])
									{
										MonoSingleton<CrossfadeTracker>.Instance.actives[num].StopFade();
									}
								}
							}
						}
					}
				}
				if (fromMaxVolumes != null && fromMaxVolumes.Length != 0)
				{
					fromMaxVolumes[i] = froms[i].volume;
				}
			}
		}
		if (tos.Length != 0)
		{
			for (int j = 0; j < tos.Length; j++)
			{
				if (tos[j] == null)
				{
					continue;
				}
				if (MonoSingleton<CrossfadeTracker>.Instance.actives.Count > 0)
				{
					bool flag = false;
					for (int num4 = MonoSingleton<CrossfadeTracker>.Instance.actives.Count - 1; num4 >= 0; num4--)
					{
						if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num4] == null))
						{
							if (MonoSingleton<CrossfadeTracker>.Instance.actives[num4].froms != null && MonoSingleton<CrossfadeTracker>.Instance.actives[num4].froms.Length != 0)
							{
								for (int num5 = MonoSingleton<CrossfadeTracker>.Instance.actives[num4].froms.Length - 1; num5 >= 0; num5--)
								{
									if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num4].froms[num5] == null) && MonoSingleton<CrossfadeTracker>.Instance.actives[num4].froms[num5] == tos[j])
									{
										flag = true;
									}
								}
							}
							if (MonoSingleton<CrossfadeTracker>.Instance.actives[num4].tos != null && MonoSingleton<CrossfadeTracker>.Instance.actives[num4].tos.Length != 0)
							{
								for (int num6 = MonoSingleton<CrossfadeTracker>.Instance.actives[num4].tos.Length - 1; num6 >= 0; num6--)
								{
									if (!(MonoSingleton<CrossfadeTracker>.Instance.actives[num4].tos[num6] == null) && MonoSingleton<CrossfadeTracker>.Instance.actives[num4].tos[num6] == tos[j])
									{
										flag = true;
									}
								}
							}
							if (flag)
							{
								MonoSingleton<CrossfadeTracker>.Instance.actives[num4].StopFade();
								toMinVolumes[j] = tos[j].volume;
							}
						}
					}
					if (!flag && firstTime)
					{
						tos[j].volume = 0f;
					}
				}
				else if (firstTime)
				{
					tos[j].volume = 0f;
				}
				if (toMinVolumes != null && toMinVolumes.Length != 0)
				{
					toMinVolumes[j] = tos[j].volume;
				}
				if (toMaxVolumes != null && toMaxVolumes.Length != 0)
				{
					toMaxVolumes[j] = toOriginalVolumes[j];
				}
				if (!tos[j].isPlaying)
				{
					tos[j].Play();
				}
				if (match && froms.Length != 0)
				{
					tos[j].time = froms[0].time % tos[j].clip.length;
				}
			}
		}
		MonoSingleton<CrossfadeTracker>.Instance.actives.Add(this);
		fadeAmount = 0f;
		inProgress = true;
		firstTime = false;
	}

	public void StopFade()
	{
		if (inProgress)
		{
			inProgress = false;
			if (MonoSingleton<CrossfadeTracker>.Instance.actives.Contains(this))
			{
				MonoSingleton<CrossfadeTracker>.Instance.actives.Remove(this);
			}
		}
	}
}

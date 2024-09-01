using UnityEngine;

public class HudOpenEffect : MonoBehaviour
{
	private RectTransform tran;

	private Vector2 originalDimensions;

	private bool gotValues;

	private bool animating;

	public bool skip;

	private void Awake()
	{
		if (tran == null)
		{
			tran = GetComponent<RectTransform>();
		}
		if (!gotValues)
		{
			originalDimensions = new Vector2(tran.localScale.x, tran.localScale.y);
			gotValues = true;
		}
	}

	private void OnEnable()
	{
		Reset();
	}

	private void Update()
	{
		if (!animating)
		{
			return;
		}
		float num = tran.localScale.x;
		float num2 = tran.localScale.y;
		if (!skip)
		{
			if (num != originalDimensions.x)
			{
				num = Mathf.MoveTowards(num, originalDimensions.x, Time.unscaledDeltaTime * ((originalDimensions.x - num + 0.1f) * 30f));
			}
			else if (num2 != originalDimensions.y)
			{
				num2 = Mathf.MoveTowards(num2, originalDimensions.y, Time.unscaledDeltaTime * ((originalDimensions.y - num2 + 0.1f) * 30f));
			}
		}
		else
		{
			num = originalDimensions.x;
			num2 = originalDimensions.y;
		}
		tran.localScale = new Vector3(num, num2, tran.localScale.z);
		if (num == originalDimensions.x && num2 == originalDimensions.y)
		{
			animating = false;
		}
	}

	public Vector2 GetOriginalDimensions()
	{
		Awake();
		return originalDimensions;
	}

	public void Force()
	{
		Awake();
	}

	public void Reset()
	{
		Reset(null);
	}

	public void Reset(Vector2? inheritedOriginalDimensions)
	{
		if (inheritedOriginalDimensions.HasValue)
		{
			originalDimensions = inheritedOriginalDimensions.Value;
			gotValues = true;
		}
		Awake();
		if (!skip)
		{
			tran.localScale = new Vector3(0.05f, 0.05f, tran.localScale.z);
			animating = true;
		}
	}
}

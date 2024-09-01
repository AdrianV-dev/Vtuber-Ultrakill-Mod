using UnityEngine;

public class ScaleNFade : MonoBehaviour
{
	public bool scale;

	public bool fade;

	public FadeType ft;

	public float scaleSpeed;

	public float fadeSpeed;

	private SpriteRenderer sr;

	private LineRenderer lr;

	private Light lght;

	private Renderer rend;

	public bool dontDestroyOnZero;

	public bool lightUseIntensityInsteadOfRange;

	public bool fadeToBlack;

	private Vector3 scaleAmt = Vector3.one;

	private bool hasOpacScale;

	private bool hasTint;

	private bool hasColor;

	private void Start()
	{
		if (fade)
		{
			switch (ft)
			{
			case FadeType.Sprite:
				sr = GetComponent<SpriteRenderer>();
				break;
			case FadeType.Line:
				lr = GetComponent<LineRenderer>();
				break;
			case FadeType.Light:
				lght = GetComponent<Light>();
				break;
			case FadeType.Renderer:
				rend = GetComponent<Renderer>();
				if (rend == null)
				{
					rend = GetComponentInChildren<Renderer>();
				}
				break;
			}
		}
		if (rend != null)
		{
			hasOpacScale = rend.material.HasProperty("_OpacScale");
			hasTint = rend.material.HasProperty("_Tint");
			hasColor = rend.material.HasProperty("_Color");
		}
		scaleAmt = base.transform.localScale;
	}

	private void Update()
	{
		if (scale)
		{
			scaleAmt += Vector3.one * Time.deltaTime * scaleSpeed;
			base.transform.localScale = scaleAmt;
		}
		if (fade)
		{
			switch (ft)
			{
			case FadeType.Sprite:
				UpdateSpriteFade();
				break;
			case FadeType.Light:
				UpdateLightFade();
				break;
			case FadeType.Renderer:
				UpdateRendererFade();
				break;
			case FadeType.Line:
				break;
			}
		}
	}

	private void UpdateSpriteFade()
	{
		Color color = sr.color;
		if (color.a <= 0f)
		{
			if (!dontDestroyOnZero)
			{
				Object.Destroy(base.gameObject);
			}
		}
		else
		{
			color.a -= fadeSpeed * Time.deltaTime;
			sr.color = color;
		}
	}

	private void UpdateLightFade()
	{
		float num = (lightUseIntensityInsteadOfRange ? lght.intensity : lght.range);
		if (num <= 0f)
		{
			if (!dontDestroyOnZero)
			{
				Object.Destroy(base.gameObject);
			}
			return;
		}
		num -= fadeSpeed * Time.deltaTime;
		if (lightUseIntensityInsteadOfRange)
		{
			lght.intensity = num;
		}
		else
		{
			lght.range = num;
		}
	}

	private void UpdateRendererFade()
	{
		if (hasOpacScale)
		{
			UpdateOpacityScale();
		}
		else if (hasTint || hasColor)
		{
			UpdateColorFade();
		}
	}

	private void UpdateOpacityScale()
	{
		float @float = rend.material.GetFloat("_OpacScale");
		if (@float <= 0f && !dontDestroyOnZero)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		@float = Mathf.Max(@float - fadeSpeed * Time.deltaTime, 0f);
		rend.material.SetFloat("_OpacScale", @float);
	}

	private void UpdateColorFade()
	{
		string text = (hasTint ? "_Tint" : "_Color");
		Color color = rend.material.GetColor(text);
		if (fadeToBlack)
		{
			color = Color.Lerp(color, Color.black, fadeSpeed * Time.deltaTime);
		}
		else
		{
			color.a = Mathf.Max(color.a - fadeSpeed * Time.deltaTime, 0f);
		}
		if (color.a <= 0f && !dontDestroyOnZero)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			rend.material.SetColor(text, color);
		}
	}

	private void FixedUpdate()
	{
		if (fade && ft == FadeType.Line)
		{
			Color startColor = lr.startColor;
			startColor.a -= fadeSpeed * Time.deltaTime;
			lr.startColor = startColor;
			startColor = lr.endColor;
			startColor.a -= fadeSpeed * Time.deltaTime;
			lr.endColor = startColor;
			if (lr.startColor.a <= 0f && lr.endColor.a <= 0f && !dontDestroyOnZero)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}

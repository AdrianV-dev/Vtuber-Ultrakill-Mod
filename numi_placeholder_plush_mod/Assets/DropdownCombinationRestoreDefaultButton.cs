using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropdownCombinationRestoreDefaultButton : MonoBehaviour
{
	[Serializable]
	public struct CombinationOption
	{
		public List<BooleanPrefOption> subOptions;
	}

	[Serializable]
	public struct BooleanPrefOption
	{
		public string prefKey;

		public bool isLocal;

		public bool expectedValue;
	}

	[SerializeField]
	private GameObject buttonContainer;

	public int defaultCombination;

	public List<CombinationOption> combinations;

	[SerializeField]
	private TMP_Dropdown dropdown;

	private bool isValueDirty;

	private void Awake()
	{
		dropdown.onValueChanged.AddListener(delegate
		{
			isValueDirty = true;
		});
	}

	private void Start()
	{
		UpdateSelf();
	}

	public void RestoreDefault()
	{
		dropdown.value = defaultCombination;
	}

	private void UpdateSelf()
	{
		CombinationOption combinationOption = combinations[defaultCombination];
		bool flag = true;
		foreach (BooleanPrefOption subOption in combinationOption.subOptions)
		{
			if ((subOption.isLocal ? MonoSingleton<PrefsManager>.Instance.GetBoolLocal(subOption.prefKey) : MonoSingleton<PrefsManager>.Instance.GetBool(subOption.prefKey)) != subOption.expectedValue)
			{
				flag = false;
				break;
			}
		}
		buttonContainer.SetActive(!flag);
	}

	private void LateUpdate()
	{
		if (isValueDirty)
		{
			isValueDirty = false;
			UpdateSelf();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sandbox
{

	public class AlterMenuElements : MonoBehaviour
	{
		public enum Axis
		{
			X,
			Y,
			Z
		}

		[SerializeField]
		private TooltipManager tooltipManager;

		[SerializeField]
		private Transform container;

		[Header("Templates")]
		[SerializeField]
		private GameObject titleTemplate;

		[SerializeField]
		private GameObject boolRowTemplate;

		[SerializeField]
		private GameObject floatRowTemplate;

		[SerializeField]
		private AlterMenuVector3Field vector3RowTemplate;

		[SerializeField]
		private GameObject dropdownRowTemplate;

		private readonly List<int> createdRows = new List<int>();

		private readonly Dictionary<int, Vector3> vector3ValueStore = new Dictionary<int, Vector3>();

		public void CreateTitle(string name)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(titleTemplate, container, worldPositionStays: false);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<TMP_Text>().text = name;
			createdRows.Add(gameObject.GetInstanceID());
		}

		public void CreateBoolRow(string name, bool initialState, Action<bool> callback, string tooltipMessage = null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(boolRowTemplate, container, worldPositionStays: false);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<TMP_Text>().text = name;
			Toggle componentInChildren = gameObject.GetComponentInChildren<Toggle>();
			componentInChildren.SetIsOnWithoutNotify(initialState);
			componentInChildren.interactable = callback != null;
			if (callback != null)
			{
				componentInChildren.onValueChanged.AddListener(delegate (bool state)
				{
					callback(state);
				});
			}
			if (tooltipMessage != null)
			{
				CreateTooltip(gameObject, tooltipMessage);
			}
			createdRows.Add(gameObject.GetInstanceID());
		}

		public void CreateFloatRow(string name, float initialState, Action<float> callback, IConstraints constraints = null, string tooltipMessage = null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(floatRowTemplate, container, worldPositionStays: false);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<TMP_Text>().text = name;
			Slider componentInChildren = gameObject.GetComponentInChildren<Slider>();
			if (constraints is SliderConstraints sliderConstraints)
			{
				componentInChildren.minValue = sliderConstraints.min;
				componentInChildren.maxValue = sliderConstraints.max;
				componentInChildren.wholeNumbers = sliderConstraints.step % 1f == 0f;
				if (!componentInChildren.wholeNumbers)
				{
					gameObject.GetComponentInChildren<SliderLabel>().floor = false;
				}
			}
			componentInChildren.SetValueWithoutNotify(initialState);
			componentInChildren.interactable = callback != null;
			if (callback != null)
			{
				componentInChildren.onValueChanged.AddListener(delegate (float value)
				{
					callback(value);
				});
			}
			if (tooltipMessage != null)
			{
				CreateTooltip(gameObject, tooltipMessage);
			}
			createdRows.Add(gameObject.GetInstanceID());
		}

		public void CreateVector3Row(string name, Vector3 initialState, Action<Vector3> callback, string tooltipMessage = null)
		{
			AlterMenuVector3Field row = UnityEngine.Object.Instantiate(vector3RowTemplate, container, worldPositionStays: false);
			row.gameObject.SetActive(value: true);
			row.nameText.text = name;
			row.xField.SetTextWithoutNotify(initialState.x.ToString(CultureInfo.InvariantCulture));
			row.yField.SetTextWithoutNotify(initialState.y.ToString(CultureInfo.InvariantCulture));
			row.zField.SetTextWithoutNotify(initialState.z.ToString(CultureInfo.InvariantCulture));
			vector3ValueStore.Add(row.GetInstanceID(), initialState);
			row.xField.interactable = callback != null;
			row.yField.interactable = callback != null;
			row.zField.interactable = callback != null;
			if (callback != null)
			{
				row.xField.onValueChanged.AddListener(delegate (string value)
				{
					if (float.TryParse(value, out var result3))
					{
						UpdateVector3Value(row.GetInstanceID(), result3, Axis.X);
						callback(vector3ValueStore[row.GetInstanceID()]);
					}
				});
				row.yField.onValueChanged.AddListener(delegate (string value)
				{
					if (float.TryParse(value, out var result2))
					{
						UpdateVector3Value(row.GetInstanceID(), result2, Axis.Y);
						callback(vector3ValueStore[row.GetInstanceID()]);
					}
				});
				row.zField.onValueChanged.AddListener(delegate (string value)
				{
					if (float.TryParse(value, out var result))
					{
						UpdateVector3Value(row.GetInstanceID(), result, Axis.Z);
						callback(vector3ValueStore[row.GetInstanceID()]);
					}
				});
			}
			if (tooltipMessage != null)
			{
				CreateTooltip(row.gameObject, tooltipMessage);
			}
			createdRows.Add(row.gameObject.GetInstanceID());
		}

		public void UpdateVector3Value(int id, float value, Axis axis)
		{
			if (!vector3ValueStore.ContainsKey(id))
			{
				vector3ValueStore.Add(id, Vector3.zero);
			}
			Vector3 value2 = vector3ValueStore[id];
			switch (axis)
			{
				case Axis.X:
					value2.x = value;
					break;
				case Axis.Y:
					value2.y = value;
					break;
				case Axis.Z:
					value2.z = value;
					break;
				default:
					throw new ArgumentOutOfRangeException("axis", axis, null);
			}
			vector3ValueStore[id] = value2;
		}

		public void CreateEnumRow(string name, int initialState, Action<int> callback, Type type, string tooltipMessage = null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(dropdownRowTemplate, container, worldPositionStays: false);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<TMP_Text>().text = name;
			Dropdown componentInChildren = gameObject.GetComponentInChildren<Dropdown>();
			string[] names = Enum.GetNames(type);
			componentInChildren.ClearOptions();
			componentInChildren.AddOptions(new List<string>(names));
			componentInChildren.SetValueWithoutNotify(initialState);
			componentInChildren.interactable = callback != null;
			if (callback != null)
			{
				componentInChildren.onValueChanged.AddListener(delegate (int value)
				{
					callback(value);
				});
			}
			if (tooltipMessage != null)
			{
				CreateTooltip(gameObject, tooltipMessage);
			}
			createdRows.Add(gameObject.GetInstanceID());
		}

		public void Reset()
		{
			foreach (Transform item in container)
			{
				if (item.gameObject.activeSelf && !(item.gameObject == titleTemplate) && !(item.gameObject == boolRowTemplate) && !(item.gameObject == floatRowTemplate) && createdRows.Contains(item.gameObject.GetInstanceID()))
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
			createdRows.Clear();
		}

		private void CreateTooltip(GameObject row, string message)
		{
			TooltipOnHover tooltipOnHover = row.AddComponent<TooltipOnHover>();
			tooltipOnHover.tooltipManager = tooltipManager;
			tooltipOnHover.text = message;
		}
	}
}
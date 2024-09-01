using System.Collections.Generic;
using System.Linq;
using plog;
using UnityEngine.InputSystem;

public static class InputExtensions
{
	private static readonly Logger Log = new Logger("Input");

	public static string GetBindingDisplayStringWithoutOverride(this InputAction action, InputBinding binding, InputBinding.DisplayStringOptions options = (InputBinding.DisplayStringOptions)0)
	{
		if (binding.isPartOfComposite)
		{
			return binding.ToDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions | InputBinding.DisplayStringOptions.IgnoreBindingOverrides);
		}
		string overridePath = binding.overridePath;
		binding.overridePath = null;
		action.ApplyBindingOverride(binding);
		string result = action.GetBindingDisplayString(binding, InputBinding.DisplayStringOptions.DontIncludeInteractions | InputBinding.DisplayStringOptions.IgnoreBindingOverrides).ToUpper();
		binding.overridePath = overridePath;
		action.ApplyBindingOverride(binding);
		return result;
	}

	public static void WipeAction(this InputAction action, string controlScheme)
	{
		new List<InputBinding>();
		InputActionSetupExtensions.BindingSyntax bindingSyntax = action.ChangeBindingWithGroup(controlScheme);
		while (bindingSyntax.valid)
		{
			bindingSyntax.Erase();
			bindingSyntax = action.ChangeBindingWithGroup(controlScheme);
		}
	}

	public static bool IsActionEqual(this InputAction action, InputAction baseAction, string controlScheme = null)
	{
		Log.Fine("<b>(" + action.name + ")</b> Comparing action " + action.name + " to default action " + baseAction.name);
		List<InputBinding> list = action.bindings.ToList();
		List<InputBinding> list2 = baseAction.bindings.ToList();
		if (controlScheme != null)
		{
			list = list.Where((InputBinding bind) => action.BindingHasGroup(bind, controlScheme)).ToList();
			list2 = list2.Where((InputBinding bind) => baseAction.BindingHasGroup(bind, controlScheme)).ToList();
		}
		if (list.Count != list2.Count)
		{
			Log.Warning($"Binding count mismatch - Action: {list.Count}, Base: {list2.Count}");
			return false;
		}
		Log.Fine($"{action.name} to {baseAction.name} - Action: {list.Count}, Base: {list2.Count}");
		for (int i = 0; i < list.Count; i++)
		{
			InputBinding binding = list[i];
			InputBinding other = list2[i];
			if (!binding.IsBindingEqual(other))
			{
				Log.Fine("Difference found - '" + other.path + "' to '" + binding.path + "'");
				return false;
			}
		}
		return true;
	}

	public static bool IsBindingEqual(this InputBinding binding, InputBinding other)
	{
		if (AreStringsEqual(other.effectivePath, binding.effectivePath) && AreStringsEqual(other.effectiveInteractions, binding.effectiveInteractions))
		{
			return AreStringsEqual(other.effectiveProcessors, binding.effectiveProcessors);
		}
		return false;
	}

	public static bool BindingHasGroup(this InputAction action, InputBinding binding, string group)
	{
		return action.BindingHasGroup(action.GetBindingIndex(binding), group);
	}

	public static bool BindingHasGroup(this InputAction action, int i, string group)
	{
		InputBinding inputBinding = action.bindings[i];
		if (inputBinding.isComposite && action.bindings.Count > i + 1)
		{
			inputBinding = action.bindings[i + 1];
		}
		if (inputBinding.groups == null)
		{
			return false;
		}
		return inputBinding.groups.Contains(group);
	}

	public static int[] GetBindingsWithGroup(this InputAction action, string group)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < action.bindings.Count; i++)
		{
			if (!action.bindings[i].isPartOfComposite && action.BindingHasGroup(i, group))
			{
				list.Add(i);
			}
		}
		return list.ToArray();
	}

	private static bool AreStringsEqual(string str1, string str2)
	{
		if (string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2))
		{
			return true;
		}
		return string.Equals(str1, str2);
	}
}

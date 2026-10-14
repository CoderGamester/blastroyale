using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils.Attributes;
using FirstLight.Game.Localization;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FirstLight.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(LocalizationTermAttribute))]
	public class LocalizationTermPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			StringPropertyPicker.Draw(position, property, label, "Localization Terms",
				GameLocalization.GetTermsList().Select(value => new KeyValuePair<string, string>(value, value)));
		}
	}

	/// <summary>Unity's searchable dropdown updates the real serialized field, retaining Undo support.</summary>
	internal static class StringPropertyPicker
	{
		public static void Draw(Rect position, SerializedProperty property, GUIContent label, string title,
			IEnumerable<KeyValuePair<string, string>> values)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.LabelField(position, label.text, "This picker requires a string field.");
				return;
			}
			EditorGUI.BeginProperty(position, label, property);
			var text = position;
			text.width = Mathf.Max(0, position.width - 76);
			property.stringValue = EditorGUI.TextField(text, label, property.stringValue);
			var button = new Rect(position.xMax - 72, position.y, 72, position.height);
			if (GUI.Button(button, "Choose…"))
			{
				var serializedObject = property.serializedObject;
				var propertyPath = property.propertyPath;
				var dropdown = new StringChoicesDropdown(title, values, value =>
				{
					if (serializedObject.targetObject == null) return;
					serializedObject.Update();
					var target = serializedObject.FindProperty(propertyPath);
					if (target == null) return;
					target.stringValue = value;
					serializedObject.ApplyModifiedProperties();
				});
				dropdown.Show(button);
			}
			EditorGUI.EndProperty();
		}

		private sealed class StringChoicesDropdown : AdvancedDropdown
		{
			private readonly string _title;
			private readonly KeyValuePair<string, string>[] _values;
			private readonly Action<string> _selected;
			public StringChoicesDropdown(string title, IEnumerable<KeyValuePair<string, string>> values, Action<string> selected)
				: base(new AdvancedDropdownState())
			{
				_title = title;
				_values = values.OrderBy(pair => pair.Key).ToArray();
				_selected = selected;
				minimumSize = new Vector2(400, 350);
			}
			protected override AdvancedDropdownItem BuildRoot()
			{
				var root = new AdvancedDropdownItem(_title);
				foreach (var pair in _values) root.AddChild(new ChoiceItem(pair.Key, pair.Value));
				return root;
			}
			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				if (item is ChoiceItem choice) _selected(choice.Value);
			}
			private sealed class ChoiceItem : AdvancedDropdownItem
			{
				public string Value { get; }
				public ChoiceItem(string label, string value) : base(label) { Value = value; }
			}
		}
	}
}

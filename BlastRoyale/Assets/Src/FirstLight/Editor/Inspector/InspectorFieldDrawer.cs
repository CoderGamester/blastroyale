using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FirstLight.Editor.EditorTools;
using FirstLight.Game.Localization;
using FirstLight.Game.Utils.Attributes;
using FirstLight.Inspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.Inspector
{
	/// <summary>Native authoring behavior for the game's audited field annotations.</summary>
	[CustomPropertyDrawer(typeof(InspectorFieldAttribute))]
	public sealed class InspectorFieldDrawer : PropertyDrawer
	{
		private InspectorFieldAttribute Settings => (InspectorFieldAttribute) attribute;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!IsVisible(property)) return -EditorGUIUtility.standardVerticalSpacing;
			var height = EditorGUI.GetPropertyHeight(property, label, true);
			return height + Issues(property).Count * (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!IsVisible(property)) return;
			EditorGUI.BeginProperty(position, label, property);
			var fieldPosition = position;
			fieldPosition.height = EditorGUI.GetPropertyHeight(property, label, true);
			var previousReference = Settings.ChildObjectsOnly ? property.objectReferenceValue : null;
			EditorGUI.BeginChangeCheck();
			using (new EditorGUI.DisabledScope(Settings.ReadOnly))
			{
				if (!string.IsNullOrEmpty(Settings.Options)) DrawOptions(fieldPosition, property, label);
				else if (fieldInfo.GetCustomAttribute<LocalizationTermAttribute>() != null)
					StringPropertyPicker.Draw(fieldPosition, property, label, "Localization Terms", GameLocalization.GetTermsList().Select(value => new KeyValuePair<string, string>(value, value)));
				else if (fieldInfo.GetCustomAttribute<SpriteClassAttribute>() != null)
					StringPropertyPicker.Draw(fieldPosition, property, label, "Generated Sprite Classes", EditorShortcuts.GetAllGeneratedClassNames());
				else EditorGUI.PropertyField(fieldPosition, property, label, true);
			}
			if (EditorGUI.EndChangeCheck())
			{
				var allowed = !Settings.ChildObjectsOnly || IsChildObject(property);
				if (!allowed)
				{
					property.objectReferenceValue = previousReference;
					Debug.LogWarning(property.displayName + " requires an object from this component's child hierarchy.", property.serializedObject.targetObject);
				}
				property.serializedObject.ApplyModifiedProperties();
				if (allowed && !string.IsNullOrEmpty(Settings.OnChanged))
				{
					foreach (var target in property.serializedObject.targetObjects)
					{
						using (var serialized = new SerializedObject(target))
						{
							var changed = serialized.FindProperty(property.propertyPath);
							if (changed == null) continue;
							try { NativeInspectorMembers.Read(NativeInspectorMembers.Owner(changed, fieldInfo), Settings.OnChanged, ReadValue(changed)); }
							catch (Exception exception) { Debug.LogException(exception, target); }
						}
					}
				}
			}
			foreach (var issue in Issues(property))
			{
				fieldPosition.y += fieldPosition.height + EditorGUIUtility.standardVerticalSpacing;
				fieldPosition.height = EditorGUIUtility.singleLineHeight * 2;
				EditorGUI.HelpBox(fieldPosition, issue, MessageType.Error);
			}
			EditorGUI.EndProperty();
		}

		private bool IsVisible(SerializedProperty property)
		{
			if (string.IsNullOrEmpty(Settings.VisibleWhen)) return true;
			try
			{
				var visible = Convert.ToBoolean(NativeInspectorMembers.Read(NativeInspectorMembers.Owner(property, fieldInfo), Settings.VisibleWhen));
				return Settings.InvertVisibility ? !visible : visible;
			}
			catch { return true; } // An invalid condition remains visible and is reported as an authoring error below.
		}

		private List<string> Issues(SerializedProperty property)
		{
			var issues = new List<string>();
			if (Settings.Required && (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null ||
				property.propertyType == SerializedPropertyType.String && string.IsNullOrEmpty(property.stringValue) ||
				property.isArray && property.propertyType != SerializedPropertyType.String && property.arraySize == 0))
				issues.Add(property.displayName + " is required.");
			if (Settings.ChildObjectsOnly && !IsChildObject(property)) issues.Add(property.displayName + " must belong to this object's child hierarchy.");
			try
			{
				var owner = NativeInspectorMembers.Owner(property, fieldInfo);
				if (!string.IsNullOrEmpty(Settings.VisibleWhen)) NativeInspectorMembers.Read(owner, Settings.VisibleWhen);
				if (!string.IsNullOrEmpty(Settings.Validate) && !Convert.ToBoolean(NativeInspectorMembers.Read(owner, Settings.Validate, ReadValue(property))))
					issues.Add(Settings.ValidationMessage ?? property.displayName + " is invalid.");
			}
			catch (Exception exception) { issues.Add("Inspector member failed: " + NativeInspectorMembers.Unwrap(exception).Message); }
			return issues;
		}

		private static bool IsChildObject(SerializedProperty property)
		{
			if (property.objectReferenceValue == null) return true;
			var child = property.objectReferenceValue is GameObject gameObject ? gameObject.transform : (property.objectReferenceValue as Component)?.transform;
			foreach (var target in property.serializedObject.targetObjects)
			{
				var parent = (target as Component)?.transform;
				if (parent == null || child == null || child == parent || !child.IsChildOf(parent)) return false;
			}
			return true;
		}

		private void DrawOptions(Rect position, SerializedProperty property, GUIContent label)
		{
			try
			{
				var source = NativeInspectorMembers.Read(NativeInspectorMembers.Owner(property, fieldInfo), Settings.Options) as IEnumerable;
				if (source == null) throw new InvalidOperationException(Settings.Options + " must return enumerable options.");
				var options = new List<InspectorOption>();
				foreach (var item in source)
					options.Add(item is InspectorOption option ? option : new InspectorOption(item?.ToString() ?? "None", item));
				var current = ReadValue(property);
				var selected = options.FindIndex(option => Equals(option.Value, current));
				if (selected < 0)
				{
					options.Insert(0, new InspectorOption("Current: " + (current?.ToString() ?? "None"), current));
					selected = 0;
				}
				var next = EditorGUI.Popup(position, label.text, selected, options.Select(option => option.Label).ToArray());
				if (next != selected && next >= 0) WriteValue(property, options[next].Value);
			}
			catch (Exception exception) { EditorGUI.LabelField(position, label.text, NativeInspectorMembers.Unwrap(exception).Message); }
		}

		private object ReadValue(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
				case SerializedPropertyType.String: return property.stringValue;
				case SerializedPropertyType.Boolean: return property.boolValue;
				case SerializedPropertyType.Integer: return property.intValue;
				case SerializedPropertyType.Float: return property.floatValue;
				case SerializedPropertyType.Enum: return Enum.ToObject(fieldInfo.FieldType, property.intValue);
				default: return fieldInfo.GetValue(NativeInspectorMembers.Owner(property, fieldInfo));
			}
		}

		private static void WriteValue(SerializedProperty property, object value)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.ObjectReference: property.objectReferenceValue = (Object) value; break;
				case SerializedPropertyType.String: property.stringValue = (string) value; break;
				case SerializedPropertyType.Boolean: property.boolValue = Convert.ToBoolean(value); break;
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Enum: property.intValue = Convert.ToInt32(value); break;
				case SerializedPropertyType.Float: property.floatValue = Convert.ToSingle(value); break;
				default: throw new InvalidOperationException("Options are unsupported for " + property.propertyType);
			}
		}
	}

	internal static class NativeInspectorMembers
	{
		private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		public static object Owner(SerializedProperty property, FieldInfo annotatedField)
		{
			object current = property.serializedObject.targetObject;
			object owner = current;
			var segments = property.propertyPath.Split('.');
			for (var i = 0; i < segments.Length - 1 && current != null; i++)
			{
				if (annotatedField.DeclaringType.IsInstanceOfType(current)) owner = current;
				var segment = segments[i];
				if (segment == "Array") continue;
				if (segment.StartsWith("data[", StringComparison.Ordinal))
				{
					current = ((IList) current)[int.Parse(segment.Substring(5, segment.Length - 6))];
					continue;
				}
				current = Read(current, segment);
			}
			return current != null && annotatedField.DeclaringType.IsInstanceOfType(current) ? current : owner;
		}

		public static object Read(object owner, string memberName, object value = null)
		{
			if (owner == null) throw new InvalidOperationException("The inspector's declaring instance is missing.");
			for (var type = owner.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField(memberName, Flags);
				if (field != null) return field.GetValue(owner);
				var property = type.GetProperty(memberName, Flags);
				if (property != null) return property.GetValue(owner);
				foreach (var method in type.GetMethods(Flags).Where(method => method.Name == memberName))
				{
					var parameters = method.GetParameters();
					if (parameters.Length == 0) return method.Invoke(owner, null);
					if (parameters.Length == 1 && (value == null || parameters[0].ParameterType.IsInstanceOfType(value))) return method.Invoke(owner, new[] {value});
				}
			}
			throw new MissingMemberException(owner.GetType().Name, memberName);
		}

		public static Exception Unwrap(Exception exception) => exception is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : exception;

		public static IEnumerable<MethodInfo> Actions(Type type)
		{
			var names = new HashSet<string>();
			for (; type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject); type = type.BaseType)
				foreach (var method in type.GetMethods(Flags))
					if (method.GetCustomAttribute<InspectorActionAttribute>() != null && names.Add(method.Name)) yield return method;
		}
	}

	[CanEditMultipleObjects]
	[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
	public class NativeGameInspector : UnityEditor.Editor
	{
		private readonly Dictionary<string, bool> _arguments = new();
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			DrawActionsAndIssues();
		}

		protected void DrawActionsAndIssues()
		{
			if (target == null) return;
			foreach (var method in NativeInspectorMembers.Actions(target.GetType()))
			{
				var action = method.GetCustomAttribute<InspectorActionAttribute>();
				var parameters = method.GetParameters();
				if (parameters.Length > 1 || parameters.Length == 1 && parameters[0].ParameterType != typeof(bool))
				{
					EditorGUILayout.HelpBox("Unsupported inspector action signature: " + method.Name, MessageType.Error);
					continue;
				}
				using (new EditorGUI.DisabledScope(action.OnlyInPlayMode && !Application.isPlaying || targets.Length != 1))
				{
					if (parameters.Length == 1)
					{
						if (!_arguments.ContainsKey(method.Name)) _arguments[method.Name] = parameters[0].HasDefaultValue && Equals(parameters[0].DefaultValue, true);
						_arguments[method.Name] = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(parameters[0].Name), _arguments[method.Name]);
					}
					if (GUILayout.Button(action.Label ?? ObjectNames.NicifyVariableName(method.Name)))
					{
						Undo.RecordObject(target, method.Name);
						try { method.Invoke(target, parameters.Length == 0 ? null : new object[] {_arguments[method.Name]}); EditorUtility.SetDirty(target); }
						catch (Exception exception) { Debug.LogException(NativeInspectorMembers.Unwrap(exception), target); }
					}
				}
			}
			foreach (var validator in targets.OfType<IInspectorValidator>())
			{
				try
				{
					foreach (var issue in validator.GetInspectorIssues())
						EditorGUILayout.HelpBox(issue.Message, issue.Severity == InspectorIssueSeverity.Error ? MessageType.Error : issue.Severity == InspectorIssueSeverity.Warning ? MessageType.Warning : MessageType.Info);
				}
				catch (Exception exception) { EditorGUILayout.HelpBox("Inspector validation failed: " + NativeInspectorMembers.Unwrap(exception).Message, MessageType.Error); }
			}
		}
	}

	[CanEditMultipleObjects]
	[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
	public sealed class NativeGameAssetInspector : NativeGameInspector { }
}

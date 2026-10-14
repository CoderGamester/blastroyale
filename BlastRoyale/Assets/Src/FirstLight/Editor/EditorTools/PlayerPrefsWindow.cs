using System;
using System.Linq;
using System.Reflection;
using FirstLight.Game.Services;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>Edits game preference keys using Unity's portable PlayerPrefs API.</summary>
	public sealed class PlayerPrefsWindow : EditorWindow
	{
		private enum ValueType { String, Int, Float }
		private string _key = "ss_ServerRegion";
		private string _stringValue = "";
		private int _intValue;
		private float _floatValue;
		private ValueType _type;
		private Vector2 _scroll;

		[MenuItem("FLG/Tools/Player Preferences")]
		public static void Open() => GetWindow<PlayerPrefsWindow>("Player Preferences");

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Select a game preference or enter any key. Changes apply to this Editor's local player data.", MessageType.Info);
			_scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180));
			foreach (var property in typeof(LocalPrefsService).GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(p => p.Name))
			{
				var key = "ss_" + property.Name;
				if (!GUILayout.Button(key + (PlayerPrefs.HasKey(key) ? "" : " (not saved)"))) continue;
				_key = key;
				var arguments = property.PropertyType.GenericTypeArguments;
				_type = arguments.Length == 1 && (arguments[0] == typeof(int) || arguments[0] == typeof(bool)) ? ValueType.Int : ValueType.String;
				Read();
			}
			EditorGUILayout.EndScrollView();
			_key = EditorGUILayout.TextField("Key", _key);
			_type = (ValueType)EditorGUILayout.EnumPopup("Type", _type);
			switch (_type)
			{
				case ValueType.String: _stringValue = EditorGUILayout.TextArea(_stringValue); break;
				case ValueType.Int: _intValue = EditorGUILayout.IntField("Value", _intValue); break;
				case ValueType.Float: _floatValue = EditorGUILayout.FloatField("Value", _floatValue); break;
			}
			using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_key)))
			{
				if (GUILayout.Button("Read")) Read();
				if (GUILayout.Button("Save"))
				{
					switch (_type)
					{
						case ValueType.String: PlayerPrefs.SetString(_key, _stringValue); break;
						case ValueType.Int: PlayerPrefs.SetInt(_key, _intValue); break;
						case ValueType.Float: PlayerPrefs.SetFloat(_key, _floatValue); break;
					}
					PlayerPrefs.Save();
				}
				if (GUILayout.Button("Delete key") && EditorUtility.DisplayDialog("Delete local preference", "Delete " + _key + "?", "Delete", "Cancel"))
				{
					PlayerPrefs.DeleteKey(_key);
					PlayerPrefs.Save();
				}
			}
		}

		private void Read()
		{
			_stringValue = PlayerPrefs.GetString(_key);
			_intValue = PlayerPrefs.GetInt(_key);
			_floatValue = PlayerPrefs.GetFloat(_key);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Game.MonoComponent.Collections;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FirstLight.Editor.Collections
{
	/// <summary>Native asset browser for character models, icons, and Unity previews.</summary>
	public class CollectionsWindow : EditorWindow
	{
		private const string CharactersPath = "Assets/AddressableResources/Collections/CharacterSkins";
		private readonly List<string> _paths = new();
		private string _selectedPath;
		private string _search = "";
		private GameObject _prefab;
		private Sprite _icon;
		private Texture2D _texture;
		private UnityEditor.Editor _preview;
		private Vector2 _scroll;
		private Vector2 _detailScroll;

		[MenuItem("FLG/Collections/Open Menu")]
		private static void OpenWindow() => GetWindow<CollectionsWindow>("Collections").Show();
		private void OnEnable() => RefreshAssets();
		private void OnDisable() { if (_preview != null) DestroyImmediate(_preview); }

		private void RefreshAssets()
		{
			_paths.Clear();
			_paths.AddRange(AssetDatabase.FindAssets("t:Model t:Prefab Char_", new[] {CharactersPath})
				.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path));
		}

		private void Load(string path)
		{
			_selectedPath = path;
			_prefab = path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
			_icon = path == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace(".prefab", ".png").Replace(".fbx", ".png").Replace("Char_", "Icon_Char_"));
			_texture = path == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace("Char_", "T_Char_").Replace(".fbx", ".png").Replace(".prefab", ".png"));
			if (_preview != null) DestroyImmediate(_preview);
			_preview = _prefab == null ? null : UnityEditor.Editor.CreateEditor(_prefab);
		}

		private void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				_search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField);
				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65))) RefreshAssets();
			}
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope(GUILayout.Width(220)))
				{
					_scroll = EditorGUILayout.BeginScrollView(_scroll);
					if (GUILayout.Button("Characters / Tools")) Load(null);
					foreach (var path in _paths)
					{
						var name = Path.GetFileNameWithoutExtension(path).Replace("Char_", "");
						if (!name.Contains(_search, StringComparison.OrdinalIgnoreCase)) continue;
						if (GUILayout.Toggle(path == _selectedPath, name, "Button") && path != _selectedPath) Load(path);
					}
					EditorGUILayout.EndScrollView();
				}
				using (new EditorGUILayout.VerticalScope())
				{
					_detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
					if (_selectedPath == null)
					{
						EditorGUILayout.LabelField("Character tools", EditorStyles.boldLabel);
						if (GUILayout.Button("Open Character Testing Scene")) EditorSceneManager.OpenScene("Assets/Art/SceneTests/AnimTest.unity");
						EditorGUILayout.LabelField("Collections_Characters Addressable paths", EditorStyles.boldLabel);
						var group = AddressableAssetSettingsDefaultObject.Settings?.FindGroup("Collections_Characters");
						if (group == null) EditorGUILayout.HelpBox("The Collections_Characters Addressable group is missing.", MessageType.Warning);
						else foreach (var entry in group.entries) EditorGUILayout.SelectableLabel(entry.AssetPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
					}
					else
					{
						EditorGUILayout.HelpBox("IDs and asset references come from the imported files. Edit the assets through their Inspector.", MessageType.Info);
						using (new EditorGUILayout.HorizontalScope())
						{
							EditorGUILayout.SelectableLabel(Path.GetFileNameWithoutExtension(_selectedPath), GUILayout.Height(EditorGUIUtility.singleLineHeight));
							if (GUILayout.Button("Copy ID", GUILayout.Width(75))) GUIUtility.systemCopyBuffer = Path.GetFileNameWithoutExtension(_selectedPath);
						}
						using (new EditorGUI.DisabledScope(true))
						{
							EditorGUILayout.ObjectField("Model", _prefab, typeof(GameObject), false);
							EditorGUILayout.ObjectField("Icon", _icon, typeof(Sprite), false);
							EditorGUILayout.ObjectField("Texture", _texture, typeof(Texture2D), false);
							EditorGUILayout.ObjectField("Skin component", _prefab?.GetComponent<CharacterSkinMonoComponent>(), typeof(CharacterSkinMonoComponent), false);
						}
						if (_prefab != null && GUILayout.Button("Inspect Model")) Selection.activeObject = _prefab;
						if (_preview != null && _preview.HasPreviewGUI()) _preview.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200, 254, GUILayout.ExpandWidth(true)), GUIStyle.none);
						EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
						EditorGUILayout.LabelField("Coming soon…");
					}
					EditorGUILayout.EndScrollView();
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Game.Configs;
using GameLovers.GoogleSheetImporter;
using GameLoversEditor.GoogleSheetImporter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace FirstLight.Editor.Configs
{
	/// <summary>Native Unity asset inspection, sheet import, and saving in one config browser.</summary>
	public class ConfigsMenuWindow : EditorWindow
	{
		private const string ConfigsFolder = "Assets/AddressableResources/Configs";
		private readonly List<Object> _assets = new();
		private Dictionary<Type, IGoogleSheetConfigsImporter> _importers = new();
		private Object _selected;
		private UnityEditor.Editor _inspector;
		private UnityWebRequest _request;
		private string _search = "";
		private Vector2 _listScroll;
		private Vector2 _inspectorScroll;

		[MenuItem("FLG/Configs/Open Menu")]
		private static void OpenWindow() => GetWindow<ConfigsMenuWindow>("Configs").Show();

		private void OnEnable() => RefreshAssets();

		private void RefreshAssets()
		{
			_assets.Clear();
			_assets.AddRange(AssetDatabase.GetAllAssetPaths()
				.Where(path => path.StartsWith(ConfigsFolder + "/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path))
				.OrderBy(path => path).Select(AssetDatabase.LoadMainAssetAtPath).Where(asset => asset != null));
			_importers = ConfigUtils.GetAllImporters();
		}

		private void OnDisable()
		{
			_request?.Abort();
			_request?.Dispose();
			_request = null;
			if (_inspector != null) DestroyImmediate(_inspector);
		}

		private void Select(Object asset)
		{
			_selected = asset;
			if (_inspector != null) DestroyImmediate(_inspector);
			_inspector = asset == null ? null : UnityEditor.Editor.CreateEditor(asset);
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
				using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
				{
					_listScroll = EditorGUILayout.BeginScrollView(_listScroll);
					foreach (var asset in _assets)
					{
						if (asset == null) continue;
						var path = AssetDatabase.GetAssetPath(asset);
						var relativePath = path.Substring(ConfigsFolder.Length + 1);
						if (!relativePath.Contains(_search, StringComparison.OrdinalIgnoreCase)) continue;
						var category = asset is GameConfigs ? "Game Config" : relativePath.StartsWith("Settings/") ? relativePath : "Configs/" + relativePath;
						if (GUILayout.Toggle(_selected == asset, category, "Button"))
							if (_selected != asset) Select(asset);
					}
					EditorGUILayout.EndScrollView();
				}
				using (new EditorGUILayout.VerticalScope())
				{
					if (_selected == null || _inspector == null)
					{
						EditorGUILayout.HelpBox("Select a config asset to inspect its serialized data.", MessageType.Info);
						return;
					}
					using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
					{
						if (GUILayout.Button("Select in Project", EditorStyles.toolbarButton)) Selection.activeObject = _selected;
						if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssetIfDirty(_selected);
						using (new EditorGUI.DisabledScope(_request != null || !_importers.ContainsKey(_selected.GetType())))
							if (GUILayout.Button(_request == null ? "Import Sheet" : "Importing…", EditorStyles.toolbarButton)) ImportSelected();
					}
					_inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
					EditorGUI.BeginChangeCheck();
					_inspector.OnInspectorGUI();
					if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_selected);
					EditorGUILayout.EndScrollView();
				}
			}
		}

		private void ImportSelected()
		{
			if (_selected == null || !_importers.TryGetValue(_selected.GetType(), out var importer)) return;
			var request = UnityWebRequest.Get(importer.GoogleSheetUrl.Replace("edit#", "export?format=csv&"));
			_request = request;
			request.SendWebRequest().completed += _ =>
			{
				try
				{
					if (request.result != UnityWebRequest.Result.Success)
					{
						Debug.LogError($"Sheet import failed: {request.error}");
						return;
					}
					var values = CsvParser.ConvertCsv(request.downloadHandler.text);
					if (values.Count == 0) throw new InvalidDataException("The sheet did not return CSV rows.");
					importer.Import(values);
					AssetDatabase.SaveAssets();
					if (this != null) Repaint();
					Debug.Log($"Finished importing sheet data from {importer.GetType().Name}");
				}
				catch (Exception exception) { Debug.LogException(exception); }
				finally
				{
					request.Dispose();
					if (_request == request) _request = null;
				}
			};
		}
	}
}

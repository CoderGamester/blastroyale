using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Editor.AssetImporters;
using FirstLight.Editor.SheetImporters;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using GameLovers.Services.AssetsImporter.Editor;
using GameLoversEditor.GoogleSheetImporter;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Debug = UnityEngine.Debug;


namespace FirstLight.Editor.EditorTools.MapGenerator
{
	/// <summary>
	/// This editor window provides functionality for generating a map
	/// </summary>
	public class MapGeneratorEditorWindow : EditorWindow
	{
		[Serializable]
		public struct AllowedMapStruct
		{
			public string Id;
			public bool AllowedMap;
		}

		private const string _exportFolderPath = "Assets/AddressableResources/Scenes";
		public GameId _mapGameId;
		public List<AllowedMapStruct> _allowedMaps = new List<AllowedMapStruct>();

		public int _maxPlayers = 20;
		public bool _isTestMap = true;
		public bool _isCustomOnly = false;
		public float _dropSelectionSize = 1f;
		private const string _sceneIdScriptPath = "Src/FirstLight/Game/Ids/SceneId.cs";
		private AssetReference _assetReference;

		private void OnEnable()
		{
			if (_allowedMaps.Count > 0)
			{
				return;
			}

			var gameModeConfigs = AssetDatabase.LoadAssetAtPath<GameModeConfigs>("Assets/AddressableResources/Configs/GameModeConfigs.asset");
			if (gameModeConfigs == null)
			{
				Debug.LogError("Game Mode Configs reference invalid");

				return;
			}

			for (int i = 0; i < gameModeConfigs.Configs.Count; i++)
			{
				_allowedMaps.Add(new AllowedMapStruct()
				{
					Id = gameModeConfigs.Configs[i].Id,
					AllowedMap = false
				});
			}
		}

		[MenuItem("FLG/Generators/Map Generator EditorWindow")]
		private static void OpenWindow()
		{
			GetWindow<MapGeneratorEditorWindow>("Map Generator EditorWindow").Show();
		}



		private Vector2 _scroll;

		private void OnGUI()
		{
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			_mapGameId = (GameId) EditorGUILayout.EnumPopup("Map ID", _mapGameId);
			_maxPlayers = EditorGUILayout.IntSlider("Max Players", _maxPlayers, 1, 48);
			_isTestMap = EditorGUILayout.Toggle("Test Map", _isTestMap);
			_isCustomOnly = EditorGUILayout.Toggle("Custom Only", _isCustomOnly);
			_dropSelectionSize = EditorGUILayout.FloatField("Drop Selection Size", _dropSelectionSize);
			EditorGUILayout.LabelField("Allowed Game Modes", EditorStyles.boldLabel);
			for (var i = 0; i < _allowedMaps.Count; i++)
			{
				var map = _allowedMaps[i];
				map.AllowedMap = EditorGUILayout.Toggle(map.Id, map.AllowedMap);
				_allowedMaps[i] = map;
			}
			if (GUILayout.Button("Generate Map Configs")) GenerateMapAssetConfigs();
			if (GUILayout.Button("Export Scene")) ExportScene();
			EditorGUILayout.EndScrollView();
		}

		private void ExportScene()
		{
			if (_mapGameId == GameId.Random)
			{
				Debug.LogError($"Invalid map game id [{_mapGameId}]");

				return;
			}

			var sceneDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), _exportFolderPath);

			if (!Directory.Exists(sceneDirectoryPath))
			{
				Debug.LogError($"Invalid export folder path [{sceneDirectoryPath}]");

				return;
			}


			var scenePath = $"{_exportFolderPath}/{_mapGameId}.unity";
			if (File.Exists(scenePath))
			{
				Debug.LogError($"A scene already exists at {scenePath}; select a new map ID to export.");
				return;
			}
			var quantumMap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/Environment/Root/QuantumMap.prefab");
			var sceneIdPath = Path.Combine(Application.dataPath, _sceneIdScriptPath);

			if (sceneIdPath == "" || !File.Exists(sceneIdPath))
			{
				Debug.LogError($"Invalid scene id path [{sceneIdPath}]");

				return;
			}

			if (quantumMap == null || quantumMap.GetComponent<QuantumMapData>() == null)
			{
				Debug.LogError("The QuantumMap prefab with MapData is required for scene export.");
				return;
			}
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null || settings.DefaultGroup == null)
			{
				Debug.LogError("Addressable settings and a default group are required for scene export.");
				return;
			}
			var path = $"Assets/AddressableResources/Maps/{_mapGameId}.asset";
			if (AssetDatabase.LoadMainAssetAtPath(path) != null)
			{
				Debug.LogError($"A map asset already exists at {path}; select a new map ID to export.");
				return;
			}

			string fileContents = File.ReadAllText(sceneIdPath);
			string modifiedContents = AddEnumValue(fileContents, "SceneId", _mapGameId.ToString());

			if (!string.IsNullOrEmpty(modifiedContents))
			{
				File.WriteAllText(sceneIdPath, modifiedContents);
			}

			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			if (!EditorSceneManager.SaveScene(scene, scenePath))
			{
				EditorSceneManager.CloseScene(scene, true);
				Debug.LogError($"Could not save the generated scene at {scenePath}.");
				return;
			}
			var group = settings.DefaultGroup;
			var guid = AssetDatabase.GUIDFromAssetPath(scenePath);

			var entry = settings.CreateOrMoveEntry(guid.ToString(), group, readOnly: false, postEvent: true);
			entry.address = AssetDatabase.GUIDToAssetPath(guid);

			_assetReference = new AssetReference(guid.ToString());

			var go = Instantiate(quantumMap, Vector3.zero, Quaternion.identity);
			UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

			Map asset = ScriptableObject.CreateInstance<Map>();

			// Create a path for the new asset
			// Save the asset at the specified path
			AssetDatabase.CreateAsset(asset, path);

			var mapData = go.GetComponent<QuantumMapData>();
			mapData.AssetRef = asset;
			QuantumUnityDBUtilities.RefreshGlobalDB();

			QuantumEditorAutoBaker.BakeMap(mapData, mapData.BakeAllMode);

			asset.Scene = _mapGameId.ToString();
			asset.ScenePath = Path.Combine(_exportFolderPath, $"{_mapGameId}.unity");
			asset.SceneGuid = guid.ToString();

			EditorUtility.SetDirty(asset);

			EditorSceneManager.SaveScene(scene, scenePath);
			EditorSceneManager.CloseScene(scene, true);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("Finished scene export");
		}

		private void GenerateMapAssetConfigs()
		{
			var mapConfigs = AssetDatabase.LoadAssetAtPath<MapConfigs>("Assets/AddressableResources/Configs/MapConfigs.asset");
			if (mapConfigs == null)
			{
				Debug.LogError("Map Configs reference invalid");

				return;
			}

			var gameModeConfigs = AssetDatabase.LoadAssetAtPath<GameModeConfigs>("Assets/AddressableResources/Configs/GameModeConfigs.asset");
			if (gameModeConfigs == null)
			{
				Debug.LogError("Game Mode Configs reference invalid");

				return;
			}

			if (mapConfigs.Configs.Any(c => c.Map == _mapGameId))
			{
				var configs = mapConfigs.Configs.Where(c => c.Map == _mapGameId).ToList();

				for (int i = 0; i < configs.Count(); i++)
				{
					mapConfigs.Configs.Remove(configs[i]);
				}
			}

			mapConfigs.Configs.Add(new QuantumMapConfig()
			{
				Map = _mapGameId,
				MaxPlayers = _maxPlayers,
				IsTestMap = _isTestMap,
				IsCustomOnly = _isCustomOnly,
				DropSelectionSize = _dropSelectionSize
			});

			EditorUtility.SetDirty(mapConfigs);

			for (int i = 0; i < gameModeConfigs.Configs.Count(); i++)
			{
				gameModeConfigs.Configs[i].AllowedMaps.Remove(_mapGameId);
			}

			foreach (var m in _allowedMaps)
			{
				for (int i = 0; i < gameModeConfigs.Configs.Count(); i++)
				{
					if (m.AllowedMap && m.Id == gameModeConfigs.Configs[i].Id)
					{
						gameModeConfigs.Configs[i].AllowedMaps.Add(_mapGameId);
					}
				}
			}

			EditorUtility.SetDirty(gameModeConfigs);
			AssetDatabase.SaveAssets();

			Debug.Log("Finished generating map asset configs");
		}


		private static string AddEnumValue(string fileContents, string enumName, string enumValue)
		{
			int enumEntryStartIndex = fileContents.IndexOf(enumValue);
			if (enumEntryStartIndex != -1)
			{
				return null;
			}

			// Find the enum declaration
			string enumDeclaration = $"enum {enumName}";

			int enumStartIndex = fileContents.IndexOf(enumDeclaration);
			if (enumStartIndex == -1)
			{
				throw new InvalidOperationException($"Enum '{enumName}' not found in the file.");
			}

			// Find the closing brace of the enum declaration
			int enumEndIndex = fileContents.IndexOf('}', enumStartIndex);
			if (enumEndIndex == -1)
			{
				throw new InvalidOperationException($"Closing brace of enum '{enumName}' not found in the file.");
			}

			// Find the last enum value
			int lastEnumValueIndex = fileContents.LastIndexOf(',', enumEndIndex);
			if (lastEnumValueIndex == -1)
			{
				lastEnumValueIndex = enumEndIndex - 1;
			}

			// Insert a comma if it doesn't exist
			if (fileContents[lastEnumValueIndex] != ',')
			{
				fileContents = fileContents.Insert(lastEnumValueIndex + 1, ",");
			}

			// Insert the new enum value before the closing brace
			string newEnumValue = $"\t{enumValue},\n\t";

			string modifiedContents = fileContents.Insert(enumEndIndex, newEnumValue);

			return modifiedContents;
		}
	}
}

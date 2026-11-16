using FirstLight.Inspector;
using DebugUI;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FirstLight.Game.MonoComponent.Collections;
using Quantum;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
#endif

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class DebugCharacters : DebugUIBuilderBase
	{
#if UNITY_EDITOR
		public readonly string ALL_VALUE = "ALL";
		private static Dictionary<string, MethodInfo> _triggers;

		public Transform CharacterAnchor;
		public Transform Cameras;

		private string _currentCharacter;
		[NonSerialized] public MethodInfo _currentTrigger;
		private Transform _currentCamera;
		private GameObject _currentWeapon;
		private int _characterRotation;
		private WeaponType _weaponType = WeaponType.Melee;
		private bool _moving;
		private bool _aiming;

		public void Trigger()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				_currentTrigger.Invoke(skin, new object[] { });
			}
		}

		public void UpdateAnimationState()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.Moving = _moving;
				skin.Aiming = _aiming;
				skin.WeaponType = _weaponType;
			}
		}

		public void UpdateCurrentWeapon()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.WeaponAnchor.localScale = Vector3.one;

				while (skin.WeaponAnchor.childCount > 0)
				{
					DestroyImmediate(skin.WeaponAnchor.GetChild(0).gameObject);
				}

				if (!_currentWeapon) continue;
				var weaponInstance = (GameObject) PrefabUtility.InstantiatePrefab(_currentWeapon, skin.WeaponAnchor);
				foreach (var child in weaponInstance.GetComponentsInChildren<Transform>(true))
					child.gameObject.layer = skin.gameObject.layer;
				// TODO: Temporary if, new weapons don't have a child
				if (weaponInstance.transform.childCount > 0)
				{
					var weaponChild = weaponInstance.transform.GetChild(0);
					weaponChild.localPosition = Vector3.zero;
					weaponChild.localRotation = Quaternion.identity;
				}
			}
		}

		private static Dictionary<string, MethodInfo> GetTriggers()
		{
			if (_triggers == null)
			{
				_triggers = typeof(CharacterSkinMonoComponent).GetMethods()
					.Where(m => m.Name.StartsWith("Trigger") && m.GetParameters().Length == 0)
					.ToDictionary(m => m.Name, m => m);
			}

			return _triggers;
		}

		public void ActivateCamera(Transform transform)
		{
			for (var i = 0; i < Cameras.childCount; i++)
			{
				var c = Cameras.GetChild(i);
				c.gameObject.SetActive(transform == c);
			}
		}

		public void UpdateCharacterRotation()
		{
			var skins = CharacterAnchor.transform.GetComponentsInChildren<CharacterSkinMonoComponent>();

			foreach (var skin in skins)
			{
				skin.transform.localRotation = Quaternion.Euler(0, _characterRotation, 0);
			}
		}

		public void LoadSkin(string id)
		{
			if (id == ALL_VALUE)
			{
				LoadAll();
				return;
			}

			DestroyAllSkins();

			var prfb = PrefabUtility.InstantiatePrefab(
				AssetDatabase.LoadAssetAtPath<CharacterSkinMonoComponent>(AssetDatabase.GUIDToAssetPath(_currentCharacter)), CharacterAnchor);

			UpdateAnimationState();
			UpdateCurrentWeapon();
			UpdateCharacterRotation();
		}

		public void DestroyAllSkins()
		{
			while (CharacterAnchor.childCount > 0)
			{
				DestroyImmediate(CharacterAnchor.GetChild(0).gameObject);
			}
		}

		private Dictionary<string, GameObject> GetAllWeapons()
		{
			var weapons = AssetDatabase.FindAssets("t:Prefab Weapon_", new[] {"Assets/AddressableResources/Weapons"});

			return weapons.Select(AssetDatabase.GUIDToAssetPath)
				.ToDictionary(path => Path.GetFileNameWithoutExtension(path).Replace("Weapon_", ""),
					path => AssetDatabase.LoadAssetAtPath<GameObject>(path));
		}

		[InspectorAction("Load all skins")]
		public void LoadAll()
		{
			DestroyAllSkins();

			const float SPACING = 2f;
			const int ROWS = 6;
			var indexZ = 0;
			var indexX = 0;
			var assets = AssetDatabase.FindAssets("t:Prefab Char_", new[] {"Assets/AddressableResources/Collections/CharacterSkins"});
			foreach (var character in assets)
			{
				var prfb = PrefabUtility.InstantiatePrefab(
					AssetDatabase.LoadAssetAtPath<CharacterSkinMonoComponent>(AssetDatabase.GUIDToAssetPath(character)), CharacterAnchor);

				var instance = (CharacterSkinMonoComponent) prfb;

				instance.transform.position = new Vector3(-indexX * SPACING, 0, indexZ * SPACING);

				if (indexZ == ROWS - 1)
				{
					indexX++;
				}

				indexZ = (indexZ + 1) % ROWS;
			}

			UpdateAnimationState();
			UpdateCurrentWeapon();
			UpdateCharacterRotation();
		}

		// Start is called before the first frame update
		protected override void Configure(IDebugUIBuilder builder)
		{
			_characterRotation = 90;
			_currentTrigger = GetTriggers().First().Value;

			builder.ConfigureWindowOptions(options =>
			{
				options.Title = "Debug Character";
				options.Draggable = true;
			});

			var chars = AssetDatabase.FindAssets("t:Prefab Char_", new[] {"Assets/AddressableResources/Collections/CharacterSkins"})
				.ToDictionary(id => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(id)).Replace("Char_", ""), id => id);
			_currentCharacter = chars.First().Value;

			chars.Add("All", ALL_VALUE);
			builder.Factories.Add(new CharacterChoiceFactory<string>("Skin", chars, () => _currentCharacter, id =>
			{
				_currentCharacter = id;
				LoadSkin(id);
			}));

			builder.Factories.Add(new CharacterChoiceFactory<MethodInfo>("Trigger", GetTriggers(), () => _currentTrigger,
				id => _currentTrigger = id));
			builder.AddButton("Play", Trigger);

			var cameras = new Dictionary<string, Transform>();
			for (int i = 0; i < Cameras.childCount; i++)
			{
				cameras[Cameras.GetChild(i).name] = Cameras.GetChild(i);
			}

			builder.AddField("Moving", () => _moving, (v) =>
			{
				_moving = v;
				UpdateAnimationState();
			});
			builder.AddField("Aiming", () => _aiming, (v) =>
			{
				_aiming = v;
				UpdateAnimationState();
			});
			var weaponDict = GetAllWeapons();
			weaponDict["None"] = null;
			builder.Factories.Add(new CharacterChoiceFactory<GameObject>("Weapon", weaponDict, () => _currentWeapon, v =>
			{
				_currentWeapon = v;
				UpdateCurrentWeapon();
			}));
			builder.AddField("Weapon Type", () => _weaponType, (v) =>
			{
				_weaponType = v;
				UpdateAnimationState();
			});
			builder.Factories.Add(new CharacterChoiceFactory<Transform>("Camera", cameras, () => _currentCamera, c =>
			{
				_currentCamera = c;
				ActivateCamera(c);
			}));
			builder.AddSlider("Character Rotation", 0, 360, () => _characterRotation, (v) =>
			{
				_characterRotation = v;
				UpdateCharacterRotation();
			});
			UpdateCharacterRotation();
			_currentCamera = cameras.First().Value;
			ActivateCamera(_currentCamera);
			UpdateAnimationState();
			UpdateCurrentWeapon();
		}

		// The current public DebugUI builder supports primitive/enum fields. Its actual
		// factory extension point supplies the typed choices needed by this game tool.
		private sealed class CharacterChoiceFactory<T> : IDebugUIElementFactory
		{
			private readonly string _label;
			private readonly Dictionary<string, T> _choices;
			private readonly Func<T> _getter;
			private readonly Action<T> _setter;

			public CharacterChoiceFactory(string label, Dictionary<string, T> choices, Func<T> getter, Action<T> setter)
			{
				_label = label;
				_choices = choices;
				_getter = getter;
				_setter = setter;
			}

			public VisualElement CreateVisualElement(ICollection<IDisposable> disposables)
			{
				var labels = _choices.Keys.ToList();
				var field = new DropdownField(_label, labels, 0);
				void Refresh()
				{
					var selected = _getter();
					foreach (var choice in _choices)
					{
						if (!EqualityComparer<T>.Default.Equals(choice.Value, selected)) continue;
						field.SetValueWithoutNotify(choice.Key);
						return;
					}
				}
				Refresh();
				field.RegisterValueChangedCallback(evt => _setter(_choices[evt.newValue]));
				field.RegisterCallback<AttachToPanelEvent>(_ =>
					field.GetFirstAncestorOfType<DebugUI.UIElements.DebugWindow>()?.SetValueWithoutNotify(true));
				var update = field.schedule.Execute(Refresh).Every(100);
				disposables.Add(new ScheduledChoiceRefresh(update));
				return field;
			}
		}

		private sealed class ScheduledChoiceRefresh : IDisposable
		{
			private readonly IVisualElementScheduledItem _update;
			public ScheduledChoiceRefresh(IVisualElementScheduledItem update) => _update = update;
			public void Dispose() => _update.Pause();
		}

		protected override void Awake()
		{
			base.Awake();
			LoadSkin(_currentCharacter);
		}
#else
	protected override void Configure(IDebugUIBuilder builder){}
#endif
	}
}

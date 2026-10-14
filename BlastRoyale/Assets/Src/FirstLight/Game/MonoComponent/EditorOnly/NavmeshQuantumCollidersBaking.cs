using FirstLight.Inspector;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using Quantum;
using Object = UnityEngine.Object;

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class NavmeshQuantumCollidersBaking : MonoBehaviour, IInspectorValidator
	{
		[Serializable]
		public class GroupConfig
		{
			public string Name;
			[InspectorField(ChildObjectsOnly = true)] public Transform Destination;
			public Transform[] SourceTransform;
			public bool Walkable;
		}

		[SerializeField, UnityEngine.Header("Config")]
		private GroupConfig[] _groups;

		[SerializeField]
		private NavMeshCleaner _cleaner;

		[SerializeField, HideInInspector] private Transform _floorObj;

		public IEnumerable<InspectorIssue> GetInspectorIssues()
		{
#if UNITY_EDITOR
			if (_groups == null || _groups.Length == 0)
			{
				yield return new InspectorIssue("No collider groups configured.", InspectorIssueSeverity.Error);
			}
			else
			{
				foreach (var groupConfig in _groups)
				{
					if (groupConfig == null || groupConfig.SourceTransform == null || groupConfig.SourceTransform.Length == 0)
					{
						yield return new InspectorIssue("No source colliders configured for group " + groupConfig?.Name + "!",
							InspectorIssueSeverity.Error);
					}
				}
			}

			if (!_cleaner || (_cleaner.m_WalkablePoint?.Count ?? 0) == 0)
			{
				yield return new InspectorIssue("There are no walkable points configured in the Cleaner script!\nCleaner will do nothing!",
					InspectorIssueSeverity.Warning);
			}
#endif
			yield break;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			_cleaner ??= GetComponentInChildren<NavMeshCleaner>();
		}

		[InspectorAction("Create Colliders")]


		public void CreateColliders()
		{
			CreateFloor();
			foreach (var groupConfig in _groups)
			{
				// Already created
				if (groupConfig.Destination.transform.childCount > 0)
				{
					Debug.LogError($"Colliders already created for group {groupConfig.Name}!");
					return;
				}

				foreach (var source in groupConfig.SourceTransform)
				{
					CreateCollidersBasedOn(source, groupConfig.Destination, groupConfig.Walkable);
				}
			}
		}

		[InspectorAction("Delete Colliders")]


		public void DeleteColliders()
		{
			if (_floorObj != null)
			{
				DestroyImmediate(_floorObj.gameObject);
			}

			var children = new List<GameObject>();
			foreach (var groupConfig in _groups)
			{
				foreach (Transform child in groupConfig.Destination)
				{
					children.Add(child.gameObject);
				}
			}

			children.ForEach(DestroyImmediate);
		}

		[InspectorAction("Apply Cleaner")]


		private void ApplyCleaner()
		{
			_cleaner.Build();
			_cleaner.SetMeshVisible(true);
			foreach (var o in _cleaner.m_Child)
			{
				var newCollider = o.AddComponent<MeshCollider>();
				newCollider.sharedMesh = o.GetComponent<MeshFilter>().sharedMesh;
			}
		}

		[InspectorAction("Reset Cleaner")]


		private void ResetCleaner()
		{
			_cleaner.Reset();
		}

		private void CreateCollidersBasedOn(Transform parentSource, Transform parentDestination, bool walkable)
		{
			var colliders = parentSource.GetComponentsInChildren<QuantumStaticBoxCollider2D>();
			foreach (var boxCollider2D in colliders)
			{
				var go = new GameObject("TempCollider " + boxCollider2D.gameObject.name, typeof(BoxCollider), typeof(NavMeshModifierVolume));
				var goPosition = boxCollider2D.transform.position;
				goPosition.y = 0;
				var goScale = boxCollider2D.transform.localScale;
				goScale.y = 5;
				go.transform.position = goPosition;
				go.transform.rotation = Quaternion.Euler(boxCollider2D.transform.rotation.eulerAngles);
				go.transform.localScale = goScale;
				var bx = go.GetComponent<BoxCollider>();
				var colliderSize = boxCollider2D.Size.ToUnityVector3();
				colliderSize.y = 5;
				bx.center = boxCollider2D.PositionOffset.ToUnityVector3();
				bx.size = colliderSize;
				var modifierVolume = go.GetComponent<NavMeshModifierVolume>();
				modifierVolume.center = bx.center;
				modifierVolume.size = bx.size;
				modifierVolume.area = walkable ? 0 : 1;
				go.transform.SetParent(parentDestination);
			}
		}

		private void CreateFloor()
		{
			if (_floorObj != null)
			{
				DestroyImmediate(_floorObj.gameObject);
			}

			var mapSize = GetComponentInParent<QuantumMapData>().GetAsset(true).WorldSize;
			_floorObj = new GameObject("TempColliderFloor", typeof(BoxCollider),typeof(NavMeshModifier)).transform;
			_floorObj.parent = this.transform;
			_floorObj.transform.position = Vector3.zero;
			_floorObj.transform.localScale = new Vector3(mapSize, 0.01f, mapSize);
		}

		private async UniTaskVoid Bake(bool applyCleaner)
		{
			// Bake mesh with islands
			var navmeshSurface = GetComponent<NavMeshSurface>();
			if (Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface)) return;

			DeleteColliders();
			CreateColliders();
			Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] {navmeshSurface});
			await UniTask.WaitUntil(() => !Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface));
			if (!applyCleaner || _cleaner.m_WalkablePoint?.Count == 0)
			{
				DeleteColliders();
				return;
			}

			// Now apply the cleaner creating a new mesh collider to remove the islands
			ApplyCleaner();
			Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] {navmeshSurface});
			await UniTask.WaitUntil(() => !Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(navmeshSurface));
			ResetCleaner();
			DeleteColliders();
		}

		[InspectorAction("Bake Navmesh")]

		public void BakeUnityNavMesh(bool applyCleaner = true)
		{
			Bake(applyCleaner).Forget();
		}
#endif
	}
}

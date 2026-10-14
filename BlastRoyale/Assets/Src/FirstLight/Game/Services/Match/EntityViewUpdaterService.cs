using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using GameLovers.Services;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This Service allows to access <see cref="QuantumEntityView"/> from any point in the game.
	/// Also overrides the Quantum's <see cref="EntityViewUpdaterService"/> by extending the possibility to only delete
	/// GameObjects on late update to allow other components to access it on the same frame if marked to be manual desposable.
	/// </summary>
	public interface IEntityViewUpdaterService
	{
		/// <summary>
		/// Requests the <see cref="QuantumEntityView"/> existing in the game world that represents the given <paramref name="entityRef"/>.
		/// Returns true if the entity exists in the active or manual entities to destroy.
		/// </summary>
		/// <remarks>
		/// Use this call instead of <see cref="QuantumEntityViewUpdater.GetView"/> in potential cases where the entity is
		/// marked to be manual destroyed
		/// </remarks>
		bool TryGetView(EntityRef entityRef, out QuantumEntityView view);

		/// <summary>
		/// Requests the <see cref="QuantumEntityView"/> existing in the game world that represents the given <paramref name="entityRef"/>.
		/// </summary>
		/// <remarks>
		/// Use this call instead of <see cref="QuantumEntityViewUpdater.GetView"/> in potential cases where the entity is
		/// marked to be manual destroyed
		/// </remarks>
		QuantumEntityView GetManualView(EntityRef entityRef);

		/// <summary>
		/// Set the parents of the view in the scene, only used to organize objects in runtime
		/// </summary>
		void SetParents(QuantumEntityView view, string group = null);
	}

	/// <inheritdoc cref="IEntityViewUpdaterService" />
	public class EntityViewUpdaterService : QuantumEntityViewUpdater, IEntityViewUpdaterService
	{
		private readonly IDictionary<EntityRef, QuantumEntityView>
			_viewsToDestroy = new Dictionary<EntityRef, QuantumEntityView>(256);

		private readonly List<QuantumEntityView> _viewsListToDestroy = new List<QuantumEntityView>(256);
		private readonly Dictionary<QuantumEntityView, QuantumGame> _pendingGames = new();

		private IGameServices _gameServices;

		private Dictionary<AssetGuid, List<QuantumEntityView>> _inactiveBullets = new();

		private Dictionary<string, GameObject> _entityViewGroups = new();

		private GameObject _viewParent;

		private void PollBullet(QuantumEntityView view)
		{
			view.gameObject.SetActive(false);
			if (!_inactiveBullets.TryGetValue(view.AssetGuid, out var list))
			{
				list = new List<QuantumEntityView>();
				_inactiveBullets[view.AssetGuid] = list;
			}
			list.Add(view);
		}

		/// <summary>
		/// Override to poll bullets so we dont need to re-create the views for every new bullet.
		/// This saves us some CPU time and makes memory usage more stable.
		/// </summary>
		protected override QuantumEntityView CreateEntityViewInstance(Quantum.EntityView asset, Vector3? position = null,
															   Quaternion? rotation = null)
		{
			if (_inactiveBullets.TryGetValue(asset.Guid, out var inactiveList))
			{
				while (inactiveList.Count > 0)
				{
					var bullet = inactiveList[0];
					inactiveList.RemoveAt(0);
					if (bullet == null) continue; // A scene unload may have destroyed a pooled object.
					if (position.HasValue) bullet.transform.position = position.Value;
					if (rotation.HasValue) bullet.transform.rotation = rotation.Value;
					bullet.gameObject.SetActive(true);
					return bullet;
				}
			}
			return base.CreateEntityViewInstance(asset, position, rotation);
		}

		public void SetParents(QuantumEntityView view, string group = null)
		{
			if (_viewParent == null)
			{
				_viewParent = new GameObject("Views");
				_entityViewGroups.Clear();
			}
			GameObject parent;
			// Need to move to the parent, because when creating it will use the same scene as the current MonoBehaviour.
			// And this is a service running on the main scene, with this line it will move to the same scene as the view
			SceneManager.MoveGameObjectToScene(_viewParent, view.gameObject.scene);
			if (group == null)
			{
				parent = _viewParent;
			}
			else if (!_entityViewGroups.TryGetValue(group, out parent) || parent == null)
			{
				parent = new GameObject($"{group}");
				parent.transform.SetParent(_viewParent.transform);
				_entityViewGroups[group] = parent;
			}
			view.transform.SetParent(parent.transform);
		}

		private new void Awake()
		{
			base.Awake();

			_viewParent = new GameObject("Views");
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		private void LateUpdate()
		{
			var pendingCount = _viewsListToDestroy.Count;
			for (var i = 0; i < pendingCount; i++)
			{
				var view = _viewsListToDestroy[i];
				if (view != null) DisposeView(_pendingGames[view], view);
				_pendingGames.Remove(view);
				if ((object) view != null && _viewsToDestroy.TryGetValue(view.EntityRef, out var queued) && ReferenceEquals(queued, view))
					_viewsToDestroy.Remove(view.EntityRef);
			}
			// Destruction callbacks may queue other views; process those on the next LateUpdate.
			_viewsListToDestroy.RemoveRange(0, pendingCount);
		}

		private void DisposeView(QuantumGame game, QuantumEntityView view)
		{
			var manual = view.ManualDisposal;
			view.ManualDisposal = false;
			try { base.DestroyEntityView(game, view); }
			finally { if (view != null) view.ManualDisposal = manual; }
		}

		protected override void DestroyEntityViewInstance(QuantumEntityView instance)
		{
			if (instance.gameObject.CompareTag("Bullet")) PollBullet(instance);
			else base.DestroyEntityViewInstance(instance);
		}

		/// <inheritdoc />
		public bool TryGetView(EntityRef entityRef, out QuantumEntityView view)
		{
			return ActiveViews.TryGetValue(entityRef, out view) || _viewsToDestroy.TryGetValue(entityRef, out view);
		}

		/// <inheritdoc />
		public QuantumEntityView GetManualView(EntityRef entityRef)
		{
			if (!TryGetView(entityRef, out var view))
			{
				throw new KeyNotFoundException($"There is no {nameof(QuantumEntityView)} for the given entity {entityRef}.");
			}

			return view;
		}

		protected override void DestroyEntityView(QuantumGame game, QuantumEntityView view)
		{
			if (view == null || _pendingGames.ContainsKey(view)) return;
			// Quantum 3 deactivates native view components inside its destruction lifecycle.
			// Let the base perform that lifecycle exactly once, retaining the original game's late disposal.
			if (game?.Frames?.Predicted == null)
			{
				DisposeView(game, view);
				return;
			}
			if (view.ManualDisposal)
			{
				_viewsListToDestroy.Add(view);
				_pendingGames.Add(view, game);
				_viewsToDestroy[view.EntityRef] = view;
				return;
			}
			base.DestroyEntityView(game, view);
		}

		protected override void DisableMapEntityInstance(QuantumEntityView instance)
		{
			if (!instance.IsDestroyed())
			{
				base.DisableMapEntityInstance(instance);
			}
		}

		private async void SpawnDeathMarker(ItemData marker, Vector3 position)
		{
			var obj = await _gameServices.CollectionService.LoadCollectionItem3DModel(marker);

			obj.transform.position = position;
		}
	}
}

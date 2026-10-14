using GameLovers.Services.Pooling;
using System;
using GameLovers.Services;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	public class PoolServiceTest
	{
		private PoolService _poolService;
		private ObjectPool<PoolableEntity> _pool;
		private PoolableEntity _lastCreatedEntity;

		public class PoolableEntity : IPoolEntitySpawn, IPoolEntityDespawn
		{
			public int SpawnCalls { get; private set; }
			public int DespawnCalls { get; private set; }
			public void OnSpawn() => SpawnCalls++;
			public void OnDespawn() => DespawnCalls++;
		}

		[SetUp]
		public void Init()
		{
			_poolService = new PoolService();
			_pool = new ObjectPool<PoolableEntity>(0, () => _lastCreatedEntity = new PoolableEntity());
			
			_poolService.AddPool(_pool);
		}

		[Test]
		public void AddPool_Successfully()
		{
			Assert.True(_poolService.TryGetPool<PoolableEntity>(out _));
		}

		[Test]
		public void AddPool_SameType_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _poolService.AddPool(_pool));
		}

		[Test]
		public void Spawn_Successfully()
		{
			var entity = _poolService.Spawn<PoolableEntity>();
			Assert.AreSame(_lastCreatedEntity, entity);
			Assert.AreSame(entity, _pool.SpawnedReadOnly[0]);
			Assert.AreEqual(1, entity.SpawnCalls);
		}

		[Test]
		public void Spawn_NotAddedPool_ThrowsException()
		{
			_poolService = new PoolService();
			
			Assert.Throws<ArgumentException>(() => _poolService.Spawn<PoolableEntity>());
		}

		[Test]
		public void Despawn_Successfully()
		{
			var entity = _poolService.Spawn<PoolableEntity>();
			_poolService.Despawn(entity);
			Assert.AreEqual(1, entity.DespawnCalls);
			Assert.IsEmpty(_pool.SpawnedReadOnly);
		}

		[Test]
		public void Despawn_NotAddedPool_ThrowsException()
		{
			var entity = new PoolableEntity();
			
			_poolService = new PoolService();
			
			Assert.Throws<ArgumentException>(() => _poolService.Despawn(entity));
		}

		[Test]
		public void DespawnAll_Successfully()
		{
			var first = _poolService.Spawn<PoolableEntity>();
			var second = _poolService.Spawn<PoolableEntity>();
			_poolService.DespawnAll<PoolableEntity>();
			Assert.AreEqual(1, first.DespawnCalls);
			Assert.AreEqual(1, second.DespawnCalls);
			Assert.IsEmpty(_pool.SpawnedReadOnly);
		}

		[Test]
		public void RemovePool_Successfully()
		{
			_poolService.RemovePool<PoolableEntity>();
				
			Assert.False(_poolService.TryGetPool<PoolableEntity>(out _));
		}

		[Test]
		public void RemovePool_NotAdded_DoesNothing()
		{
			_poolService = new PoolService();
			
			Assert.DoesNotThrow(() => _poolService.RemovePool<PoolableEntity>());
		}
	}
}
using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial class QuantumDestructibleConfig
	{
		public GameId Id;
		public AssetRef<EntityPrototype> AssetRef;
		public AssetRef<EntityPrototype> ProjectileAssetRef;
		public uint Health;
		public FP SplashRadius;
		public uint PowerAmount;
		public FP DestructionLengthTime;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumDestructibleConfig"/>
	/// </summary>
	public partial class QuantumDestructibleConfigs : Quantum.AssetObject
	{
		public List<QuantumDestructibleConfig> QuantumConfigs = new List<QuantumDestructibleConfig>();
	}
}

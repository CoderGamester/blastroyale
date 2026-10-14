using System;

namespace Quantum
{
	/// <summary>
	/// This decision checks if we should do a Skydive drop.
	/// </summary>
	[Serializable]
	public partial class ShouldPerformSkydiveDecision : HFSMDecision
	{
		/// <inheritdoc />
		public override unsafe bool Decide(Frame f, EntityRef e, ref AIContext aiContext)
		{
			return f.Context.GameModeConfig.SkydiveSpawn;
		}
	}
}

using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Fetches the current frame time
	/// </summary>
	[Serializable]
	public class CurrentFrameTimeFunction : AIFunction<FP>
	{
		/// <inheritdoc />
		public override FP Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			return f.Time;
		}
	}
}

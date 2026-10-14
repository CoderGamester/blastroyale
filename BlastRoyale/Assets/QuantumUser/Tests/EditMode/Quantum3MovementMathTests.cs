using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Tests.EditorMode.Systems
{
	/// <summary>Steering helpers operate on the actual SDK Transform2D/FP types and native lookup tables.</summary>
	public unsafe class Quantum3MovementMathTests
	{
		[OneTimeSetUp]
		public void LoadRealSdkLookupTables()
		{
			FPMathUtils.LoadLookupTables();
			Assert.That(FPLut.IsLoaded, Is.True, "Install the SDK QuantumLookupTables resource before running math fixtures.");
		}

		[TestCase(1, 0)]
		[TestCase(0, 1)]
		[TestCase(-1, 0)]
		[TestCase(0, -1)]
		public void CardinalAimDirectionSurvivesRotationRoundTrip(int x, int y)
		{
			var expected = new FPVector2(x, y);
			var actual = expected.ToRotation().ToDirection();
			// Native trigonometric lookup tables are approximate; tolerate at most 1/256 in each axis.
			Assert.That(FPMath.Abs(actual.X - expected.X).RawValue, Is.LessThanOrEqualTo(256L));
			Assert.That(FPMath.Abs(actual.Y - expected.Y).RawValue, Is.LessThanOrEqualTo(256L));
		}

		[Test]
		public void FullSteeringPointsTransformTowardsAim()
		{
			var transform = default(Transform2D);
			transform.Rotation = FP.Rad_90;
			QuantumHelpers.LookAt2d(&transform, FPVector2.Up, FP._1);
			Assert.That(FPMath.Abs(transform.Rotation).RawValue, Is.LessThanOrEqualTo(256L));
		}

		[Test]
		public void ZeroSteeringWeightKeepsCurrentRotation()
		{
			var transform = default(Transform2D);
			transform.Rotation = FP.Rad_90;
			QuantumHelpers.LookAt2d(&transform, FPVector2.Right, FP._0);
			Assert.That(transform.Rotation.RawValue, Is.EqualTo(FP.Rad_90.RawValue));
		}
	}
}

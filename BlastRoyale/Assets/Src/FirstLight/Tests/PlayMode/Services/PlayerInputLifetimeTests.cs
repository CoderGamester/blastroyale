using FirstLight.Game.Services;
using NUnit.Framework;
using Quantum;
using UnityEngine.InputSystem;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.PlayMode.Services
{
	public class PlayerInputLifetimeTests : InputTestFixture
	{
		[Test]
		public void EndMatch_TwoMatches_RemovesOwnedNativeSubscriptions()
		{
			using var input = new PlayerInputService(null, null, null);
			for (var match = 0; match < 2; match++)
			{
				input.OnMatchStarted(null, false);
				AssertSubscriptions(input, true);
				input.OnMatchEnded(null, false);
				AssertSubscriptions(input, false);
			}
		}

		[Test]
		public void Dispose_WithoutMatchEnded_RemovesNativeSubscriptions()
		{
			var input = new PlayerInputService(null, null, null);
			try
			{
				input.OnMatchStarted(null, false);
				AssertSubscriptions(input, true);
			}
			finally
			{
				input.Dispose();
			}
			AssertSubscriptions(input, false);
		}

		[TestCase(Key.Q)]
		[TestCase(Key.E)]
		public void Dispose_WithHeldSpecial_DoesNotSendCommandToStoppedRunner(Key key)
		{
			var keyboard = InputSystem.AddDevice<Keyboard>();
			var input = new PlayerInputService(null, null, null);
			try
			{
				input.OnMatchStarted(null, false);
				Press(keyboard[key]);
				Assert.That(keyboard[key].isPressed, Is.True);
				var special = key == Key.Q ? input.Input.Gameplay.SpecialButton0 : input.Input.Gameplay.SpecialButton1;
				Assert.That(special.IsPressed(), Is.True, "The real gameplay action must be held before teardown.");
			}
			finally
			{
				input.Dispose();
			}
			AssertSubscriptions(input, false);
		}

		private static void AssertSubscriptions(PlayerInputService input, bool expected)
		{
			Assert.That(QuantumCallback.IsSubscribed<CallbackPollInput>(input), Is.EqualTo(expected));
			Assert.That(QuantumEvent.IsSubscribed<EventOnPlayerKnockedOut>(input), Is.EqualTo(expected));
			Assert.That(QuantumEvent.IsSubscribed<EventOnPlayerRevived>(input), Is.EqualTo(expected));
		}
	}
}

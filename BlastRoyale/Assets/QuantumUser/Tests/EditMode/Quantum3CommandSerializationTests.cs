using System;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;

namespace FirstLight.Tests.EditorMode.Systems
{
	/// <summary>Actual game command payloads round-trip through Quantum 3's native bit codec.</summary>
	public class Quantum3CommandSerializationTests
	{
		[TestCase(GameId.FlagPooEmoji)]
		[TestCase(GameId.FlagCryEmoji)]
		[TestCase(GameId.MalePunk)]
		public void EmojiIdRoundTrips(GameId id)
		{
			var decoded = RoundTrip(new PlayerEmojiCommand { Emoji = id }, new PlayerEmojiCommand());
			Assert.That(decoded.Emoji, Is.EqualTo(id));
		}

		[TestCase(-1)]
		[TestCase(0)]
		[TestCase(2)]
		public void WeaponSlotPayloadPreservesSignedIndex(int index)
		{
			var decoded = RoundTrip(new WeaponSlotSwitchCommand { WeaponSlotIndex = index }, new WeaponSlotSwitchCommand());
			Assert.That(decoded.WeaponSlotIndex, Is.EqualTo(index));
		}

		[TestCase(TeamPingType.General)]
		[TestCase(TeamPingType.Chest)]
		[TestCase(TeamPingType.Player)]
		[TestCase(TeamPingType.Equipment)]
		public void TeamPingRoundTripsNegativeAndFractionalCoordinates(TeamPingType type)
		{
			var position = new FPVector2(FP.FromRaw(-32769), FP.FromRaw(196609));
			var decoded = RoundTrip(new TeamPositionPingCommand { Position = position, Type = type }, new TeamPositionPingCommand());
			Assert.That(decoded.Position.X.RawValue, Is.EqualTo(position.X.RawValue));
			Assert.That(decoded.Position.Y.RawValue, Is.EqualTo(position.Y.RawValue));
			Assert.That(decoded.Type, Is.EqualTo(type));
		}

		[TestCase(0L, 0L, 0)]
		[TestCase(-196609L, 131073L, 1)]
		[TestCase(65537L, -32769L, 2)]
		public void SpecialAimRoundTripsEveryFixedPointBit(long x, long y, int slot)
		{
			var command = new SpecialUsedCommand { AimInput = new FPVector2(FP.FromRaw(x), FP.FromRaw(y)), SpecialIndex = slot };
			var decoded = RoundTrip(command, new SpecialUsedCommand());
			Assert.That(decoded.AimInput.X.RawValue, Is.EqualTo(x));
			Assert.That(decoded.AimInput.Y.RawValue, Is.EqualTo(y));
			Assert.That(decoded.SpecialIndex, Is.EqualTo(slot));
		}

		[Test]
		public void RepeatedSerializationProducesIdenticalPayload()
		{
			var command = new TeamPositionPingCommand
			{
				Position = new FPVector2(FP.FromRaw(-1000003), FP.FromRaw(1000007)), Type = TeamPingType.Player
			};
			var first = Encode(command);
			for (var attempt = 0; attempt < 100; attempt++)
			{
				var current = Encode(command);
				Assert.That(current.BitCount, Is.EqualTo(first.BitCount));
				Assert.That(current.Bytes, Is.EqualTo(first.Bytes));
			}
		}

		private static T RoundTrip<T>(T input, T output) where T : DeterministicCommand
		{
			var payload = Encode(input);
			var reader = new BitStream(payload.Bytes, payload.Bytes.Length) { Writing = false };
			output.Serialize(reader);
			Assert.That(reader.Position, Is.EqualTo(payload.BitCount), "Decode must consume the complete command payload.");
			Assert.That(reader.Overflowing, Is.False);
			return output;
		}

		private static (byte[] Bytes, int BitCount) Encode(DeterministicCommand command)
		{
			var buffer = new byte[256];
			var writer = new BitStream(buffer, buffer.Length) { Writing = true };
			command.Serialize(writer);
			Assert.That(writer.Overflowing, Is.False);
			var bytes = new byte[writer.BytesRequired];
			Array.Copy(buffer, bytes, bytes.Length);
			return (bytes, writer.Position);
		}
	}
}

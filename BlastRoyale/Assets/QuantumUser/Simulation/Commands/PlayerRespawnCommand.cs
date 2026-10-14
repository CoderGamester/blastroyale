using System;
using Photon.Deterministic;

namespace Quantum.Commands
{
	/// <summary>
	/// This command makes the current player respawn again in the game
	/// </summary>
	public unsafe class PlayerRespawnCommand : CommandBase
	{
		/// <inheritdoc />
		public override void Serialize(BitStream stream)
		{
		}

		/// <inheritdoc />
		internal override void Execute(Frame f, PlayerRef playerRef)
		{
			var entity = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData[playerRef].Entity;

			if (!f.TryGet<DeadPlayerCharacter>(entity, out var deadPlayer) ||
			    f.Time < deadPlayer.TimeOfDeath + f.GameConfig.PlayerRespawnTime)
			{
				Log.Error($"The player {playerRef} is not ready to be respawn yet");
				return;
			}

			var aiContext = default(AIContext);
			var agent = f.Unsafe.GetPointer<HFSMAgent>(entity);
			f.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var blackboard);
			aiContext.SetHFSMAgentAndBlackboard(agent, entity, blackboard);
			HFSMManager.TriggerEvent(f, entity, Constants.RESPAWN_EVENT, ref aiContext);
		}
	}
}

using System;
using Photon.Deterministic;

namespace Quantum
{

	unsafe partial class RuntimePlayer
	{
		public string PlayerId;
		public string UnityId;
		public string PlayerName;
		public GameId[] Cosmetics;
		public uint PlayerLevel;
		public uint PlayerTrophies;
		public uint LeaderboardRank;
		public FPVector2 NormalizedSpawnPosition;
		public string PartyId = string.Empty;
		public string AvatarUrl;
		public bool UseBotBehaviour;
		public byte TeamColor;


	}
}

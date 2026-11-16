using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace FirstLight.Game.Configs.Remote
{
	[Serializable]
	public class PlayfabMatchmakingConfig
	{
		public string QueueName;
		public int QueueTimeoutTimeInSeconds;
		public bool FailsOnTimeout;
	}

	[Serializable]
	public class TeamSizeConfig : PlayfabMatchmakingConfig
	{
		public string IconSpriteClass;
		public string EventImageModifierByTeam;
	}

	public class MatchmakingQueuesConfig : Dictionary<string, TeamSizeConfig>
	{
	}
}

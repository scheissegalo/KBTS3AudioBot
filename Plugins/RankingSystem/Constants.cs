using System;
using System.Collections.Generic;
using TSLib;

namespace RankingSystem
{
	internal class Constants
	{
		public readonly List<uint> BotGroups = new List<uint> { 11, 47, 115 };
		public readonly ServerGroupId AdminGroup = (ServerGroupId)90;
		public readonly ChannelId onlineCountChannel = (ChannelId)171;
		public readonly ChannelId AfkChannel = (ChannelId)18;

		// Define the reset time as a TimeSpan (e.g., 18:00 for 6 PM)
		public readonly TimeSpan ResetTime = new TimeSpan(6, 0, 0); // 6AM
		public readonly int AFKTime = 60; // in Minutes
		public readonly int UpdateInterval = 2; // in minutes
	}
}

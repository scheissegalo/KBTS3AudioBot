using System;
using System.Collections.Generic;
using TSLib;

namespace RankingSystem
{
	internal class Constants
	{
		//public readonly List<uint> BotGroups = new List<uint> { 11, 47, 115 };
		public readonly List<ServerGroupId> BotGroupsE = new List<ServerGroupId> { (ServerGroupId)11, (ServerGroupId)47, (ServerGroupId)115 };
		public readonly ServerGroupId AdminGroup = (ServerGroupId)90;
		public readonly ChannelId onlineCountChannel = (ChannelId)171;
		public readonly ChannelId AfkChannel = (ChannelId)18;
		public readonly ServerGroupId NoAfkGroup = (ServerGroupId)69;
		public ServerGroupId memberGroup = (ServerGroupId)7;
		public ChannelId CustomParentChannel = (ChannelId)68;

		// Define the reset time as a TimeSpan (e.g., 18:00 for 6 PM)
		public readonly TimeSpan ResetTime = new TimeSpan(6, 0, 0); // 6AM
		public readonly int AFKTime = 30; // in Minutes
		public readonly int UpdateInterval = 2; // in minutes
		public readonly float ScorePerTick = 0.03f;
		public readonly bool onboardingEnabled = true;
		public readonly bool SendAFKNotice = false;
		public readonly bool SendDaylyMessage = false;
		public readonly TimeSpan timeToAllowChannelCreation = TimeSpan.FromMinutes(30);

		public readonly string messageHeader = @$"
[b]══════════════════════════════════════════════[/b]
[b]  [color=#24336b]███[/color][color=#0095db]██[/color]  [color=#24336b]North[/color][color=#0095db]Industries[/color] - Free Secure Gaming Services  [color=#0095db]██[/color][color=#24336b]███[/color]  [/b]
[b]══════════════════════════════════════════════[/b]

";
		public readonly string messageFooter = $@"

[b]══════════════════════════════════════════════[/b]
[b][url=https://north-industries.com]HOME[/url] | [url=https://north-industries.com/news/]NEWS[/url] | [url=https://north-industries.com/teamspeak-connect/#rules]RULES[/url] | [url=https://north-industries.com/teamspeak-help]HELP[/url] | [url=https://teamspeak-servers.org/server/12137/vote/]VOTE[/url] | [url=https://north-industries.com/ts-viewer/]TS-VIEWER[/url] | [url=https://north-industries.com/teamspeak-connect/]SHARE[/url][/b]

[b]Need help? Just type ""[color=#00FF00]help[/color]""[/b]

[color=#24336b]North[/color][color=#0095db]Industries[/color] [i]""Your Gaming Journey Starts Here!""[/i]
";

		public List<ServerGroupInfo> _serverGroupList = new List<ServerGroupInfo>
			{
				// Year 1
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromMinutes(30), ServerGroup = (ServerGroupId)23 },//Frischling
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromHours(1), ServerGroup = (ServerGroupId)24 },//Halbe stunde 
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromHours(2), ServerGroup = (ServerGroupId)25 },//Eine Stunde
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromHours(5), ServerGroup = (ServerGroupId)26 },//2 Stunden 
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromHours(10), ServerGroup = (ServerGroupId)27 },//5 StundenNo password login
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(1), ServerGroup = (ServerGroupId)28 },//10 Stunden
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(2), ServerGroup = (ServerGroupId)29 },//1 Tag
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(4), ServerGroup = (ServerGroupId)30 },//2 Tage
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(7), ServerGroup = (ServerGroupId)31 },//4 Tage
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(15), ServerGroup = (ServerGroupId)32 },//7 Tage
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(30), ServerGroup = (ServerGroupId)33 },//15 Tage
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(60), ServerGroup = (ServerGroupId)35 },//1 Monat
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(90), ServerGroup = (ServerGroupId)36 },//2 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(120), ServerGroup = (ServerGroupId)37 },//3 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(150), ServerGroup = (ServerGroupId)38 },//4 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(180), ServerGroup = (ServerGroupId)39 },//5 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(210), ServerGroup = (ServerGroupId)40 },//6 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(240), ServerGroup = (ServerGroupId)41 },//7 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(270), ServerGroup = (ServerGroupId)42 },//8 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(300), ServerGroup = (ServerGroupId)43 },//9 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(330), ServerGroup = (ServerGroupId)44 },//10 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(360), ServerGroup = (ServerGroupId)45 },//11 Monate
				// Year 2
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(395), ServerGroup = (ServerGroupId)46 },//12 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(425), ServerGroup = (ServerGroupId)93 },//13 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(455), ServerGroup = (ServerGroupId)94 },//14 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(485), ServerGroup = (ServerGroupId)95 },//15 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(515), ServerGroup = (ServerGroupId)96 },//16 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(545), ServerGroup = (ServerGroupId)97 },//17 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(575), ServerGroup = (ServerGroupId)98 },//18 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(605), ServerGroup = (ServerGroupId)99 },//19 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(635), ServerGroup = (ServerGroupId)100 },//20 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(665), ServerGroup = (ServerGroupId)101 },//21 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(695), ServerGroup = (ServerGroupId)102 },//22 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(730), ServerGroup = (ServerGroupId)116 },//23 Monate
				// Year 3
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(760), ServerGroup = (ServerGroupId)117 },//12 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(790), ServerGroup = (ServerGroupId)118 },//13 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(820), ServerGroup = (ServerGroupId)119 },//14 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(850), ServerGroup = (ServerGroupId)120 },//15 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(880), ServerGroup = (ServerGroupId)121 },//16 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(910), ServerGroup = (ServerGroupId)122 },//17 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(940), ServerGroup = (ServerGroupId)123 },//18 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(970), ServerGroup = (ServerGroupId)124 },//19 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(1000), ServerGroup = (ServerGroupId)125 },//20 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(1030), ServerGroup = (ServerGroupId)126 },//21 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(1060), ServerGroup = (ServerGroupId)127 },//22 Monate
				new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(1090), ServerGroup = (ServerGroupId)128 },//23 Monate
				//new ServerGroupInfo { OnlineTimeThreshold = TimeSpan.FromDays(755), ServerGroup = (ServerGroupId)129 },//24 Monate
			};

	}

}

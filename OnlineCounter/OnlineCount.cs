using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Commands;
using TSLib.Full;
using TSLib.Messages;
using TS3AudioBot.Config;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;

namespace OnlineCounter
{
	public class OnlineCount : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;


		private readonly ulong channelToUpdateId = 171; // replace with the ID of the channel to update
		private readonly TimeSpan resetInterval = TimeSpan.FromDays(1);
		private readonly List<uint> excludedGroups = new List<uint> { 11, 47, 115 }; // replace with the IDs of the excluded groups

		private List<string> userIDS = new List<string>();

		private readonly object countLock = new object();
		private uint count = 0;
		private uint countToday = 0;
		private DateTime lastResetTime = DateTime.MinValue;
		private bool isChecking = false;

		public OnlineCount(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public void Initialize()
		{
			tsFullClient.OnClientEnterView += OnUserConnected;
			tsFullClient.OnClientLeftView += OnUserDisconnected;
			Task.Run(ResetCountPeriodically);
			CheckOnlineUsers(true);
		}

		private void OnUserConnected(object sender, IEnumerable<ClientEnterView> clients)
		{
			foreach (var user in clients)
			{
				if (user.ClientType.Equals(ClientType.Full))
				{
					//Console.WriteLine(clients);
					CheckOnlineUsers(true);
				}
				else
				{
					DateTime now = DateTime.Now;
					Console.WriteLine(now + " | " +user.Name + " Query");
					continue;
				}
			}
		}

		private void OnUserDisconnected(object sender, IEnumerable<ClientLeftView> clients)
		{
			CheckOnlineUsers(false);
		}

		private async void CheckOnlineUsers(bool connected)
		{
			if (isChecking) { return; }
			isChecking = true;
			//Console.WriteLine("Checking online users");
			bool doUpdate = false;
			uint oldCount = count;
			count = 0;
			var allUsers = await tsFullClient.ClientList();

			try
			{
				foreach (var user in allUsers.Value)
				{

					//var ServerGroupIDs = await tsFullClient.ServerGroupsByClientDbId(user.DatabaseId);
					var fulluser = await tsFullClient.ClientInfo(user.ClientId);
					//Valid Full CLient
					bool skipClient = false;
					bool containsUserID = userIDS.Any(item => item == fulluser.Value.Uid.ToString());

					if (fulluser.Value.ClientType.Equals(ClientType.Full))
					{
						foreach (var sg in excludedGroups)
						{
							ServerGroupId newSG = (ServerGroupId)sg;
							if (fulluser.Value.ServerGroups.Contains(newSG))
							{
								//Console.WriteLine(user.Name + " Online but is a Bot");
								skipClient = true;
								break;
							}
						}

						if (skipClient)
						{
							continue;
						}

						//Console.WriteLine(user.Name + " Online and Not a Bot UID:"+ fulluser.Value.Uid);
						userIDS.Add(fulluser.Value.Uid.ToString());
						count++;
						doUpdate = true;
						if (connected && !containsUserID) { countToday++; }
					}

				}
				if (doUpdate && oldCount != count && count <= countToday) { UpdateChannelName(); }
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			isChecking = false;
		}

		private async Task ResetCountPeriodically()
		{
			while (true)
			{
				await Task.Delay(resetInterval);
				ResetCount();
			}
		}

		private void ResetCount()
		{
			lock (countLock)
			{
				count = 0;
				countToday = 0;
				lastResetTime = DateTime.UtcNow;
			}
		}

		private string GetChannelName()
		{
			lock (countLock)
			{
				return $"[cspacer73]Heute {count} von {countToday} online";
				//return $"[cspacer73] {count} users online today";
			}
		}

		private async void UpdateChannelName()
		{
			try
			{
				string newChanDis = $"Zuletzt zurückgesetzt: {lastResetTime}";
				ChannelId channelId = new ChannelId(channelToUpdateId);

				//await tsFullClient.ChannelEdit(currentChannel, codec: defaultCodec, codecQuality: defaultCodecQuality);
				//Console.WriteLine("Channel name: " + GetChannelName());
				await tsFullClient.ChannelEdit(channelId, name: GetChannelName(), description: newChanDis);
				//$"UPDATE channels SET channel_name='{GetChannelName()}' WHERE channel_id={channelToUpdateId}";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to update channel name: {ex.Message}");
			}
		}

		public void Dispose()
		{
			tsFullClient.OnClientEnterView -= OnUserConnected;
			tsFullClient.OnClientLeftView -= OnUserDisconnected;
		}
	}
}

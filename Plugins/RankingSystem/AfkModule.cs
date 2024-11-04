using System.Threading.Tasks;
using System;
using TS3AudioBot;
using TSLib.Full.Book;
using TSLib.Full;
using TSLib;
using System.Collections.Generic;

namespace RankingSystem
{
	internal class AfkModule
    {
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Constants constants = new Constants();

		private int AFKNotice;

		public AfkModule(Ts3Client ts3Client, TsFullClient tsFullClient, Connection serverView)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFullClient;
			this.serverView = serverView;
		}

		public void StartAfkModule()
		{
			AFKNotice = constants.AFKTime - 1;
			Console.WriteLine("AFK Module initialized!");
		}

		public async Task UserIdleCheck()
		{
			Console.WriteLine("Checking Idle Users...");
			try
			{
				var allConnectedClients = serverView.Clients;
				foreach (var client in allConnectedClients)
				{
					//Check if user is in excludet group
					bool skipCurrentClient = false;
					foreach (var sg in constants.BotGroups)
					{
						ServerGroupId newSG = (ServerGroupId)sg;
						if (client.Value.ServerGroups.Contains(newSG))
						{
							//Console.WriteLine("Skipping Bot");
							skipCurrentClient = true;
							break;
						}
					}
					if (skipCurrentClient)
						continue;

					if (GetUserCountFromChannelId(client.Value.Channel) > 1	&& client.Value.Channel != constants.AfkChannel)
					{
						var ci = await ts3Client.GetClientInfoById(client.Value.Id);

						if (ci.ClientIdleTime != null)
						{
							TimeSpan ts = ci.ClientIdleTime;
							double totalMinutesAfk = Math.Round(ts.TotalMinutes);

							//Console.WriteLine($"Checking user {ci.Name}, AFK Time: {ci.ClientIdleTime} converted: {totalMinutesAfk} and ");

							if (totalMinutesAfk >= AFKNotice)
							{
								await tsFullClient.SendPrivateMessage("\n[b][color=red]🚨 !Attention! 🚨[/color][/b]\n[color=green] Please note, if there's no activity in the next minute, you'll be moved to the AFK channel. Feel free to talk or type to stay active![/color]\nYou can also type anything in this chat window to stay active.", client.Value.Id);
								//Console.WriteLine($"Sendind Warning to user {ci.Name} to AFK while afkNoticeTime is: {totalMinutesAfk}>={AFKNotice}");
							}
							if (totalMinutesAfk >= constants.AFKTime)
							{
								// move to afk Channel
								await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
								await tsFullClient.PokeClient("Moved to AFK: No activity for 1 hour. Join when ready!", client.Value.Id);
								//Console.WriteLine($"Sendind User to Channel AFK {ci.Name} to AFK Channel as AFKTime is: {totalMinutesAfk}>={constants.AFKTime}");
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}
		}

		private async Task SendServerMessage(ClientId clientId, string message)
		{
			const int maxChunkSize = 95; // Poke Message Legth

			// Split the message into chunks
			var chunks = SplitIntoChunks(message, maxChunkSize);

			// Send each chunk through PokeClient
			foreach (var chunk in chunks)
			{
				await tsFullClient.PokeClient(chunk, clientId);
			}
		}

		private List<string> SplitIntoChunks(string message, int chunkSize)
		{
			List<string> chunks = new List<string>();
			for (int i = 0; i < message.Length; i += chunkSize)
			{
				int endIndex = Math.Min(i + chunkSize, message.Length);
				chunks.Add(message.Substring(i, endIndex - i));
			}
			return chunks;
		}

		private List<int> GetUsersInChannel(int ChanId)
		{
			var list = new List<int>();
			string usrId;
			foreach (var client in serverView.Clients)
			{
				if (((int)client.Value.Channel.Value) == ChanId)
				{
					usrId = client.Value.Id.ToString();
					list.Add(Convert.ToInt32(usrId));
				}
			}
			return list;
		}

		private int GetUserCountFromChannelId(ChannelId ChanId)
		{
			int count = 0;

			var allConnectedClients = serverView.Clients;

			foreach (var client in allConnectedClients)
			{
				ServerGroupId serverGroupId = (ServerGroupId)11;
				if (!client.Value.ServerGroups.Contains(serverGroupId))
				{
					if (client.Value.Channel == ChanId)
					{
						count++;
					}
				}
			}
			return count;
		}

		public async Task TestTask()
		{
			await Task.Delay(1000);
			Console.WriteLine("Tested AFK done!");
		}
	}
}

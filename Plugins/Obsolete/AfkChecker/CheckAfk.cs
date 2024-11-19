using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Collections.Generic;


namespace AfkChecker
{
	public class CheckAfk : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private bool TimerOn = false;
		private int AFKTime = 60; // in Minutes
		private int AFKNotice;

		// Your dependencies will be injected into the constructor of your class.
		public CheckAfk(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		// The Initialize method will be called when all modules were successfully injected.
		public async void Initialize()
		{
			AFKNotice = AFKTime - 1;
			Console.WriteLine("Starting AFK Service - AFK Time: " + AFKTime + " AFK Notice: " + AFKNotice);

			TimerOn = true;
			await StartTimerLoop();
			//UserIdleCheck();

		}

		private async Task StartTimerLoop()
		{
			while (TimerOn)
			{
				try
				{
					await UserIdleCheck();
				}
				catch (Exception ex) 
				{
					Console.WriteLine($"Error in AFK Loop User Check! Error:{ex.Message}");
				}
				//Console.WriteLine("Executed method - " + DateTime.Now);
				await Task.Delay(30000); // Wait for 10 seconds before executing again
			}
		}

		public async Task UserIdleCheck()
		{
			Console.WriteLine("Checking Idle Users...");
			try
			{
				var allConnectedClients = serverView.Clients;
				foreach (var client in allConnectedClients)
				{
					// If is in Bot Server Group Ignore
					ServerGroupId serverGroupId = (ServerGroupId)11;
					ServerGroupId serverGroupId2 = (ServerGroupId)69;
					if (!client.Value.ServerGroups.Contains(serverGroupId)
						&& !client.Value.ServerGroups.Contains(serverGroupId2)
						&& GetUserCountFromChannelId(client.Value.Channel) > 1
						&& client.Value.Channel.ToString() != "18")
					{
						var ci = await ts3Client.GetClientInfoById(client.Value.Id);

						if (ci.ClientIdleTime != null)
						{
							TimeSpan ts = ci.ClientIdleTime;
							double totalMinutesAfk = Math.Round(ts.TotalMinutes);

							if (totalMinutesAfk >= AFKNotice)
							{
								await tsFullClient.SendPrivateMessage("\n[b][color=red]🚨 !Attention! 🚨[/color][/b]\n[color=green] Please note, if there's no activity in the next minute, you'll be moved to the AFK channel. Feel free to talk or type to stay active![/color]\nYou can also type anything in this chat window to stay active.", client.Value.Id);
							}
							if (totalMinutesAfk >= AFKTime)
							{
								//Console.WriteLine("Sending " + client.Value.Name + " to AFK Channel");
								// move to afk Channel
								await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
								await tsFullClient.PokeClient("Moved to AFK: No activity for 1 hour. Join when ready!", client.Value.Id);
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

		private static List<string> SplitIntoChunks(string message, int chunkSize)
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

		public void Dispose()
		{
			TimerOn = false;
		}
	}

}

using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Collections.Generic;
using System.Linq;
using Heijden.DNS;
//using System.Threading;

namespace AfkChecker
{
	public class CheckAfk : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		private bool TimerOn = false;
		private int AFKTime = 60; // in Minutes
		private int AmongAFKTime = 120; // in Minutes
		private double AbmahnungSpamTime = 5; // in Minutes
		private List<AbmahnUser> AbmahnClientIds = new List<AbmahnUser>();
		private int AFKNotice; // in Minutes
		private int AmongAFKNotice; // in Minutes
									// replace with the IDs of the excluded groups
									//ivate int BotServerGroup = 11;
									//private static Timer timer;

		//public static Dictionary<ClientId, Client> Clients { get; private set; }

		// Your dependencies will be injected into the constructor of your class.
		public CheckAfk(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		// The Initialize method will be called when all modules were successfully injected.
		public void Initialize()
		{
			// (ChannelId)18
			AFKNotice = AFKTime - 1;
			AmongAFKNotice = AmongAFKTime - 1;
			Console.WriteLine("Starting AFK Service - AFK Time: " + AFKTime + " AFK Notice: " + AFKNotice);

			//timer2 = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
			//Timer timer = new Timer();
			//timer.Interval = 10000; // 10 seconds
			//timer.Elapsed += Timer_Elapsed;
			//timer.Start();
			//playManager.AfterResourceStarted += Start;
			//playManager.PlaybackStopped += Stop;
			//setChannelCommander();

			//tsFullClient.OnClientEnterView += OnUserConnected;
			//tsFullClient.OnClientLeftView += OnUserDisconnected;
			//tsFullClient.OnClientMoved += OnUserMoved;
			TimerOn = true;
			StartTimerLoop();
			//UserIdleCheck();

		}

		private async void StartTimerLoop()
		{
			while (TimerOn)
			{
				UserIdleCheck();
				//Console.WriteLine("Executed method - " + DateTime.Now);
				await Task.Delay(30000); // Wait for 10 seconds before executing again
			}
		}

		public async void UserIdleCheck()
		{
			try
			{
				var allConnectedClients = serverView.Clients;
				foreach (var client in allConnectedClients)
				{
					// If is in Bot Server Group Ignore
					ServerGroupId serverGroupId = (ServerGroupId)11;
					ServerGroupId serverGroupId2 = (ServerGroupId)69;
					if (client.Value.ServerGroups.Contains(serverGroupId) || client.Value.ServerGroups.Contains(serverGroupId2))
					{
						//Console.WriteLine("User is Bot: " + client.Value.Name);
					}
					else
					{
						CheckIfAbmahnung(client);
						// If Channel has more that 1 user
						if (GetUserCountFromChannelId(client.Value.Channel) > 1)
						{
							//Console.WriteLine("Usercount in channel: " + GetUserCountFromChannelId(client.Value.Channel));

							// Check if already in AFK Channel
							if (client.Value.Channel.ToString() == "18")
							{
								//Console.WriteLine("Already in Afk channel");
							}
							else
							{
								var ci = await ts3Client.GetClientInfoById(client.Value.Id);
								//var ci = await tsFullClient.ClientInfo(client.Value.Id);
								if (ci.ClientIdleTime == null)
								{
									//Console.WriteLine("Full Client Empty " + client.Value.Name);
								}
								else
								{
									TimeSpan ts = ci.ClientIdleTime;
									double totalMinutesAfk = Math.Round(ts.TotalMinutes);
									//Console.WriteLine("Real Client and not in AFK Channel " + client.Value.Name + " Timespan: " + ts.ToString());
									// Check if in Among us channel
									//if (client.Value.Channel.ToString() == "460" || client.Value.Channel.ToString() == "461")
									//{
									// In Among us channels AmongAFKTime
									//if (ts.Minutes >= AmongAFKNotice && ts.Minutes <= AmongAFKTime)
									//{
									//Console.WriteLine(client.Value.Name + " is " + ts.Minutes + " minutes Idle");
									//Console.WriteLine("Sending "+ client.Value.Name+" a Notice");
									// move to afk Channel
									//ts3Client.
									//await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
									//await tsFullClient.SendPrivateMessage("[b][color=red]🚨 Attention![/color][/b] Please note, if there's no activity in the next minute, you'll be moved to the AFK channel. Feel free to type or type to stay active!", client.Value.Id);

									//client.Value.Channel = (ChannelId)18;
									//}
									//if (ts.Minutes >= AmongAFKTime)
									//{
									//Console.WriteLine(client.Value.Name + " is " + ts.Minutes + " minutes Idle");
									//Console.WriteLine("Sending " + client.Value.Name + " to AFK Channel");
									// move to afk Channel
									//ts3Client.
									//await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
									//await tsFullClient.PokeClient("Moved to AFK: No activity for 2 hours. Join when ready!", client.Value.Id);

									//client.Value.Channel = (ChannelId)18;
									//}
									//Console.WriteLine("Already in Afk channel");
									//}
									//else
									//{
									//Console.WriteLine(client.Value.Name + " is " + totalMinutesAfk + " minutes Idle - Notice at: " + AFKNotice+" and AFK at: "+ AFKTime);
									if (totalMinutesAfk >= AFKNotice && totalMinutesAfk <= AFKTime)
									{
										//Console.WriteLine(client.Value.Name + " is " + totalMinutesAfk + " minutes Idle");
										//Console.WriteLine("Sending "+ client.Value.Name+" a Notice");
										// move to afk Channel
										//ts3Client.
										//await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
										await tsFullClient.SendPrivateMessage("[b][color=red]🚨 Attention![/color][/b] Please note, if there's no activity in the next minute, you'll be moved to the AFK channel. Feel free to talk or type to stay active!", client.Value.Id);

										//client.Value.Channel = (ChannelId)18;
									}
									if (totalMinutesAfk >= AFKTime)
									{
										//Console.WriteLine(client.Value.Name + " is " + totalMinutesAfk + " minutes Idle");
										Console.WriteLine("Sending " + client.Value.Name + " to AFK Channel");
										// move to afk Channel
										//ts3Client.
										await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
										await tsFullClient.PokeClient("Moved to AFK: No activity for 1 hour. Join when ready!", client.Value.Id);

										//client.Value.Channel = (ChannelId)18;
										//}
									}
								}
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

		private async void CheckIfAbmahnung(KeyValuePair<ClientId, Client> client)
		{
			ServerGroupId serverGroupId = (ServerGroupId)111; // Abmahnung 1
			ServerGroupId serverGroupId1 = (ServerGroupId)112; // Abmahnung 2
			ServerGroupId serverGroupId2 = (ServerGroupId)113; // Abmahnung 3
			if (client.Value.ServerGroups.Contains(serverGroupId) || client.Value.ServerGroups.Contains(serverGroupId2) || client.Value.ServerGroups.Contains(serverGroupId1))
			{
				//user has abmahnung
				//Console.WriteLine($"User {client.Value.Name} has Abmahnung");

				var existingUser = AbmahnClientIds.FirstOrDefault(u => u.ClientId == client.Value.Id);

				if (existingUser == null)
				{
					//Console.WriteLine($"User {client.Value.Name} Added to list");

					int abmahnTimes = 0;

					if (client.Value.ServerGroups.Contains(serverGroupId))
					{
						abmahnTimes = 5;
					}
					else if (client.Value.ServerGroups.Contains(serverGroupId1))
					{
						abmahnTimes = 15;
					}
					else if (client.Value.ServerGroups.Contains(serverGroupId2))
					{
						abmahnTimes = 30;
					}
					else
					{
						abmahnTimes = 5;
					}

					abmahnTimes--;

					AbmahnUser au = new AbmahnUser
					{
						ClientId = client.Value.Id,
						LastAbmahnung = DateTime.Now,
						MessagesRecived = abmahnTimes
					};
					AbmahnClientIds.Add(au);

					ChooseAndSendAbmahnung(client.Value.Id);
				}
				else
				{
					var fiveMinutesAgo = DateTime.Now.Subtract(TimeSpan.FromMinutes(AbmahnungSpamTime));
					if (existingUser.LastAbmahnung < fiveMinutesAgo)
					{
						// Do something here
						ChooseAndSendAbmahnung(client.Value.Id);

						existingUser.LastAbmahnung = DateTime.Now;
						if (existingUser.MessagesRecived <= 0)
						{
							// Finished Sending - Remove Server Group - Final Message
							AbmahnClientIds.Remove(existingUser);

							var userGroups = await tsFullClient.ServerGroupsByClientDbId(client.Value.DatabaseId);
							//bool hasGroup = false;
							// Store server group IDs in a collection for easier management
							var serverGroupIdsToRemove = new[] { serverGroupId1, serverGroupId2, serverGroupId };

							// Retrieve user groups and check for membership efficiently
							var hasGroup = userGroups.Value.Any(g => serverGroupIdsToRemove.Contains(g.ServerGroupId));

							if (hasGroup)
							{
								// Optimized: Remove only the first matching group to avoid redundant calls
								await tsFullClient.ServerGroupDelClient(serverGroupIdsToRemove.First(g => userGroups.Value.Any(ug => ug.ServerGroupId == g)), client.Value.DatabaseId);

								SendServerMessage(client.Value.Id, "Abmahnung entfernt, das nächste mal uffpasse!");
							}

							//SendServerMessage(client.Value.Id, "Abmahnung entfernt, das nächste mal uffpasse!");
						}
						else
						{
							existingUser.MessagesRecived--;
						}
						//Console.WriteLine($"User {client.Value.Name} Abmahnung send {existingUser.MessagesRecived} times");

					}
					//else
					//{
					//	//Console.WriteLine($"User {client.Value.Name} waiting {existingUser.LastAbmahnung.Minute}min. recived {existingUser.MessagesRecived} times");
					//}
				}
			}
		}

		private void ChooseAndSendAbmahnung(ClientId clientId)
		{
			// Choose a random quote
			var randomQuote = Abmahnungen.Quotes[new Random().Next(Abmahnungen.Quotes.Count)];

			// Send the chosen quote
			SendServerMessage(clientId, randomQuote);
		}

		private async void SendServerMessage(ClientId clientId, string message)
		{
			const int maxChunkSize = 95; // Poke Message Legth

			// Split the message into chunks
			var chunks = SplitIntoChunks(message, maxChunkSize);

			// Send each chunk through PokeClient
			foreach (var chunk in chunks)
			{
				await tsFullClient.PokeClient(chunk, clientId);
			}

			//await tsFullClient.PokeClient("Du hast eine abmahnung", clientId);
		}

		private static List<string> SplitIntoChunks(string message, int chunkSize)
		{
			List<string> chunks = new List<string>();
			for (int i = 0; i < message.Length; i += chunkSize)
			{
				// Adjust end index to avoid exceeding the chunk size
				int endIndex = Math.Min(i + chunkSize, message.Length);
				chunks.Add(message.Substring(i, endIndex - i));
			}
			return chunks;
		}

		private async Task<List<int>> GetUsersInChannel(int ChanId)
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
				//Console.WriteLine(client.Value.Id + " is in channel: " + client.Value.Channel.Value.ToString());
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
				if (client.Value.ServerGroups.Contains(serverGroupId))
				{
					//Console.WriteLine("User is Bot: " + client.Value.Name);
				}
				else
				{
					if (client.Value.Channel == ChanId)
					{
						count++;
					}
				}

				//Console.WriteLine(client.Value.Id + " is in channel: " + client.Value.Channel.Value.ToString());
			}

			return count;
		}


		private void OnUserConnected(object sender, IEnumerable<ClientEnterView> clients)
		{

		}

		private void OnUserDisconnected(object sender, IEnumerable<ClientLeftView> clients)
		{

		}

		private void OnUserMoved(object sender, IEnumerable<ClientMoved> clients)
		{
			foreach (var client in clients)
			{
				Console.WriteLine("Client " + client.InvokerName + " moved to: " + client.TargetChannelId);
			}
		}

		[Command("afk")]
		public static string CommandAFK()
		{
			string outputString = "empty";
			//foreach (var client in Clients)
			//{
			//	outputString = outputString + "Client: " + client.Value.Name + " Idle time: " + client.Value.ConnectionData.IdleTime;
			//	Console.WriteLine("Client: " + client.Value.Name + " Idle time: " + client.Value.ConnectionData.IdleTime);
			//}

			return outputString;
		}

		public void Dispose()
		{
			TimerOn = false;
			// Don't forget to unregister everything you have subscribed to,
			// otherwise your plugin will remain in a zombie state
			//playManager.AfterResourceStarted -= Start;
			//playManager.PlaybackStopped -= Stop;
		}
	}

	public class AbmahnUser
	{
		public ClientId ClientId { get; set; }
		public DateTime LastAbmahnung { get; set; }

		public int MessagesRecived { get; set; }
	}

	public class Abmahnungen
	{
		public static readonly List<string> Quotes = new List<string>()
	{
		"Bitte beachte die Regeln!",
		"Dein Verhalten ist nicht akzeptabel.",
		"Denke an die Konsequenzen deines Handelns.",
		"Es wird erwartet, dass du dich an die Regeln hältst.",
		"Nimm dir Zeit, um über dein Verhalten nachzudenken.",
		"Dein Verhalten stört den Spielfluss und beeinträchtigt die Spielerfahrung anderer.",
		"Es ist wichtig, dass alle Spieler respektvoll miteinander umgehen.",
		"Bitte lies dir die Regeln noch einmal durch und befolge sie in Zukunft.",
		"Bei wiederholtem Fehlverhalten kann es zu weiteren Konsequenzen kommen.",
		"Vielleicht solltest du eine Pause machen und etwas frische Luft schnappen.",
		"Erwachsen werden ist nicht immer einfach, aber es gehört dazu."
	};
	}

}

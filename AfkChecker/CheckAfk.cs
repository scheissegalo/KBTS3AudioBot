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
//using System.Threading;

namespace AfkChecker
{
	public class CheckAfk : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		private bool TimerOn = false;
		private int AFKTime = 30; // in Minutes
		private int AFKNotice; // in Minutes
		//ivate int BotServerGroup = 11;
		//private static Timer timer;

		//public static Dictionary<ClientId, Client> Clients { get; private set; }

		// Your dependencies will be injected into the constructor of your class.
		public CheckAfk(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		// The Initialize method will be called when all modules were successfully injected.
		public void Initialize()
		{
			AFKNotice = AFKTime - 1;
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
				//Console.WriteLine("Executing method at " + DateTime.Now);
				await Task.Delay(30000); // Wait for 10 seconds before executing again
			}
		}

		public async void UserIdleCheck()
		{
			var allConnectedClients = serverView.Clients;
			foreach (var client in allConnectedClients)
			{
				// If is in Bot Server Group Ignore
				ServerGroupId serverGroupId = (ServerGroupId)11;
				if (client.Value.ServerGroups.Contains(serverGroupId))
				{
					//Console.WriteLine("User is Bot: " + client.Value.Name);
				}
				else
				{
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
								//Console.WriteLine(client.Value.Name + " is " + ts.Minutes + " minutes Idle");
								if (ts.Minutes >= AFKNotice && ts.Minutes <= AFKTime)
								{
									Console.WriteLine(client.Value.Name + " is " + ts.Minutes + " minutes Idle");
									Console.WriteLine("Sending "+ client.Value.Name+" a Notice");
									// move to afk Channel
									//ts3Client.
									//await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
									await tsFullClient.SendPrivateMessage("[b][color=red]!!! Achtung !!![/color][/b] wenn du die nächste minute nichts sagst oder schreibst wirst du in den AFK Kanal gezogen", client.Value.Id);

									//client.Value.Channel = (ChannelId)18;
								}
								if (ts.Minutes >= AFKTime)
								{
									Console.WriteLine(client.Value.Name + " is " + ts.Minutes + " minutes Idle");
									Console.WriteLine("Sending "+ client.Value.Name+" to AFK Channel");
									// move to afk Channel
									//ts3Client.
									await tsFullClient.ClientMove(client.Value.Id, (ChannelId)18);
									await tsFullClient.PokeClient("Verschiebe dich in den AFK Kanal da du länger als 30 min. nichts gesagt hast", client.Value.Id);

									//client.Value.Channel = (ChannelId)18;
								}
							}
						}

					}
				}


			}
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
				Console.WriteLine("Client "+client.InvokerName+" moved to: "+ client.TargetChannelId);
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
}

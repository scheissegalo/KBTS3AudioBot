using System;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
	public class CoreModule : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		//private List<TSChannel> tSChannels = new List<TSChannel>();
		private List<ChannelUser> channelUsers = new List<ChannelUser>();
		// Create a dictionary to store channel IDs and their corresponding online time
		private Dictionary<ChannelId, TimeSpan> channelOnlineTimes = new Dictionary<ChannelId, TimeSpan>();
		private Dictionary<ChannelId, DateTime> channelLastChecks = new Dictionary<ChannelId, DateTime>();
		private List<TSChannel> cachedChannelList = new List<TSChannel>();
		private bool isChecking = false;

		public CoreModule(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, PlayManager playManager)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public async void Initialize()
		{
			tsFullClient.OnClientMoved += OnUserMoved;
			tsFullClient.OnClientEnterView += OnUserJoin;
			tsFullClient.OnClientLeftView += OnUserLeft;
			await GetAllUsers();
			await StartLoop();

			Console.WriteLine("Core Module Loaded");
		}

		private async void OnUserLeft(object sender, IEnumerable<ClientLeftView> e)
		{
			//Fix user not displayed by waiting 500 ms
			await Task.Delay(500);

			foreach (ClientLeftView view in e)
			{
				//if (view.Value.ClientType == ClientType.Full)
				//{
				//	view.
				//}
			}


		}
		private void OnUserJoin(object sender, IEnumerable<ClientEnterView> e)
		{
			foreach (ClientEnterView view in e)
			{
				//view.ClientId
			}
		}

		private void OnUserMoved(object sender, IEnumerable<ClientMoved> e)
		{
			if (!isChecking)
			{
				foreach (ClientMoved item in e)
				{
					ClientId clientId = item.ClientId;
					ChannelId channelId = item.TargetChannelId;
					DateTime joinTime = DateTime.Now; // Capture current UTC time

					// Find the user in the channelUsers list and update their information
					var userToUpdate = channelUsers.FirstOrDefault(user => user.ClientId == clientId);
					if (userToUpdate != null)
					{
						userToUpdate.ChannelId = channelId;
						userToUpdate.JoinTime = joinTime;
						Console.WriteLine($"Client {clientId} moved to channel {channelId}, Join time: {joinTime}");
					}
					else
					{
						Console.WriteLine($"User with ID {clientId} not found in the user list.");
					}
				}
			}
			else
			{
				Console.WriteLine("Unable to update User, checking locked");
			}
		}

		private async Task CheckUsers()
		{
			isChecking = true;
			//var channelList = await tsFullClient.ChannelList();

			if (cachedChannelList.Count > 0)
			{
				//TimeSpan totalOnlineTime = TimeSpan.Zero; // Initialize total online time to zero

				foreach (var channel in cachedChannelList)
				{
					// Check if there are online users in the channel
					if (channelUsers.Any(user => user.IsOnline && user.ChannelId == channel.ChannelId))
					{
						DateTime lastCheck;
						if (!channelLastChecks.TryGetValue(channel.ChannelId, out lastCheck))
						{
							lastCheck = DateTime.MinValue;
						}

						var elapsedTime = DateTime.Now - lastCheck;

						var currentUsers = channelUsers.Where(user => user.IsOnline && user.ChannelId == channel.ChannelId)
								 .Select(user => user.ClientId) // Select only client IDs if desired
								 .ToList(); // Convert results to a list

						if (elapsedTime > TimeSpan.Zero)
						{

							//Update channelOnlineTimes with safe addition
							if (channelOnlineTimes.TryGetValue(channel.ChannelId, out TimeSpan existingTime))
							{
								channelOnlineTimes[channel.ChannelId] = existingTime + (elapsedTime * currentUsers.Count());
								Console.WriteLine($"Channel {channel.ChannelId} Updated with time {elapsedTime}");
							}
							else
							{
								channelOnlineTimes.Add(channel.ChannelId, TimeSpan.Zero);
								Console.WriteLine($"Channel {channel.ChannelId} not updatet");
							}
						}

						channelLastChecks[channel.ChannelId] = DateTime.Now;
					}

					// Update user online time individually
					foreach (var user in channelUsers.Where(u => u.IsOnline && u.ChannelId == channel.ChannelId))
					{
						if (user.LastCheck != default(DateTime)) // Check if LastCheck is not null
						{
							// Calculate elapsed time since last check
							var elapsedTime = DateTime.Now - user.LastCheck;
							user.OnlineTime += elapsedTime;
						}

						user.LastCheck = DateTime.Now; // Update LastCheck with current time
					}
				}


			}
			ListAllUsers();
			isChecking = false;
		}

		private void ListAllUsers()
		{
			foreach (var user in channelUsers)
			{
				Console.WriteLine($"User {user.ClientId} is {user.OnlineTime.TotalSeconds} seconds online");
			}

			int i = 0;
			foreach (var cot in channelOnlineTimes)
			{
				i++;
				Console.WriteLine($"channel{i}  {cot.Key} total seconds {cot.Value.TotalSeconds}");
			}
		}

		private async Task GetChannelList()
		{
			cachedChannelList.Clear();
			var channelList = await tsFullClient.ChannelList();
			foreach (var channel in channelList.Value)
			{
				TSChannel newChannel = new TSChannel { ChannelId = channel.ChannelId, ChannelName = channel.Name };
				cachedChannelList.Add(newChannel);
			}
		}

		private async Task GetAllUsers()
		{
			var clientList = await tsFullClient.ClientList();
			channelUsers.Clear();
			foreach (var user in clientList.Value)
			{
				ChannelUser channelUser = new ChannelUser
				{
					ClientId = user.ClientId,
					ChannelId = user.ChannelId,
					IsOnline = true,
					JoinTime = DateTime.Now
				};

				channelUsers.Add(channelUser);
			}
			Console.WriteLine("User List Loaded");
			await GetChannelList();
		}



		public async Task UpdateChannelDescription(Dictionary<ChannelId, TimeSpan> topFiveChannels)
		{

			string formattedDescription = "";
			foreach (var channelEntry in topFiveChannels)
			{
				ChannelId channelId = channelEntry.Key;
				TimeSpan visitTime = channelEntry.Value;

				// Get channel name using TS3Server's GetChannelInfo method (or similar)
				string channelName = channelId.ToString();

				formattedDescription += $"{channelName}: {visitTime.ToString()}\n";
			}

			// Update description of the desired channel (replace targetChannelId with the appropriate ID)
			ChannelId updateChannelId = new ChannelId(455);
			//string newChannelName = bms.serverName + " - Offline";
			await tsFullClient.ChannelEdit(updateChannelId, name: formattedDescription);
		}

		public async Task StartLoop()
		{
			Console.WriteLine("Timer Started");
			while (true)
			{
				await Task.Delay(5000); // Add a 500ms delay before starting the method

				Console.WriteLine("Timer Run Thru");
				await CheckUsers();

			}

		}

		public void GetInfo()
		{

		}

		public void Dispose()
		{
			tsFullClient.OnClientMoved -= OnUserMoved;
			tsFullClient.OnClientMoved -= OnUserMoved;
			tsFullClient.OnClientEnterView -= OnUserJoin;
		}

	}

	//public class TSChannel
	//{
	//	public ChannelId ChannelId { get; set; }
	//	public List<ChannelUser> ClientIds { get; set; }
	//}

	public class ChannelUser
	{
		public ClientId ClientId { get; set; }
		public ChannelId ChannelId { get; set; }
		public TimeSpan OnlineTime { get; set; }
		public DateTime JoinTime { get; set; }
		public DateTime LeaveTime { get; set; }
		public DateTime LastCheck { get; set; }
		public bool IsOnline { get; set; }
	}

	public class TSChannel
	{
		public ChannelId ChannelId { get; set; }
		public string ChannelName { get; set; }
	}
}

using TS3AudioBot;
using TSLib.Full.Book;
using TSLib.Full;
using System.Threading.Tasks;
using System;
using TSLib;
using System.Collections.Generic;
using TSLib.Messages;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;

namespace RankingSystem
{
	internal class OnlineCounterModule
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Constants constants = new Constants();
		private Timer resetTimer;

		private bool isChecking = false;
		public uint count = 0;
		private uint countToday = 0;
		public DateTime lastResetTime = DateTime.MinValue;
		private DateTime lastResetDate;
		private List<string> userIDS = new List<string>();
		private List<string> userNames = new List<string>();

		private const string UserDataFile = "user_data.json"; // File path for saving user data

		public OnlineCounterModule(Ts3Client ts3Client, TsFullClient tsFullClient, Connection serverView)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFullClient;
			this.serverView = serverView;
		}

		public void StartOnlineCounterModule()
		{
			LoadLastResetDate();
			LoadUserData();
			lastResetTime = DateTime.UtcNow;

			tsFullClient.OnClientEnterView += onUserConnected;
			tsFullClient.OnClientLeftView += onUserDisconnected;

			//StartDailyResetTimer();
			Console.WriteLine("OnlineCounter Module initialized!");
		}

		private async void onUserDisconnected(object sender, IEnumerable<ClientLeftView> e) => await CheckOnlineUsers(false);
		private async void onUserConnected(object sender, IEnumerable<ClientEnterView> e) => await CheckOnlineUsers(true);

		// Method that runs every 2 minutes
		public async Task CheckForDailyReset()
		{
			DateTime now = DateTime.Now;

			if (lastResetDate.Date != now.Date && now.TimeOfDay >= constants.ResetTime && now.TimeOfDay < constants.ResetTime.Add(TimeSpan.FromMinutes(2)))
			{
				await PerformDailyReset();

				// Update last reset date and save it to file
				lastResetDate = now.Date;
				SaveLastResetDate();
			}
		}

		private void SaveLastResetDate()
		{
			System.IO.File.WriteAllText("lastResetDate.txt", lastResetDate.ToString("yyyy-MM-dd"));
		}

		private void LoadLastResetDate()
		{
			if (System.IO.File.Exists("lastResetDate.txt"))
			{
				string dateText = System.IO.File.ReadAllText("lastResetDate.txt");
				if (DateTime.TryParse(dateText, out DateTime savedDate))
				{
					lastResetDate = savedDate;
				}
				else
				{
					lastResetDate = DateTime.MinValue;
				}
			}
			else
			{
				lastResetDate = DateTime.MinValue;
			}
		}

		public async Task PerformDailyReset()
		{
			// Delete the user data file if it exists
			if (System.IO.File.Exists(UserDataFile))
			{
				System.IO.File.Delete(UserDataFile);
			}

			// Clear the lists
			userIDS.Clear();
			userNames.Clear();

			await CheckOnlineUsers(true);
			await ts3Client.SendServerMessage("[b][color=red]Online Counter Reset! everyday at 6:00 PM CET[/color][/b]");

			Console.WriteLine("User data reset at 6 AM.");
		}

		private async Task CheckOnlineUsers(bool connected)
		{
			if (isChecking) { return; }
			isChecking = true;

			await Task.Delay(500); // Add a 500ms delay before starting the method

			count = 0;
			foreach (var oneuser in serverView.Clients)
			{
				// Check if is full user
				if (oneuser.Value.ClientType == ClientType.Full)
				{
					//Check if user is in excludet group
					bool skipCurrentClient = false;
					foreach (var sg in constants.BotGroups)
					{
						ServerGroupId newSG = (ServerGroupId)sg;
						if (oneuser.Value.ServerGroups.Contains(newSG))
						{
							//Console.WriteLine("Skipping Bot");
							skipCurrentClient = true;
							break;
						}
					}
					// Skip processing this user and move to the next iteration
					if (skipCurrentClient)
						continue;
					// User is Fulluser and is not a Bot go on
					bool containsUserID = userIDS.Any(item => item == oneuser.Value.Uid.Value.ToString());
					count++;
					if (connected && !containsUserID)
					{
						countToday++;
						userNames.Add(oneuser.Value.Name);
						userIDS.Add(oneuser.Value.Uid.Value.ToString());
					}

				}
			}
			await UpdateChannelName();
			SaveUserData();
			isChecking = false;
		}

		// Method to save user data to a file
		private void SaveUserData()
		{
			var userData = new UserData
			{
				UserIds = userIDS,
				UserNames = userNames
			};

			var json = JsonConvert.SerializeObject(userData, Formatting.Indented);
			System.IO.File.WriteAllText(UserDataFile, json);
		}

		// Method to load user data from a file
		private void LoadUserData()
		{
			if (System.IO.File.Exists(UserDataFile))
			{
				var json = System.IO.File.ReadAllText(UserDataFile);
				var userData = JsonConvert.DeserializeObject<UserData>(json);

				if (userData != null)
				{
					userIDS = userData.UserIds;
					userNames = userData.UserNames;
				}
			}
		}

		private async Task UpdateChannelName()
		{
			string usernameList = "";
			try
			{
				if (userNames.Count <= 0)
				{
					// No usernames
					usernameList = "No user Online";
				}
				else
				{
					foreach (var user in userNames)
					{
						usernameList = usernameList + "- " + user + "\n";
					}
				}
				string newChanDis = $"Last Reset: {lastResetTime}\n\n[b]Userlist:[/b]\n{usernameList}";
				string newChanTop = $"Last Reset: {lastResetTime}";

				await tsFullClient.ChannelEdit(constants.onlineCountChannel, name: GetChannelName(), description: newChanDis, topic: newChanTop);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to update channel name: {ex.Message}");
			}
		}

		private string GetChannelName()
		{
			return $"[cspacer73]Today {count} of {userIDS.Count} online";
		}

		public async Task TestTask()
		{
			await Task.Delay(1000);
			Console.WriteLine("Tested Online Counter done!");
		}

		// Define a class for the user data
		private class UserData
		{
			public List<string> UserIds { get; set; } = new List<string>();
			public List<string> UserNames { get; set; } = new List<string>();
		}
	}
}

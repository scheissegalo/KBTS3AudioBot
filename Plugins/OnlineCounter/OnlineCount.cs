using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using TS3AudioBot.CommandSystem;

namespace OnlineCounter
{
	public class OnlineCount : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;

		public static OnlineCount Instance { get; private set; }

		private static readonly HttpClient httpClient = new HttpClient();

		private readonly ulong channelToUpdateId = 171; // replace with the ID of the channel to update
		private readonly TimeSpan resetInterval = TimeSpan.FromDays(1);
		//private readonly TimeSpan resetInterval = TimeSpan.FromMinutes(1);
		private readonly List<uint> excludedGroups = new List<uint> { 11, 47, 115 }; // replace with the IDs of the excluded groups
		public static string filePath = "badusernames.txt";
		private static readonly string logFilePath = "geolocation_log.txt"; // Path to your log file

		private Dictionary<string, List<DateTime>> userMoveTimestamps = new Dictionary<string, List<DateTime>>();
		private TimeSpan moveWindow = TimeSpan.FromSeconds(5); // The time window to check moves
		private int maxMoves = 3; // Maximum allowed moves within the time window
		private ChannelId adminChannel;
		ServerGroupId AdminGroup = (ServerGroupId)90;

		private List<string> userIDS = new List<string>();
		private List<string> userNames = new List<string>();
		private const string UserDataFile = "user_data.json"; // File path for saving user data

		private readonly object countLock = new object();
		private uint count = 0;
		private uint countToday = 0;
		private DateTime lastResetTime = DateTime.MinValue;
		private bool isChecking = false;
		//private string jsonFilePath = "data.json";
		private bool initialCheck = true;

		private Timer resetTimer;

		private List<UserStatistic> userStatistics = new List<UserStatistic>();

		public OnlineCount(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			Instance = this;
		}

		public async void Initialize()
		{
			if (System.IO.File.Exists("local.txt"))
			{
				//parentChannelId = (ChannelId)506; // Local setting
				adminChannel = (ChannelId)266;
			}
			else
			{
				//parentChannelId = (ChannelId)589; // Remote setting
				adminChannel = (ChannelId)266;
			}

			tsFullClient.OnClientEnterView += OnUserConnected;
			tsFullClient.OnClientLeftView += OnUserDisconnected;
			tsFullClient.OnClientUpdated += onUserChangedNickname;
			//tsFullClient.OnClientChannelGroupChanged += ClientChannelGroupChanged;
			tsFullClient.OnClientMoved += ClientMoved;
			tsFullClient.OnClientServerGroupAdded += ClientServerGroupAdded;
			tsFullClient.OnClientServerGroupRemoved += ClientServerGroupRemoved;
			tsFullClient.OnServerEdited += OnServerEdited;

			LoadUserData();

			//ResetCountPeriodically();
			lastResetTime = DateTime.UtcNow;
			//CheckOnlineUsers(true);
			await CheckOnlineUsersNeu(true);
			StartDailyResetTimer();
			await LogUserCountPeriodically();
		}

		private async Task LogUserCountPeriodically()
		{
			while (true)
			{
				if (initialCheck)
				{
					initialCheck = false;
				}
				else
				{
					userStatistics.Add(new UserStatistic
					{
						Timestamp = DateTime.UtcNow,
						UserCount = count
					});

					// Save to file for persistence
					SaveUserStatisticsToFile();
				}
				await Task.Delay(TimeSpan.FromMinutes(5));
			}
		}

		private void SaveUserStatisticsToFile()
		{
			List<UserStatistic> currentStatistics;

			// Read existing file data if the file exists
			if (System.IO.File.Exists("user_statistics.json"))
			{
				var existingData = System.IO.File.ReadAllText("user_statistics.json");
				currentStatistics = JsonConvert.DeserializeObject<List<UserStatistic>>(existingData) ?? new List<UserStatistic>();
			}
			else
			{
				currentStatistics = new List<UserStatistic>();
			}

			// Add the new entry
			currentStatistics.Add(new UserStatistic
			{
				Timestamp = DateTime.UtcNow,
				UserCount = count // or however you’re capturing the count
			});

			// Write back the entire updated list
			System.IO.File.WriteAllText("user_statistics.json", JsonConvert.SerializeObject(currentStatistics));
			Console.WriteLine("Statistics Recorded!");
		}


		private async void OnServerEdited(object sender, IEnumerable<ServerEdited> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				bool skipCurrentClient = false;
				foreach (var sg in excludedGroups)
				{
					ServerGroupId newSG = (ServerGroupId)sg;
					if (fullClient.ServerGroups.Contains(newSG))
					{
						//Console.WriteLine("Skipping Bot");
						skipCurrentClient = true;
						break;
					}

					if (skipCurrentClient)
						continue;

					await ReportAdmin($"{item.InvokerName} has modified the server!");
				}
			}
		}

		private async void ClientServerGroupRemoved(object sender, IEnumerable<ClientServerGroupRemoved> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				//var clientServerGroups = fullClient.ServerGroups;
				bool skipCurrentClient = false;
				foreach (var sg in excludedGroups)
				{
					ServerGroupId newSG = (ServerGroupId)sg;
					if (fullClient.ServerGroups.Contains(newSG))
					{
						//Console.WriteLine("Skipping Bot");
						skipCurrentClient = true;
						break;
					}
				}

				if (skipCurrentClient)
					continue;

				await ReportAdmin($"{item.InvokerName} has removed Server Group {item.ServerGroupId} from {item.Name}");
			}
		}
		private async void ClientServerGroupAdded(object sender, IEnumerable<ClientServerGroupAdded> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				//var clientServerGroups = fullClient.ServerGroups;
				bool skipCurrentClient = false;
				foreach (var sg in excludedGroups)
				{
					ServerGroupId newSG = (ServerGroupId)sg;
					if (fullClient.ServerGroups.Contains(newSG))
					{
						//Console.WriteLine("Skipping Bot");
						skipCurrentClient = true;
						break;
					}
				}

				if (skipCurrentClient)
					continue;

				await ReportAdmin($"{item.InvokerName} has added Server Group {item.ServerGroupId} to {item.Name}");				
			}
		}
		private async void ClientMoved(object sender, IEnumerable<ClientMoved> e)
		{
			foreach (var item in e)
			{
				string clientId = item.ClientId.Value.ToString();
				var client = await ts3Client.GetClientInfoById(item.ClientId);

				if (!userMoveTimestamps.ContainsKey(clientId))
				{
					userMoveTimestamps[clientId] = new List<DateTime>();
				}

				// Remove timestamps older than the move window
				userMoveTimestamps[clientId].RemoveAll(timestamp => DateTime.UtcNow - timestamp > moveWindow);

				// Check if the user can move
				if (userMoveTimestamps[clientId].Count >= maxMoves)
				{
					//Console.WriteLine($"Client {clientId} has exceeded the maximum number of moves within the time window.");
					await ReportAdmin($"[color=red]Client {client.Name} Wechselt sehr oft die Kanaele![/color]");
					continue; // Ignore the move if the limit is reached
				}

				// Record the new move timestamp
				userMoveTimestamps[clientId].Add(DateTime.UtcNow);

			}			
		}

		private async Task ReportAdmin(string message)
		{
			var AllClients = serverView.Clients;
			foreach (var item in AllClients)
			{
				var ServerGroups = item.Value.ServerGroups;
				foreach (var ServerGroup in ServerGroups)
				{
					if (ServerGroup == AdminGroup)
					{
						await ts3Client.SendMessage(message, item.Value.Id);
					}
				}
			}
		}

		private async void onUserChangedNickname(object sender, IEnumerable<ClientUpdated> e)
		{
			await CheckOnlineUsersNeu(true);
		}

		private async void OnUserConnected(object sender, IEnumerable<ClientEnterView> clients)
		{
			await CheckOnlineUsersNeu(true);
		}

		private async void OnUserDisconnected(object sender, IEnumerable<ClientLeftView> clients)
		{
			await CheckOnlineUsersNeu(false);
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
					//Console.WriteLine($"User {String.Join(", ",userData.UserNames)} added to the list: {String.Join(" ,", userIDS)}!");
					
				}
			}
		}

		private async Task CheckOnlineUsersNeu(bool connected)
		{
			if (isChecking) { return; }
			isChecking = true;
			//uint oldCount = count;

			await Task.Delay(500); // Add a 500ms delay before starting the method

			count = 0;
			//int testcount = 0;
			//bool skipCurrentClient = false;
			foreach (var oneuser in serverView.Clients)
			{
				if (CheckBadUsernames(oneuser.Value.Name))
				{
				    var cci = await tsFullClient.GetClientConnectionInfo(oneuser.Value.Id);
					//long insta = 1;
					await tsFullClient.KickClientFromServer(oneuser.Value.Id, "No DDoS, No Trolls, No Nazis and No Kevin you immature little prick, you neither. Please go back to Discord and stay there!");
					//await tsFullClient.SendServerMessage("User IP: "+cci.Value.Ip, insta);
					//string geolocation = await GetGeolocationAsync(cci.Value.Ip);
					//await tsFullClient.SendServerMessage("Location: " + geolocation, insta);
					//await tsFullClient.SendServerMessage("Possible DDoS, report the IP!", insta);
					//Console.WriteLine("Bad Username: " + oneuser.Value.Name);
				}
				// Check if is full user
				if (oneuser.Value.ClientType == ClientType.Full)
				{
					//Console.WriteLine("ID: " + oneuser.Value.Id.Value + " | Type: " + oneuser.Value.ClientType);

					//Check if user is in excludet group
					bool skipCurrentClient = false;
					foreach (var sg in excludedGroups)
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
						//testcount++;
						countToday++;
						userNames.Add(oneuser.Value.Name);
						userIDS.Add(oneuser.Value.Uid.Value.ToString());
						//Console.WriteLine("User Added: " + oneuser.Value.Name);
					}

				}
			}
			await UpdateChannelName();
			SaveUserData();
			//Console.WriteLine("Currently "+ count + " users online of "+ countToday + " today");
			isChecking = false;
		}


		public static async Task<string> GetGeolocationAsync(string ipAddress)
		{
			try
			{
				string apiUrl = $"http://ip-api.com/json/{ipAddress}";

				// Send a GET request to the API
				HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
				response.EnsureSuccessStatusCode();

				// Read the response content as a string
				string content = await response.Content.ReadAsStringAsync();

				// Parse the response JSON
				JObject json = JObject.Parse(content);

				// Extract relevant fields (country, region, city, etc.)
				string country = json["country"]?.ToString();
				string region = json["regionName"]?.ToString();
				string city = json["city"]?.ToString();
				string isp = json["isp"]?.ToString();

				// Create a formatted string with the geolocation details
				string logEntry = $"[{DateTime.Now}] IP: {ipAddress}, Country: {country}, Region: {region}, City: {city}, ISP: {isp}";

				// Log the entry to a file
				LogToFile(logEntry);

				// Return a formatted string with the geolocation details
				return logEntry;
			}
			catch (Exception ex)
			{
				string errorLog = $"[{DateTime.Now}] Error fetching geolocation for IP {ipAddress}: {ex.Message}";
				LogToFile(errorLog);
				return errorLog;
			}
		}

		// Method to log the geolocation data into a file
		private static void LogToFile(string logEntry)
		{
			try
			{
				// Append the log entry to the file
				System.IO.File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error writing to log file: {ex.Message}");
			}
		}


		public void StartDailyResetTimer()
		{
			// Calculate the time until the next 5 AM
			DateTime now = DateTime.Now;
			DateTime nextReset = new DateTime(now.Year, now.Month, now.Day, 5, 0, 0);
			if (now >= nextReset) // If it's already past 5 AM today, set it for tomorrow
			{
				nextReset = nextReset.AddDays(1);
			}

			TimeSpan timeToGo = nextReset - now;

			// Set the timer to trigger daily at 5 AM
			resetTimer = new Timer(PerformDailyReset, null, timeToGo, TimeSpan.FromDays(1));
		}

		[Command("resetonlinetime")]
		public static async Task<string> ResetTheOnlineTimer(ClientCall invoker)
		{
			if (Instance == null)
				return "Plugin not initialized.";

			await Instance.ManualResetOnlineTimes();

			return "Online Times Reset!";

		}

		private async void PerformDailyReset(object state)
		{
			// Delete the user data file if it exists
			if (System.IO.File.Exists(UserDataFile))
			{
				System.IO.File.Delete(UserDataFile);
			}

			// Clear the lists
			userIDS.Clear();
			userNames.Clear();

			await CheckOnlineUsersNeu(true);
			await ts3Client.SendServerMessage("[b][color=red]Online Counter Reset! everyday at 5:00 PM CET[/color][/b]");

			Console.WriteLine("User data reset at 5 AM.");
		}

		public async Task ManualResetOnlineTimes()
		{
			// Delete the user data file if it exists
			if (System.IO.File.Exists(UserDataFile))
			{
				System.IO.File.Delete(UserDataFile);
				Console.WriteLine("UserFile Deleted!");
			}

			// Clear the lists
			userIDS.Clear();
			userNames.Clear();

			await CheckOnlineUsersNeu(true);
			await ts3Client.SendServerMessage("[b][color=red]Online Counter Reset![/color][/b]");

		}

		private async Task ResetCountPeriodically()
		{
			while (true)
			{
				await Task.Delay(resetInterval);
				//Console.WriteLine("Resetting Online Counter!");
				await ResetCount();
			}
		}

		private async Task ResetCount()
		{
			count = 0;
			//countToday = 0;
			lastResetTime = DateTime.UtcNow;
			userNames.Clear();
			userIDS.Clear();
			//TSuserDB.DeleteAllData(jsonFilePath);
			//CheckOnlineUsers(true);
			await CheckOnlineUsersNeu(true);
			await ts3Client.SendServerMessage("[b][color=red]Online Counter Reset![/color][/b]");

		}

		private string GetChannelName()
		{
			lock (countLock)
			{
				return $"[cspacer73]Today {count} of {userIDS.Count} online";
				//return $"[cspacer73] {count} users online today";
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
				ChannelId channelId = new ChannelId(channelToUpdateId);

				await tsFullClient.ChannelEdit(channelId, name: GetChannelName(), description: newChanDis, topic: newChanTop);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to update channel name: {ex.Message}");
			}
		}


		public static bool CheckBadUsernames(string inputString, bool ignoreCase = true)
		{
			// Check if the file exists
			if (!System.IO.File.Exists(filePath))
			{
				throw new FileNotFoundException($"File not found: {filePath}");
			}

			// Read all lines from the file
			string[] lines = System.IO.File.ReadAllLines(filePath);

			// If we are ignoring case, convert both inputString and lines to lower case
			if (ignoreCase)
			{
				inputString = inputString.ToLower();
				lines = lines.Select(line => line.ToLower()).ToArray();
			}

			// Loop through each line in the file
			foreach (var line in lines)
			{
				if (line.Contains("*"))
				{
					// Treat the '*' as a wildcard, removing it for partial match
					string pattern = line.Replace("*", "");

					// If inputString contains the pattern, it's a bad username
					if (inputString.Contains(pattern))
					{
						return true;
					}
				}
				else
				{
					// Exact match case
					if (inputString == line)
					{
						return true;
					}
				}
			}

			// If no matches found
			return false;
		}

		public void Dispose()
		{
			tsFullClient.OnClientEnterView -= OnUserConnected;
			tsFullClient.OnClientLeftView -= OnUserDisconnected;
			tsFullClient.OnClientUpdated -= onUserChangedNickname;
			tsFullClient.OnClientMoved -= ClientMoved;
			tsFullClient.OnClientServerGroupAdded -= ClientServerGroupAdded;
			tsFullClient.OnClientServerGroupRemoved -= ClientServerGroupRemoved;
			tsFullClient.OnServerEdited -= OnServerEdited;
		}

		// Define a class for the user data
		private class UserData
		{
			public List<string> UserIds { get; set; } = new List<string>();
			public List<string> UserNames { get; set; } = new List<string>();
		}
	}

	public class UserStatistic
	{
		public DateTime Timestamp { get; set; }
		public uint UserCount { get; set; }
	}


}

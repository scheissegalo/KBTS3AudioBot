using System;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Globalization;

namespace GameServer
{
	public class Empyrion : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		private string empyrionServerApiKey = "fEyVHKHbjxIUkad33XJaQ1PJoWJuOUUNLck"; // Replace with your API key
		private string empyrionServerID = "59988"; // ID of the server
		private readonly ulong empyrionChannel = 470; // replace with the ID of the channel to update 470
		private readonly int UpdateInterval = 10; //min

		string apiKey = "no key";
		string serverId = "24273362"; // Replace with the actual server ID
		readonly ulong theFrontChannel = 509; // 509 - remote / 452 - local  replace with the ID of the channel to update

		string RustServerID = "25539753";
		readonly ulong RustChannel = 454;

		private static BattleMetricsServer TheFrontServer = new BattleMetricsServer();
		private static BattleMetricsServer RustServer = new BattleMetricsServer();

		// endpoints
		private string empyrionServerUrl;
		private string empyrionServerVotesUrl;

		public Empyrion(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}
		public void Initialize()
		{
			TheFrontServer.channelID = 509; // 452 Local / 509 Remote
			TheFrontServer.serverID = "24273362";
			TheFrontServer.serverName = "The Front";

			RustServer.channelID = 531; // 454 Local / 531 Remote
			RustServer.serverID = "25539753";
			RustServer.serverName = "Palworld";
			//GetVotes();
			//empyrionServerVotesUrl = "https://empyrion-servers.com/api/?object=servers&element=votes&key="+ empyrionServerApiKey+ "&format=json";
			//empyrionServerUrl = "https://empyrion-servers.com/api/?object=servers&element=detail&key="+empyrionServerApiKey;
			// Specify the path to your text file
			string filePath = "front.api_key.txt";

			try
			{
				// Read the API key from the text file
				apiKey = System.IO.File.ReadAllText(filePath);

				// Now you can use the apiKey as needed
				//Console.WriteLine("API Key: " + apiKey);
			}
			catch (IOException e)
			{
				Console.WriteLine("Error reading the file: " + e.Message);
			}
			//And start the Timer
			StartLoop();
			//GetVotes();
			//FetchPlayerData();
			//getCSV();
			//getUsersfromCSV();
			getServers();
		}

		private void getServers()
		{
			DLCSV();
			ProcessServerData(TheFrontServer, true);
			ProcessServerData(RustServer, false);
		}

		private async void StartLoop()
		{
			int update = UpdateInterval;
			while (true)
			{
				//Console.WriteLine($"Tick: Update:{update}");
				if (update <= 0)
				{
					// Timer end
					//FetchPlayerData();
					getServers();
					update = UpdateInterval;
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}


		static async void DLCSV()
		{
			//Console.WriteLine("Fetching accounts");
			string fileUrl = "http://195.20.227.239:9090/Accounts.csv";
			string destinationPath = "Accounts.csv";

			using (HttpClient client = new HttpClient())
			{
				try
				{
					// Download the file
					byte[] fileData = await client.GetByteArrayAsync(fileUrl);

					// Save the file to the local destination
					System.IO.File.WriteAllBytes(destinationPath, fileData);

					//Console.WriteLine("File downloaded successfully.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
				}
			}
		}

		static void getCSV()
		{
			string ftpServer = "176.57.160.16";
			string ftpUsername = "gpftp31274201209767533";
			string ftpPassword = "B0eBvAO0"; // Replace with your actual password
			int ftpPort = 33931;

			string remoteFilePath = "/ProjectWar/Saved/GameStates/Accounts/Accounts.csv";
			string localFileName = "Accounts.csv"; // The file name you want to save locally

			// Create FTP request
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer}:{ftpPort}{remoteFilePath}");
			request.Method = WebRequestMethods.Ftp.DownloadFile;
			request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

			try
			{
				// Get the FTP response
				using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
				{
					// Get the response stream
					using (Stream responseStream = response.GetResponseStream())
					{
						// Create a local file to save the downloaded content
						using (FileStream localFileStream = new FileStream(localFileName, FileMode.Create))
						{
							// Read and write the content
							byte[] buffer = new byte[1024];
							int bytesRead;
							while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								localFileStream.Write(buffer, 0, bytesRead);
							}
						}
					}
				}

				// Now, you can perform actions with the local file (Accounts.csv)
				// For example, you can read the content or manipulate the data as needed.

				//Console.WriteLine("File downloaded successfully.");
			}
			catch (WebException ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		static string getUsersfromCSV()
		{
			//Console.WriteLine("Extracting users from csv");
			string pluserlist = "[b]LAST CONNECTED USERS 24h:[/b]\n\n";
			// Read the contents of the file
			string filePath = "Accounts.csv"; // Update with your actual file path
			string[] lines = System.IO.File.ReadAllLines(filePath);

			// Define the date format used in the file
			string dateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

			// Get the current time
			DateTime currentTime = DateTime.UtcNow;

			// Create a list to store the results
			var results = lines
				.Select(line =>
				{
					// Split the line by commas
					string[] parts = line.Split(',');

					// Check if the line has enough parts and has a valid date
					if (parts.Length >= 6 && DateTime.TryParseExact(parts[4], dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime recordTime))
					{
						// Calculate the time difference
						TimeSpan timeDifference = currentTime - recordTime;

						// Check if the record is within the last 24 hours
						if (timeDifference.TotalHours <= 24)
						{
							// Extract the username, day of the week, and time
							string username = parts[2];
							string dayOfWeek = recordTime.ToString("dddd"); // Get the full day name
							string timeOfDay = recordTime.ToString("HH:mm:ss"); // Get the time

							return new { Username = username, DayAndTime = $"{dayOfWeek} {timeOfDay}" };
						}
					}

					return null;
				})
				.Where(result => result != null) // Filter out null results
				.OrderBy(result => result.DayAndTime) // Sort by day and time
				.ToList();

			//string resultString = string.Join("aggagg", results);

			// Print or use the results as needed
			foreach (var result in results)
			{
				//Console.WriteLine($"{result.Username} - {result.DayAndTime}");
				pluserlist += $"[b]{result.Username}[/b] - {result.DayAndTime} \n";
			}

			return pluserlist;
		}

		public async void FetchPlayerData()
		{
			DLCSV();
			//getCSV();
			//getUsersfromCSV();
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

				// Construct the API endpoint URL
				string endpoint = $"https://api.battlemetrics.com/servers/{serverId}?include=player";

				try
				{
					HttpResponseMessage response = await client.GetAsync(endpoint);

					if (response.IsSuccessStatusCode)
					{
						string content = await response.Content.ReadAsStringAsync();
						if (!string.IsNullOrWhiteSpace(content))
						{
							// Parse the JSON response
							var serverInfo = JsonConvert.DeserializeObject<ServerInfoResponse>(content);

							// Extract current players and max players
							int currentPlayers = serverInfo.data.attributes.players;
							int maxPlayers = serverInfo.data.attributes.maxPlayers;
							int rank = serverInfo.data.attributes.rank;

							//string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}"; getUsersfromCSV()
							ChannelId channelId = new ChannelId(theFrontChannel);
							string newChannelName = "The Front - Players: (" + currentPlayers + "/" + maxPlayers + ") | Rank: " + rank;
							await tsFullClient.ChannelEdit(channelId, name: newChannelName, description: getUsersfromCSV());
							//await tsFullClient.ChannelEdit(channelId, name: newChannelName);

							//Console.WriteLine($"New Channel Name: {newChannelName} Description: {getUsersfromCSV()}");

							//Console.WriteLine(content); // Process the player data as needed
						}
						else
						{
							Console.WriteLine("No player data available.");
						}
					}
					else
					{
						Console.WriteLine($"Failed to retrieve player information. Status code: {response.StatusCode}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
				}
			}
		}

		private async void GetVotes()
		{
			using (var client = new WebClient())
			{
				string NewChannelName = "";
				string userVotesList = "";

				var responseVotes = client.DownloadString(empyrionServerUrl);
				dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
				int serverRank = dataVotes.rank;
				int serverVotes = dataVotes.votes;
				int players = dataVotes.players;
				int maxplayers = dataVotes.maxplayers;
				NewChannelName = "Player: " + players + "/" + maxplayers + " | Rank: " + serverRank + " | Votes: " + serverVotes;
				//Console.WriteLine("Server Rank: {0}, Votes: {1}", serverRank, serverVotes);

				var response = client.DownloadString(empyrionServerVotesUrl);
				dynamic data = JsonConvert.DeserializeObject(response);
				var voters = data.votes;

				foreach (var voter in voters)
				{
					string nickname = voter.nickname;
					int votes = voter.claimed;
					//Console.WriteLine("Nickname: {0}, Votes: {1}", nickname, votes);
					userVotesList = userVotesList + nickname + " = " + votes + "\n";
				}

				userVotesList = userVotesList + "\n\n[url=https://empyrion-servers.com/server/" + empyrionServerID + "/vote/]Vote for US[/url]";
				//Console.WriteLine(userVotesList);
				string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
				ChannelId channelId = new ChannelId(empyrionChannel);
				await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);

			}
		}

		private async void ProcessServerData(BattleMetricsServer bms, bool csv)
		{
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

				// Construct the API endpoint URL
				string endpoint = $"https://api.battlemetrics.com/servers/{bms.serverID}?include=player";

				try
				{
					HttpResponseMessage response = await client.GetAsync(endpoint);

					if (response.IsSuccessStatusCode)
					{
						string content = await response.Content.ReadAsStringAsync();
						if (!string.IsNullOrWhiteSpace(content))
						{
							//Console.WriteLine(content);
							// Parse the JSON response
							var serverInfo = JsonConvert.DeserializeObject<ServerInfoResponse>(content);

							// Extract current players and max players
							int currentPlayers = serverInfo.data.attributes.players;
							int maxPlayers = serverInfo.data.attributes.maxPlayers;
							int rank = serverInfo.data.attributes.rank;
							string status = serverInfo.data.attributes.status;

							//string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}"; getUsersfromCSV()
							ChannelId channelId = new ChannelId(bms.channelID);
							//string newChannelName = bms.serverName + " - Players: (" + currentPlayers + "/" + maxPlayers + ") | Rank: " + rank + " - " + status;
							string newChannelName = $"{bms.serverName} - Players: ({currentPlayers} / {maxPlayers}) | {status}";
							if (csv)
							{
								await tsFullClient.ChannelEdit(channelId, name: newChannelName, description: getUsersfromCSV());
							}
							else
							{
								await tsFullClient.ChannelEdit(channelId, name: newChannelName);
							}

							//await tsFullClient.ChannelEdit(channelId, name: newChannelName);

							//Console.WriteLine($"New Channel Name: {newChannelName}");

							//Console.WriteLine(content); // Process the player data as needed
						}
						else
						{
							Console.WriteLine("No player data available.");
						}
					}
					else
					{
						Console.WriteLine($"Failed to retrieve player information. Status code: {response.StatusCode}");
						//string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}"; getUsersfromCSV()
						ChannelId channelId = new ChannelId(bms.channelID);
						string newChannelName = bms.serverName + " - Offline";
						await tsFullClient.ChannelEdit(channelId, name: newChannelName);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
					ChannelId channelId = new ChannelId(bms.channelID);
					string newChannelName = bms.serverName + " - Offline";
					await tsFullClient.ChannelEdit(channelId, name: newChannelName);
				}
			}

		}

		public void Dispose()
		{

		}
	}

	// Define classes to represent the JSON response
	public class ServerInfoResponse
	{
		public Data data { get; set; }
	}

	public class Data
	{
		public Attributes attributes { get; set; }
	}

	public class Attributes
	{
		public int players { get; set; }
		public int maxPlayers { get; set; }
		public string status { get; set; }
		public int rank { get; set; }
	}

	public class BattleMetricsServer
	{
		public string serverID { get; set; }
		public string serverName { get; set; }
		public ulong channelID { get; set; }
		public string channelName { get; set; }
	}
}

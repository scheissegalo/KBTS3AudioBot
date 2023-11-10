using System;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

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
		private readonly ulong empyrionChannel = 470; // replace with the ID of the channel to update
		private readonly int UpdateInterval = 10; //min

		string apiKey = "no key";
		string serverId = "24206105"; // Replace with the actual server ID
		readonly ulong theFrontChannel = 509; // 509 - remote / 452 - local  replace with the ID of the channel to update

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
			FetchPlayerData();
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
					FetchPlayerData();
					update = UpdateInterval;
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}

		public async void FetchPlayerData()
		{
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

							//string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
							ChannelId channelId = new ChannelId(theFrontChannel);
							string newChannelName = "The Front - Players: (" + currentPlayers + "/" + maxPlayers+") | Rank: "+ rank;
							await tsFullClient.ChannelEdit(channelId, name: newChannelName);

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
				NewChannelName = "Player: "+ players + "/"+ maxplayers + " | Rank: " + serverRank + " | Votes: " + serverVotes;
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

		public int rank { get; set; }
	}
}

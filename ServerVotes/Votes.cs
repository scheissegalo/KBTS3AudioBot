using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting.Server;
using TS3AudioBot.Audio;

namespace ServerVotes
{
	public class Votes : IBotPlugin
	{
		private TsFullClient tsFullClient;

		private ulong ServerVotesChannel = 466; // 449 local | 466 Remote | replace with the ID of the channel to update
		private ulong ServerEmopyrionChannel = 1545; // 449 local | 1545 Remote | replace with the ID of the channel to update

		private static readonly HttpClient httpClient = new HttpClient();
		//EmpyrionQuery empyrionQuery = new EmpyrionQuery();
		bool looping = true;

		public Votes(TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.tsFullClient = tsFull;
		}

		public async void Initialize()
		{
			Console.WriteLine("initializing Vote Script");

			await getVotes();
			//await GetEmpyrionInfos();

			//string serverIP = "152.53.64.213";
			//int serverPort = 2457; // Default query port

			//await SteamQuery.QueryServer();

			await StartLoop();
		}

		private async Task StartLoop()
		{
			Console.WriteLine("Loop Started");
			while (looping)
			{
				try
				{
					await getVotes();
					//await GetEmpyrionInfos();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error in Display Crypto Loop. Error: {ex.Message}");
				}
				//Console.WriteLine($"Next check: {DateTime.Now.AddMinutes(1)}");
				await Task.Delay(60000 * 3); // 60000 1 min
			}
		}

		//private async Task GetEmpyrionInfos()
		//{
		//	//Console.WriteLine($"Awaiting Server response");
		//	var serverData = await empyrionQuery.GetPlayers();
		//	//Console.WriteLine($"Server response recieved");


		//	string NewChannelName = $"Empyrion: {serverData.ConnectedPlayers.Count}/24";
		//	string newChanDis = $"[size=12]Players:[/size]\n";
		//	//Console.WriteLine($"Editing Channel {NewChannelName} | {newChanDis}");

		//	newChanDis += $"Online: \n";

		//	foreach (var player in serverData.GlobalOnlinePlayers)
		//	{
		//		newChanDis += $"- {player.Name} ({player.Faction})\n";
		//	}

		//	newChanDis += $"\nAll Players on Server: \n";

		//	foreach (var player in serverData.GlobalPlayers)
		//	{
		//		newChanDis += $"- {player.Name} ({player.Faction})\n";
		//	}
		//	newChanDis += $"\n\nPlay with us just search for North-industries in the server browser.";
		//	ChannelId channelId = new ChannelId(ServerEmopyrionChannel);
		//	try
		//	{
		//		var result = await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);
		//		//Console.WriteLine($"Edited Channel");
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine($"Error: {ex.Message} | {ex.InnerException}");
		//	}

		//	//Console.WriteLine("Connected Players:");
		//	//foreach (var player in serverData.ConnectedPlayers)
		//	//{
		//	//	Console.WriteLine($"ID: {player.Id}, Name: {player.Name}, Playfield: {player.PlayfieldName}, IP: {player.IPAddress}");
		//	//}

		//	//Console.WriteLine("Global Online Players:");
		//	//foreach (var player in serverData.GlobalOnlinePlayers)
		//	//{
		//	//	Console.WriteLine($"ID: {player.Id}, Name: {player.Name}, Faction: {player.Faction}, Role: {player.Role}");
		//	//}

		//	//Console.WriteLine("Global Players:");
		//	//foreach (var player in serverData.GlobalPlayers)
		//	//{
		//	//	Console.WriteLine($"ID: {player.Id}, Name: {player.Name}, Faction: {player.Faction}, Role: {player.Role}, Online Time: {player.OnlineTime}");
		//	//}
		//}

		private async Task getVotes()
		{
			//Console.WriteLine("Fetching votes");
			string apiUrl = "https://my-ts.org/api/server";
			string apiKey = "2|MT5eXJBCTDWOVRMpgk3ThBRsyimVhmdBQR0tqn3jf13ecb69";

			try
			{
				var serverData = await GetServerData(apiUrl, apiKey);
				//Console.WriteLine("Fetching serverdata");

				if (serverData != null)
				{
					foreach (var server in serverData.Data)
					{
						//Console.WriteLine($"Rank: {server.Rank:F2}, Votes: {server.Votes}");
						string NewChannelName = $"[cspacer14]Server Rank: {server.Rank} | Votes: {server.Votes}";
						string newChanDis = $"[URL=https://my-ts.org/view/1]Vote for us[/URL]";
						ChannelId channelId = new ChannelId(ServerVotesChannel);
						try
						{
							var result = await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);
							//Console.WriteLine($"Edited Channel");
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Error: {ex.Message} | {ex.InnerException}");
						}
						

					}
				}
				else
				{
					Console.WriteLine("Empty response!");

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}

		}

		static async Task<ServerResponse> GetServerData(string url, string apiKey)
		{
			using HttpClient client = new HttpClient();

			// Add Authorization header
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

			// Send GET request
			HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode(); // Throw if not successful

			// Read JSON response
			string json = await response.Content.ReadAsStringAsync();

			// Deserialize using Newtonsoft.Json
			return JsonConvert.DeserializeObject<ServerResponse>(json);
		}

		public void Dispose()
		{
			looping = false;
		}

	}

	// Root model for deserialization
	public class ServerResponse
	{
		public string Message { get; set; }
		public Server[] Data { get; set; }
	}

	// Server model
	public class Server
	{
		public int Id { get; set; }
		public int Active { get; set; }
		public string Name { get; set; }
		public string Hostname { get; set; }
		public int Users { get; set; }
		public int MaxUsers { get; set; }
		public string LastScanned { get; set; }
		public string ServerId { get; set; }
		public string UpdatedAt { get; set; }
		public double Rank { get; set; }
		public double Fullrank { get; set; }
		public int Votes { get; set; }
	}
}

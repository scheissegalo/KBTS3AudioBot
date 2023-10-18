using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System;
using Heijden.DNS;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSLib.Messages;

namespace Cryptoz
{
	public class DisplayCrypto : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		// endpoints
		private string BTC = "https://north-industries.com/getcry.php?cry=BTC";
		private readonly ulong BTCchannel = 267; // replace with the ID of the channel to update

		private string ETH = "https://north-industries.com/getcry.php?cry=ETH";
		private readonly ulong ETHchannel = 268; // replace with the ID of the channel to update

		private string Gold = "https://north-industries.com/getcry.php?cry=GOLD";
		private readonly ulong GOLDchannel = 486; // replace with the ID of the channel to update

		private string Silver = "https://north-industries.com/getcry.php?cry=SILVER";
		private readonly ulong SILVERchannel = 488; // replace with the ID of the channel to update

		private readonly ulong ServerVotesChannel = 466; // 449 local | 466 Remote | replace with the ID of the channel to update

		private readonly int UpdateInterval = 30; //min
												  //private readonly int GoldUpdateInterval = 480; //min
		ServerGroupInfo groupA = new ServerGroupInfo
		{
			VotesCount = 1,
			ServerGroup = (ServerGroupId)132 // Group A
		};

		ServerGroupInfo groupB = new ServerGroupInfo
		{
			VotesCount = 5,
			ServerGroup = (ServerGroupId)133 // Group A
		};

		ServerGroupInfo groupC = new ServerGroupInfo
		{
			VotesCount = 10,
			ServerGroup = (ServerGroupId)134 // Group A
		};

		ServerGroupInfo groupD = new ServerGroupInfo
		{
			VotesCount = 20,
			ServerGroup = (ServerGroupId)135 // Group A
		};

		public DisplayCrypto(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
			//DisplayCrypto displayCrypto = new DisplayCrypto(playManager, ts3Client, serverView, tsFull);
		}

		public void Initialize()
		{
			StartLoop();
			//StartGoldLoop();
			//GetVotes();
			//GetVotesAndGroups();
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
					GetBTC();
					GetETH();
					GetGold();
					GetSilver();
					GetVotes();
					GetVotesAndGroups();
					update = UpdateInterval;
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}

		private async void StartGoldLoop()
		{
			//int update = GoldUpdateInterval;
			while (true)
			{
				//Console.WriteLine($"Tick: Update:{update}");
				//if (update <= 0)
				//{
					// Timer end
					//GetBTC();
					//GetETH();
					//GetGold();
					//GetVotes();
					//update = GoldUpdateInterval;
				//}

				//update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}

		private async void GetVotesAndGroups()
		{
			//Console.WriteLine("Scanning Voters!");
			// Step 2: Retrieve Data from the API
			string apiUrl = "https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json&rank=steamid";

			using (HttpClient client = new HttpClient())
			{
				HttpResponseMessage response = await client.GetAsync(apiUrl);
				//Console.WriteLine("Calling API!");
				if (response.IsSuccessStatusCode)
				{
					//Console.WriteLine("Success!");
					string json = await response.Content.ReadAsStringAsync();

					// Step 3: Parse JSON Response
					var data = JsonConvert.DeserializeObject<ApiResponse>(json);

					// Step 4: Use SQLite to store and retrieve data
					using (var connection = new SQLiteConnection("Data Source=steam_ids.db"))
					{
						//Console.WriteLine("Connected to db!");
						connection.Open();

						// Step 6: Create the database and sample records if it doesn't exist
						CreateDatabaseIfNotExists(connection);

						foreach (var voter in data.voters)
						{
							string steamId = voter.steamid;
							//Console.WriteLine("Voter: "+ voter.nickname + " | SteamID: "+voter.steamid);

							// Query the database to get TeamSpeak ID based on Steam ID
							string teamspeakId = GetTeamSpeakIdFromSQLite(connection, steamId);

							if (!string.IsNullOrEmpty(teamspeakId))
							{
								// Step 5: Set Server Groups
								// Check if a client with the specified UID exists in serverView.Clients.
								bool isClientOnline = serverView.Clients.Any(client => client.Value.Uid.Value.ToString() == teamspeakId);
								if (isClientOnline)
								{
									//Console.WriteLine("User Online");
									int votes = int.Parse(voter.votes);
									SetServerGroupBasedOnVotes(teamspeakId, votes, groupA, groupB, groupC, groupD);
								}
								else
								{
									Console.WriteLine("Client not online!");
								}
							}
							else
							{
								Console.WriteLine("TS ID is Empty: "+ teamspeakId);
							}
						}
					}
				}
			}

		}

		//serverView.Clients

		static void CreateDatabaseIfNotExists(SQLiteConnection connection)
		{
			// Create the database and the table if it doesn't exist
			if (!System.IO.File.Exists("steam_ids.db"))
			{
				connection.Open();

				using (var cmd = new SQLiteCommand(
					"CREATE TABLE IF NOT EXISTS SteamIds (steam_id TEXT PRIMARY KEY, teamspeak_id TEXT);",
					connection))
				{
					cmd.ExecuteNonQuery();
				}

				// Add two sample records to the table
				using (var cmd = new SQLiteCommand(
					"INSERT INTO SteamIds (steam_id, teamspeak_id) VALUES (@steamId1, @teamspeakId1), (@steamId2, @teamspeakId2);",
					connection))
				{
					cmd.Parameters.AddWithValue("@steamId1", "76561198081498574");
					cmd.Parameters.AddWithValue("@teamspeakId1", "Ft9UID/8hr19myWjp13VyZvKFGE=");
					cmd.Parameters.AddWithValue("@steamId2", "76561197986483360");
					cmd.Parameters.AddWithValue("@teamspeakId2", "KFm1RQyU83K07PzLEMXQ463ESLk=");
					cmd.ExecuteNonQuery();
				}
			}
		}

		static string GetTeamSpeakIdFromSQLite(SQLiteConnection connection, string steamId)
		{
			// Implement logic to get TeamSpeak ID from SQLite database
			using (var cmd = new SQLiteCommand("SELECT teamspeak_id FROM SteamIds WHERE steam_id = @steamId", connection))
			{
				cmd.Parameters.AddWithValue("@steamId", steamId);
				object result = cmd.ExecuteScalar();
				if (result != null)
				{
					return result.ToString();
				}
			}

			return null;
		}

		private async void SetServerGroupBasedOnVotes(string teamspeakId, int votes, ServerGroupInfo groupA, ServerGroupInfo groupB, ServerGroupInfo groupC, ServerGroupInfo groupD)
		{
			
			ServerGroupId selectedGroup = (ServerGroupId)133; // Default group if no condition matches
															  // Define an array or list with the group IDs you want to check (groupA, groupB, groupC, and groupD).
			ServerGroupId[] groupIdsToCheck = { groupA.ServerGroup, groupB.ServerGroup, groupC.ServerGroup, groupD.ServerGroup };

			// Use FirstOrDefault to find a client with the specified UID.
			var user = serverView.Clients.FirstOrDefault(client => client.Value.Uid.Value.ToString() == teamspeakId);

			if (user.Value != null)
			{
				if (votes >= groupD.VotesCount)
				{
					selectedGroup = groupD.ServerGroup;
				}
				else if (votes >= groupC.VotesCount)
				{
					selectedGroup = groupC.ServerGroup;
				}
				else if (votes >= groupB.VotesCount)
				{
					selectedGroup = groupB.ServerGroup;
				}
				else if (votes >= groupA.VotesCount)
				{
					selectedGroup = groupA.ServerGroup;
				}
				Uid uid = new Uid(teamspeakId);

				ClientDbId userDBID = await ts3Client.GetClientDbIdByUid(uid);
				//ClientDbInfo ts3Clienta = await ts3Client.GetDbClientByDbId(userDBID);
				//ts3Clienta.
				var userGroups = await tsFullClient.ServerGroupsByClientDbId(userDBID);

				// Check if the user has any of the specified groups.
				bool hasAnyGroup = userGroups.Value.Any(g => groupIdsToCheck.Contains(g.ServerGroupId));

				// Check if the selected group is already in the user's group list.
				bool hasSelectedGroup = userGroups.Value.Any(g => g.ServerGroupId == selectedGroup);

				if (!hasSelectedGroup) // client does not have the current selected group
				{
					if (hasAnyGroup) // If already has any other vote group - remove then
					{
						foreach (var group in userGroups.Value)
						{
							if (groupIdsToCheck.Contains(group.ServerGroupId))
							{
								// If the user has any of the specified groups, remove it.
								await tsFullClient.ServerGroupDelClient(group.ServerGroupId, userDBID);
							}
						}
						// If the user has any of the specified groups, remove it.
						//await tsFullClient.ServerGroupDelClient(selectedGroup, userDBID);
					}
					// If the user has any of the specified groups, and the selected group is not in the list, remove it.
					//await tsFullClient.ServerGroupDelClient(selectedGroup, userDBID);
					await tsFullClient.ServerGroupAddClient(selectedGroup, userDBID);
					Console.WriteLine($"Setting server group for TeamSpeak ID {teamspeakId} to '{selectedGroup}'");
				}
				else
				{
					Console.WriteLine($"Group already set: TeamSpeak ID {teamspeakId} to '{selectedGroup}'");
				}

				//var hasGroup = userGroups.Value.Any(g => g.ServerGroupId == selectedGroup);
				//if (hasGroup)
				//{
				//Console.WriteLine("Group removed");
				// if user has Old Group Remove it
				//await tsFullClient.ServerGroupDelClient(selectedGroup, userDBID);
				//}


				
				//user.Value.ServerGroups.Add(selectedGroup);

				//Console.WriteLine($"Setting server group for TeamSpeak ID {teamspeakId} to '{selectedGroup}'");

			}
		}


		private async void GetVotes()
		{
			using (var client = new WebClient())
			{
				string NewChannelName = "";
				string userVotesList = "";
				var responseVotes = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=detail&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA");
				dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
				int serverRank = dataVotes.rank;
				int serverVotes = dataVotes.votes;
				NewChannelName = "[cspacer1231]Server Rank: "+ serverRank +" | Votes: "+ serverVotes;
				//Console.WriteLine("Server Rank: {0}, Votes: {1}", serverRank, serverVotes);

				var response = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json");
				dynamic data = JsonConvert.DeserializeObject(response);
				var voters = data.voters;

				foreach (var voter in voters)
				{
					string nickname = voter.nickname;
					int votes = voter.votes;
					//Console.WriteLine("Nickname: {0}, Votes: {1}", nickname, votes);
					userVotesList = userVotesList + nickname + " = "+ votes+"\n";
				}

				userVotesList = userVotesList + "\n\n[url=https://teamspeak-servers.org/server/12137/vote/]Vote for US[/url]";
				//Console.WriteLine(userVotesList);
				string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
				ChannelId channelId = new ChannelId(ServerVotesChannel);
				await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);

			}

		}

		private async void GetBTC()
		{
			ChannelId channelId = new ChannelId(BTCchannel);
			HttpClient client = new HttpClient();

			string btcData = await client.GetStringAsync(BTC);

			await tsFullClient.ChannelEdit(channelId, name: btcData+" USD");
		}


		private async void GetETH()
		{
			ChannelId channelId = new ChannelId(ETHchannel);
			HttpClient client = new HttpClient();

			string ethData = await client.GetStringAsync(ETH);

			await tsFullClient.ChannelEdit(channelId, name: ethData + " USD");
		}

		private async void GetGold()
		{
			ChannelId channelId = new ChannelId(GOLDchannel);
			HttpClient client = new HttpClient();

			string goldData = await client.GetStringAsync(Gold);
			//string goldData = "0";

			await tsFullClient.ChannelEdit(channelId, name: goldData + " USD");
		}

		private async void GetSilver()
		{
			ChannelId channelId = new ChannelId(SILVERchannel);
			HttpClient client = new HttpClient();

			string silverData = await client.GetStringAsync(Silver);
			//string silverData = "0";

			await tsFullClient.ChannelEdit(channelId, name: silverData + " USD");
		}



		public void Dispose()
		{

		}
	}

	public class ApiResponse
	{
		public string name { get; set; }
		public string address { get; set; }
		public string port { get; set; }
		public string month { get; set; }
		public List<Voter> voters { get; set; }
	}

	public class Voter
	{
		public string steamid { get; set; }
		public string votes { get; set; }
		public string nickname { get; set; }
	}

	public class ServerGroupInfo
	{
		public int VotesCount { get; set; }
		public ServerGroupId ServerGroup { get; set; }
	}
}

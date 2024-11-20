using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using TS3AudioBot.CommandSystem;

namespace Cryptoz
{
	public class DisplayCrypto : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		//test

		bool checkVotes = true;
		bool enabledDebugs = false;

		// endpoints
		private string BTC = "https://north-industries.com/getcry.php?cry=BTC";
		private readonly ulong BTCchannel = 267; // replace with the ID of the channel to update

		private string ETH = "https://north-industries.com/getcry.php?cry=ETH";
		private readonly ulong ETHchannel = 268; // replace with the ID of the channel to update

		private string Gold = "https://north-industries.com/getcry.php?cry=GOLD";
		private readonly ulong GOLDchannel = 486; // replace with the ID of the channel to update

		private string Silver = "https://north-industries.com/getcry.php?cry=SILVER";
		private readonly ulong SILVERchannel = 488; // replace with the ID of the channel to update

		private ulong ServerVotesChannel = 466; // 449 local | 466 Remote | replace with the ID of the channel to update

		private readonly int UpdateInterval = 30; //min

		private ulong CryptoChannel = 232;
		//private readonly int GoldUpdateInterval = 480; //min
		ServerGroupInfo groupA = new ServerGroupInfo
		{
			VotesCount = 1,
			ServerGroup = (ServerGroupId)132 // 133 local | 132 Remote Group A
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

		private static readonly HttpClient httpClient = new HttpClient();

		public DisplayCrypto(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		[Command("steam")]
		public static string CommandGreet(InvokerData invoker, string steamid)
		{
			string response = AddOrUpdateUserInDatabase(invoker.ClientUid.Value, steamid);
			return "[b][color=red]This TeamSpeak ID (" + invoker.ClientUid.Value + ") is now added to the given SteamID (" + steamid + ").[/color][/b] - Status: " + response;
		}

		[Command("votes")]
		public async Task<string> CommandToggleVotes(InvokerData invoker, string command)
		{
			if (command == "on")
			{
				checkVotes = true;
				return "[b][color=green]Votes are now enabled![/color][/b]";
			}
			else if (command == "off")
			{
				checkVotes = false;
				return "[b][color=red]Votes are now disabled![/color][/b]";
			}
			else if (command == "fetch")
			{
				checkVotes = true;
				await GetVotesAndGroups();
				return "[b][color=green]Votes are now enabled and fetched![/color][/b]";
			}
			else if (command == "debug")
			{
				if (enabledDebugs)
				{
					enabledDebugs = false;
					return "[b][color=red]Debugs are now disabled![/color][/b]";
				}
				else
				{
					enabledDebugs = true;
					return "[b][color=green]Debugs are now enabled![/color][/b]";
				}
				//return "[b][color=green]Votes are now enabled and fetched![/color][/b]";
			}
			else
			{
				return "[b][color=red]Invalid command! Valid vote commads: on, off, fetch or debug[/color][/b]";
			}
			//string response = AddOrUpdateUserInDatabase(invoker.ClientUid.Value, steamid); enabledDebugs
			//return "[b][color=red]This TeamSpeak ID (" + invoker.ClientUid.Value + ") is now added to the given SteamID (" + steamid + ").[/color][/b] - Status: " + response;
		}

		public async void Initialize()
		{
			string fileName = ".local.txt";

			if (System.IO.File.Exists(fileName))
			{
				// Develop version running, changing variables
				ServerVotesChannel = 449;
				groupA.ServerGroup = (ServerGroupId)133;
				groupB.ServerGroup = (ServerGroupId)134;
				groupC.ServerGroup = (ServerGroupId)135;
				groupD.ServerGroup = (ServerGroupId)136;
			}

			await SetCryptoChannelInfo();
			await StartLoop();
			await GetVotesAndGroups();

		}

		private async Task StartLoop()
		{
			int update = UpdateInterval;
			while (true)
			{
				try
				{


					//Console.WriteLine($"Tick: Update:{update}");
					if (update <= 0)
					{
						// Timer end
						await GetBTC();
						await GetETH();
						await GetGold();
						await GetSilver();
						await GetVotes();
						await GetVotesAndGroups();
						await SetCryptoChannelInfo();
						update = UpdateInterval;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error in Display Crypto Loop. Error: {ex.Message}");
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}

		private async Task SetCryptoChannelInfo()
		{
			DateTime now = DateTime.Now;
			string formattedTime = now.ToString("HH:mm");

			Console.WriteLine("Changing Channel");
			string newChanDis = $"[b]Complete List: [/b]";
			string newChannelName = "Crypto & Metal - Last Update " + formattedTime;
			ChannelId channelId = new ChannelId(CryptoChannel);
			await tsFullClient.ChannelEdit(channelId, name: newChannelName, description: newChanDis);
		}

		private async Task SendSteamMessage(string message)
		{
			//Console.WriteLine("Sending Message!");
			if (enabledDebugs)
			{
				await tsFullClient.SendChannelMessage("Debug: " + message);
			}
		}

		private async Task GetVotesAndGroups()
		{
			if (!checkVotes)
			{
				Console.WriteLine("Votes Disabled, skipping!");
				return;
			}
			Console.WriteLine("Scanning Voters!");
			// Step 2: Retrieve Data from the API
			string apiUrl = "https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json&rank=steamid";
			List<String> voterList = new List<String>();

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
							await SendSteamMessage("Voter: " + voter.nickname + " | SteamID: " + voter.steamid);

							// Query the database to get TeamSpeak ID based on Steam ID
							string teamspeakId = GetTeamSpeakIdFromSQLite(connection, steamId);

							if (!string.IsNullOrEmpty(teamspeakId))
							{
								voterList.Add(teamspeakId);
								// Step 5: Set Server Groups
								// Check if a client with the specified UID exists in serverView.Clients.
								bool isClientOnline = serverView.Clients.Any(client => client.Value.Uid.Value.ToString() == teamspeakId);
								if (isClientOnline)
								{
									await SendSteamMessage("User Online");
									//Console.WriteLine("User Online");
									Uid uid = new Uid(teamspeakId);
									int votes = int.Parse(voter.votes);

									ClientDbId udbid = await ts3Client.GetClientDbIdByUid(uid);
									await SetServerGroupBasedOnVotes(teamspeakId, votes, udbid, groupA, groupB, groupC, groupD);
								}
								else
								{
									//Console.WriteLine("Client not online!");
									await SendSteamMessage("User not Online");
								}
							}
							else
							{
								//Console.WriteLine("TS ID is Empty: "+ teamspeakId);
								await SendSteamMessage("TS ID is Empty: " + teamspeakId);
							}
						}

					}
				}
			}

			await RemoveVotesFromUsersNotInList(voterList);
		}


		private async Task RemoveVotesFromUsersNotInList(List<String> UsersInList)
		{
			ServerGroupId[] groupIdsToCheck = { groupA.ServerGroup, groupB.ServerGroup, groupC.ServerGroup, groupD.ServerGroup };
			await SendSteamMessage("Removing users not in list");
			foreach (var usr in serverView.Clients)
			{
				if (UsersInList.Contains(usr.Value.Uid.Value.ToString()))
				{
					// The user ID is in the list; skip this iteration.
					await SendSteamMessage("The user ID is in the list");
					continue;
				}

				var userGroups = await tsFullClient.ServerGroupsByClientDbId(usr.Value.DatabaseId);
				bool hasAnyGroup = userGroups.Value.Any(g => groupIdsToCheck.Contains(g.ServerGroupId));

				if (hasAnyGroup) // If already has any other vote group - remove then
				{
					foreach (var group in userGroups.Value)
					{
						if (groupIdsToCheck.Contains(group.ServerGroupId))
						{
							// If the user has any of the specified groups, remove it.
							await SendSteamMessage("remove group with id: " + group.ServerGroupId);
							await tsFullClient.ServerGroupDelClient(group.ServerGroupId, usr.Value.DatabaseId);
						}
					}
				}
			}
		}

		static string AddOrUpdateUserInDatabase(string TSID, string SteamID)
		{
			string response = "no response";

			string databaseFolderPath = "Data Source=steam_ids.db;Upgrade=true;";
			using (var connection = new SQLiteConnection(databaseFolderPath))
			{
				connection.Open();

				// Check if the user with the given Steam ID already exists
				using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM SteamIds WHERE steam_id = @steamId;", connection))
				{
					checkCmd.Parameters.AddWithValue("@steamId", SteamID);
					int count = Convert.ToInt32(checkCmd.ExecuteScalar());

					if (count > 0)
					{
						// User with Steam ID exists, update the TeamSpeak ID
						using (var updateCmd = new SQLiteCommand("UPDATE SteamIds SET teamspeak_id = @teamspeakId WHERE steam_id = @steamId;", connection))
						{
							updateCmd.Parameters.AddWithValue("@steamId", SteamID);
							updateCmd.Parameters.AddWithValue("@teamspeakId", TSID);
							updateCmd.ExecuteNonQuery();
							response = "user updated";
						}
					}
					else
					{
						// User with Steam ID doesn't exist, add a new record
						using (var insertCmd = new SQLiteCommand("INSERT INTO SteamIds (steam_id, teamspeak_id) VALUES (@steamId, @teamspeakId);", connection))
						{
							insertCmd.Parameters.AddWithValue("@steamId", SteamID);
							insertCmd.Parameters.AddWithValue("@teamspeakId", TSID);
							insertCmd.ExecuteNonQuery();
							response = "New user added";
						}
					}
				}
			}

			return response;
		}


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

		private async Task SetServerGroupBasedOnVotes(string teamspeakId, int votes, ClientDbId userDBID, ServerGroupInfo groupA, ServerGroupInfo groupB, ServerGroupInfo groupC, ServerGroupInfo groupD)
		{

			ServerGroupId selectedGroup = groupA.ServerGroup; // Default group if no condition matches
			ServerGroupId[] groupIdsToCheck = { groupA.ServerGroup, groupB.ServerGroup, groupC.ServerGroup, groupD.ServerGroup };

			// Use FirstOrDefault to find a client with the specified UID.
			var user = serverView.Clients.FirstOrDefault(client => client.Value.Uid.Value.ToString() == teamspeakId);

			if (user.Value != null)
			{

				var userGroups = await tsFullClient.ServerGroupsByClientDbId(userDBID);

				// Check if the user has any of the specified groups.
				bool hasAnyGroup = userGroups.Value.Any(g => groupIdsToCheck.Contains(g.ServerGroupId));

				// Check if the selected group is already in the user's group list.
				bool hasSelectedGroup = userGroups.Value.Any(g => g.ServerGroupId == selectedGroup);

				// Use Group based on number of votes specified in the on top
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

				//Console.WriteLine("Selected Group: " + selectedGroup.ToString() + " VotesCount: "+ votes);
				await SendSteamMessage(teamspeakId + " Selected Group: " + selectedGroup.ToString() + " VotesCount: " + votes);

				if (!userGroups.Value.Any(g => g.ServerGroupId == selectedGroup)) // client does not have the current selected group
				{
					//Console.WriteLine("Group not added");
					await SendSteamMessage(teamspeakId + " Group not added");
					if (hasAnyGroup) // If already has any other vote group - remove then
					{
						//Console.WriteLine("Does have another vote group, remove it");
						await SendSteamMessage(teamspeakId + " Does have another vote group, remove it");
						foreach (var group in userGroups.Value)
						{
							//Console.WriteLine("Iterate: "+ group.Name);
							await SendSteamMessage(teamspeakId + " Iterate: " + group.Name);
							if (groupIdsToCheck.Contains(group.ServerGroupId))
							{
								//Console.WriteLine("Group found and removing: " + group.Name);
								await SendSteamMessage(teamspeakId + " Group found and removing: " + group.Name);
								// If the user has any of the specified groups, remove it.
								await tsFullClient.ServerGroupDelClient(group.ServerGroupId, userDBID);
							}
							//Console.WriteLine(group.Name.ToString());
							await SendSteamMessage(teamspeakId + " " + group.Name.ToString());
						}
					}
					// Add to Group
					await tsFullClient.ServerGroupAddClient(selectedGroup, userDBID);
					//Console.WriteLine($"Setting server group for TeamSpeak ID {teamspeakId} to '{selectedGroup}' with DBid: '{userDBID}' votes: {votes}");
					await SendSteamMessage(teamspeakId + " Setting server group to: " + selectedGroup.ToString() + " with DBid: " + userDBID + " votes: " + votes);
				}
				else
				{
					//Console.WriteLine($"Group already set: TeamSpeak ID {teamspeakId} to '{selectedGroup}' with DBid: '{userDBID}' votes: {votes}");
					await SendSteamMessage(teamspeakId + " Group already set: " + selectedGroup.ToString() + " with DBid: " + userDBID + " votes: " + votes);
				}
			}
		}


		private async Task GetVotes()
		{
			//using (var client = new WebClient())
			//{
			//	string NewChannelName = "";
			//	string userVotesList = "";
			//	var responseVotes = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=detail&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA");
			//	dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
			//	int serverRank = dataVotes.rank;
			//	int serverVotes = dataVotes.votes;
			//	NewChannelName = "[cspacer1231]Server Rank: " + serverRank + " | Votes: " + serverVotes;
			//	//Console.WriteLine("Server Rank: {0}, Votes: {1}", serverRank, serverVotes);

			//	var response = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json");
			//	dynamic data = JsonConvert.DeserializeObject(response);
			//	var voters = data.voters;

			//	foreach (var voter in voters)
			//	{
			//		string nickname = voter.nickname;
			//		int votes = voter.votes;
			//		//Console.WriteLine("Nickname: {0}, Votes: {1}", nickname, votes);
			//		userVotesList = userVotesList + nickname + " = " + votes + "\n";
			//	}

			//	userVotesList = userVotesList + "\n\n[url=https://teamspeak-servers.org/server/12137/vote/]Vote for US[/url]";
			//	//Console.WriteLine(userVotesList);
			//	string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
			//	ChannelId channelId = new ChannelId(ServerVotesChannel);
			//	await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);

			//}


				string NewChannelName = "";
				string userVotesList = "";

				try
				{
					// Fetching the server votes data
					var responseVotes = await httpClient.GetStringAsync("https://teamspeak-servers.org/api/?object=servers&element=detail&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA");
					dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
					int serverRank = dataVotes.rank;
					int serverVotes = dataVotes.votes;
					NewChannelName = $"[cspacer1231]Server Rank: {serverRank} | Votes: {serverVotes}";

					// Fetching the voters list data
					var response = await httpClient.GetStringAsync("https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json");
					dynamic data = JsonConvert.DeserializeObject(response);
					var voters = data.voters;

					foreach (var voter in voters)
					{
						string nickname = voter.nickname;
						int votes = voter.votes;
						userVotesList += $"{nickname} = {votes}\n";
					}

					userVotesList += "\n\n[url=https://teamspeak-servers.org/server/12137/vote/]Vote for US[/url]";
					string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";

					ChannelId channelId = new ChannelId(ServerVotesChannel);
					await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);
				}
				catch (HttpRequestException e)
				{
					Console.WriteLine($"Request error: {e.Message}");
				}
				catch (Exception e)
				{
					Console.WriteLine($"An error occurred: {e.Message}");
				}
		}

		private async Task GetBTC()
		{
			ChannelId channelId = new ChannelId(BTCchannel);
			HttpClient client = new HttpClient();

			string btcData = await client.GetStringAsync(BTC);

			await tsFullClient.ChannelEdit(channelId, name: btcData + " USD");
		}


		private async Task GetETH()
		{
			ChannelId channelId = new ChannelId(ETHchannel);
			HttpClient client = new HttpClient();

			string ethData = await client.GetStringAsync(ETH);

			await tsFullClient.ChannelEdit(channelId, name: ethData + " USD");
		}

		private async Task GetGold()
		{
			ChannelId channelId = new ChannelId(GOLDchannel);
			HttpClient client = new HttpClient();

			string goldData = await client.GetStringAsync(Gold);
			//string goldData = "0";

			await tsFullClient.ChannelEdit(channelId, name: goldData + " USD");
		}

		private async Task GetSilver()
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

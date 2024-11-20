// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TSLib.Full.Book;
using TSLib.Full;
using System.Collections.Generic;
using TSLib;
using TSLib.Messages;
using System.Linq;
using System.IO;

namespace RankingSystem
{
	internal class AdminModule
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Constants constants = new Constants();

		public static string filePath = "badusernames.txt";

		// Report System
		private Dictionary<string, List<DateTime>> userMoveTimestamps = new Dictionary<string, List<DateTime>>();
		private TimeSpan moveWindow = TimeSpan.FromSeconds(5); // The time window to check moves
		private int maxMoves = 3; // Maximum allowed moves within the time window
		private bool isChecking = false;

		public AdminModule(Ts3Client ts3Client, TsFullClient tsFullClient, Connection serverView)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFullClient;
			this.serverView = serverView;
		}

		public void StartAdminModule()
		{
			tsFullClient.OnClientEnterView += onUserConnected;
			tsFullClient.OnClientLeftView += onUserDisconnected;
			tsFullClient.OnClientUpdated += onUserChangedNickname;
			tsFullClient.OnClientMoved += onClientMoved;
			tsFullClient.OnClientServerGroupAdded += onClientServerGroupAdded;
			tsFullClient.OnClientServerGroupRemoved += onClientServerGroupRemoved;
			tsFullClient.OnServerEdited += onServerEdited;

			Console.WriteLine("Admin Module initialized!");
		}

		private async void onUserConnected(object sender, IEnumerable<ClientEnterView> e) => await CheckForBadUsernames();
		private async void onUserDisconnected(object sender, IEnumerable<ClientLeftView> e) => await CheckForBadUsernames();
		private async void onUserChangedNickname(object sender, IEnumerable<ClientUpdated> e) => await CheckForBadUsernames();
		private async void onServerEdited(object sender, IEnumerable<ServerEdited> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				bool skipCurrentClient = false;
				foreach (var sg in constants.BotGroupsE)
				{
					ServerGroupId newSG = sg;
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
		private async void onClientServerGroupRemoved(object sender, IEnumerable<ClientServerGroupRemoved> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				//var clientServerGroups = fullClient.ServerGroups;
				bool skipCurrentClient = false;
				foreach (var sg in constants.BotGroupsE)
				{
					ServerGroupId newSG = sg;
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
		private async void onClientServerGroupAdded(object sender, IEnumerable<ClientServerGroupAdded> e)
		{
			foreach (var item in e)
			{
				var fullClient = await ts3Client.GetClientInfoById(item.InvokerId);
				//var clientServerGroups = fullClient.ServerGroups;
				bool skipCurrentClient = false;
				foreach (var sg in constants.BotGroupsE)
				{
					ServerGroupId newSG = sg;
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
		private async void onClientMoved(object sender, IEnumerable<ClientMoved> e)
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
					if (ServerGroup == constants.AdminGroup)
					{
						await ts3Client.SendMessage(message, item.Value.Id);
					}
				}
			}
		}

		public async Task TestTask()
		{
			await Task.Delay(1000);
			Console.WriteLine("Tested AFK done!");
		}

		private async Task CheckForBadUsernames()
		{
			if (isChecking) { return; }
			isChecking = true;

			await Task.Delay(500); // Add a 500ms delay before starting the method


			foreach (var oneuser in serverView.Clients)
			{
				if (CheckBadUsernames(oneuser.Value.Name))
				{
					await tsFullClient.KickClientFromServer(oneuser.Value.Id, "No DDoS, No Trolls, No Nazis and No Kevin you immature little prick, you neither. Please go back to Discord and stay there!");
				}

			}
			isChecking = false;
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
			tsFullClient.OnClientEnterView -= onUserConnected;
			tsFullClient.OnClientLeftView -= onUserDisconnected;
			tsFullClient.OnClientUpdated -= onUserChangedNickname;
			tsFullClient.OnClientMoved -= onClientMoved;
			tsFullClient.OnClientServerGroupAdded -= onClientServerGroupAdded;
			tsFullClient.OnClientServerGroupRemoved -= onClientServerGroupRemoved;
			tsFullClient.OnServerEdited -= onServerEdited;
		}

	}
}

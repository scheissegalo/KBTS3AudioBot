// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System.Threading.Tasks;
using System;
using TS3AudioBot;
using TSLib.Full.Book;
using TSLib.Full;
using TSLib;
using System.Collections.Generic;
using RankingSystem.Interfaces;
using RankingSystem.Models;
using System.Linq;

namespace RankingSystem
{
	internal class AfkModule
    {
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Constants constants = new Constants();
		LocalizationManager localizationManager = new LocalizationManager();
		public readonly IUserRepository _userRepository;

		private int AFKNotice;

		// Dictionary to track users who have been warned
		//Dictionary<int, bool> warnedUsers = new Dictionary<int, bool>();
		Dictionary<int, DateTime> warnedUsers = new Dictionary<int, DateTime>();

		public AfkModule(Ts3Client ts3Client, TsFullClient tsFullClient, Connection serverView, IUserRepository userRepository)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFullClient;
			this.serverView = serverView;
			_userRepository = userRepository;
		}

		public void StartAfkModule()
		{
			AFKNotice = constants.AFKTime - 5;
			Console.WriteLine($"AFK Module initialized! AFKTime: {constants.AFKTime} AFKNoticeTime: {AFKNotice}");
		}

		public async Task UserIdleCheck()
		{
			//Console.WriteLine("Checking Idle Users...");
			try
			{
				var allConnectedClients = serverView.Clients;
				foreach (var client in allConnectedClients)
				{
					//Check if user is in excludet group
					bool skipCurrentClient = false;
					foreach (var sg in constants.BotGroupsE)
					{
						ServerGroupId newSG = sg;
						if (client.Value.ServerGroups.Contains(newSG))
						{
							//Console.WriteLine("Skipping Bot");
							skipCurrentClient = true;
							break;
						}
					}
					if (skipCurrentClient)
						continue;

					TSUser? tsuser = _userRepository.FindOne(client.Value.Uid.Value);
					if (tsuser != null)
					{
						if (!tsuser.RankingEnabled)
							continue;
					}

					if (GetUserCountFromChannelId(client.Value.Channel) > 1 && client.Value.Channel != constants.AfkChannel && !client.Value.ServerGroups.Contains(constants.NoAfkGroup))
					{
						var ci = await ts3Client.GetClientInfoById(client.Value.Id);

						if (ci.ClientIdleTime != null)
						{
							TimeSpan ts = ci.ClientIdleTime;
							double totalMinutesAfk = Math.Round(ts.TotalMinutes);

							//Console.WriteLine($"Checking user {ci.Name}, AFK Time: {ci.ClientIdleTime} converted: {totalMinutesAfk} and ");
							string countryCode = ci.CountryCode ?? "US"; // Default to "DE" for Germany if null

							//Console.WriteLine($"User {ci.Name} with country code {countryCode} checked!");

							if (totalMinutesAfk >= AFKNotice && totalMinutesAfk < constants.AFKTime)
							{
								// Check if the user hasn't been warned yet
								if (!warnedUsers.ContainsKey(client.Value.Id.Value) || warnedUsers[client.Value.Id.Value].Date != DateTime.Now.Date)
								{
									if (constants.SendAFKNotice)
									{
										string noticeMessage = localizationManager.GetTranslation(client.Value.CountryCode, "warningMessage");
										await tsFullClient.SendPrivateMessage(noticeMessage, client.Value.Id);
										//Console.WriteLine($"Sendind User message {ci.Name} AFKNoticeTime is: {totalMinutesAfk}>={AFKNotice}");
									}

									// Mark the user as warned by setting the current date
									warnedUsers[client.Value.Id.Value] = DateTime.Now;
								}
							}
							else if (totalMinutesAfk >= constants.AFKTime)
							{

								if (!client.Value.ServerGroups.Contains(constants.NoAfkGroup))
								{
									await tsFullClient.ServerGroupAddClient(constants.NoAfkGroup, client.Value.DatabaseId);
								}

							}

						}
					}
					else
					{
						if (client.Value.ServerGroups.Contains(constants.NoAfkGroup))
						{
							var ci = await ts3Client.GetClientInfoById(client.Value.Id);

							if (ci.ClientIdleTime != null)
							{
								TimeSpan ts = ci.ClientIdleTime;
								double totalMinutesAfk = Math.Round(ts.TotalMinutes);

								if (totalMinutesAfk < constants.AFKTime)
								{
									await tsFullClient.ServerGroupDelClient(constants.NoAfkGroup, client.Value.DatabaseId);
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}
		}

		private async Task SendServerMessage(ClientId clientId, string message)
		{
			const int maxChunkSize = 95; // Poke Message Legth

			// Split the message into chunks
			var chunks = SplitIntoChunks(message, maxChunkSize);

			// Send each chunk through PokeClient
			foreach (var chunk in chunks)
			{
				await tsFullClient.PokeClient(chunk, clientId);
			}
		}

		private List<string> SplitIntoChunks(string message, int chunkSize)
		{
			List<string> chunks = new List<string>();
			for (int i = 0; i < message.Length; i += chunkSize)
			{
				int endIndex = Math.Min(i + chunkSize, message.Length);
				chunks.Add(message.Substring(i, endIndex - i));
			}
			return chunks;
		}

		private List<int> GetUsersInChannel(int ChanId)
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
				if (!client.Value.ServerGroups.Contains(serverGroupId))
				{
					if (client.Value.Channel == ChanId)
					{
						count++;
					}
				}
			}
			return count;
		}

		public async Task TestTask()
		{
			await Task.Delay(1000);
			Console.WriteLine("Tested AFK done!");
		}
	}
}

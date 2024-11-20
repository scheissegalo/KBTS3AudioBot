// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Linq;
using System.Threading.Tasks;
using TSLib.Full;
using TSLib.Full.Book;
using RankingSystem.Interfaces;
using TSLib;
using static RankingSystem.RankingModule;
using TSLib.Messages;
using RankingSystem.Models;
using System.Collections.Generic;
using RankingSystem.Modules;

namespace RankingSystem.Services
{
	public class ChannelManager : IChannelManager
	{
		private readonly TsFullClient _tsFullClient;
		private Constants _constants = new Constants();
		public ChannelManager(TsFullClient tsFullClient)
		{
			_tsFullClient = tsFullClient;
		}

		public async Task<bool> DoesChannelExist(ChannelId channelId)
		{
			var channels = await _tsFullClient.ChannelList();
			return channels.Value.Any(c => c.ChannelId == channelId);
		}
		public async Task KickAllUsersFromChannel(TSUser user)
		{
			var clientList = await _tsFullClient.ClientList();

			if (clientList.Value.Any(c => c.ChannelId == (ChannelId)user.ChannelIDInt))
			{
				// Get all clients in the specified channel
				var clientsInChannel = clientList.Value
					.Where(c => c.ChannelId == (ChannelId)user.ChannelIDInt)
					.ToList();

				// Create a list of ClientIds to kick
				var clientIdsToKick = clientsInChannel.Select(c => c.ClientId).ToArray();

				if (clientIdsToKick.Length > 0)
				{
					try
					{
						// Kick all clients from the channel
						await _tsFullClient.KickClient(clientIdsToKick, ReasonIdentifier.Channel, "Request to delete channel!");
						//Console.WriteLine($"Kicked {clientIdsToKick.Length} client(s) from channel {user.ChannelIDInt}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to kick clients from channel {user.ChannelIDInt}: {ex.Message}");
					}
				}
			}
		}

		public async Task<ChannelId?> CreateChannel(string channelName)
		{
			try
			{
				var generator = new RandomNameGenerator();
				string newChannelName;

				int tryCounter = 0;

				while (true)
				{
					if (tryCounter == 0)
					{
						newChannelName = channelName;
					}
					else
					{
						newChannelName = $"{channelName}-{generator.GenerateRandomName()}";
					}
					
					var result = await _tsFullClient.ChannelCreate(
						newChannelName,
						parent: _constants.CustomParentChannel,
						type: ChannelType.Permanent,
						description: $"Created with [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] N-SYS for {channelName} at {DateTime.Now.ToString()}"
					);
					// Check if the result is successful and contains a response
					if (result.Ok)
					{
						return result.Value.ChannelId;
					}
					if (tryCounter >= 10)
					{
						//Console.WriteLine($"Failed to create channel. Error: {result.Error.Message}");
						return null;
					}
					tryCounter++;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception while creating channel: {ex.Message}");
			}

			return null;
		}


		public async Task<bool> DeleteChannel(ChannelId channelId)
		{
			try
			{
				await _tsFullClient.ChannelDelete(channelId, true);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error deleting channel: {ex.Message}");
				return false;
			}
		}

		public async Task<int> GetUserCountFromChannelId(ChannelId channelId)
		{
			//Console.WriteLine($"Getting user count from channel {channelId.Value.ToString()}");
			try
			{
				var clientList = await _tsFullClient.ClientList();

				if (clientList.Value == null)
				{
					//Console.WriteLine("No clients found.");
					return 0;
				}

				var tasks = clientList.Value
							.Where(client => client.ChannelId == channelId)
							.Select(client => _tsFullClient.ClientInfo(client.ClientId))
							.ToList();

				var fullUserList = await Task.WhenAll(tasks);

				// Filter the full users based on the server groups
				var filteredClients = fullUserList
					.Where(fullUser => fullUser.Value.ServerGroups != null &&
									   fullUser.Value.ServerGroups.Any(group => !_constants.BotGroupsE.Contains(group)) &&
									   !fullUser.Value.ServerGroups.Contains(_constants.NoAfkGroup))
					.ToList();

				return filteredClients.Count;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting user count: {ex.Message}");
				return 0;
			}
		}
	}
}

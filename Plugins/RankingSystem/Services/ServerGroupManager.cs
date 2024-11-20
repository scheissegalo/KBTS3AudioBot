// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TSLib.Full;
using TSLib.Full.Book;
using RankingSystem.Interfaces;
using TSLib;
using RankingSystem.Models;

namespace RankingSystem.Services
{
	public class ServerGroupManager : IServerGroupManager
	{
		private readonly TsFullClient _tsFullClient;

		public ServerGroupManager(TsFullClient tsFullClient)
		{
			_tsFullClient = tsFullClient;
		}

		//public async Task<List<ServerGroupId>> GetUserGroups(ClientDbId clientDatabaseId)
		public async Task<List<ServerGroupId>> GetUserGroups(TSUser user)
		{
			try
			{
				var usr = await _tsFullClient.ClientInfo(user.ClientID);
				var response = await _tsFullClient.ServerGroupsByClientDbId(usr.Value.DatabaseId);
				return response.Value.Select(g => g.ServerGroupId).ToList();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error fetching server groups for user: {ex.Message}");
				return new List<ServerGroupId>();
			}
		}

		public async Task<bool> AddUserToGroup(ServerGroupId groupId, TSUser user)
		{
			try
			{
				var usr = await _tsFullClient.ClientInfo(user.ClientID);
				//var response = await _tsFullClient.ServerGroupsByClientDbId(usr.Value.DatabaseId);
				await _tsFullClient.ServerGroupAddClient(groupId, usr.Value.DatabaseId);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding user to group {groupId}: {ex.Message}");
				return false;
			}
		}

		public async Task<bool> RemoveUserFromGroup(ServerGroupId groupId, TSUser user)
		{
			try
			{
				var usr = await _tsFullClient.ClientInfo(user.ClientID);
				await _tsFullClient.ServerGroupDelClient(groupId, usr.Value.DatabaseId);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error removing user from group {groupId}: {ex.Message}");
				return false;
			}
		}
	}
}

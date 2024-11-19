using RankingSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TSLib;
using TSLib.Full;

namespace RankingSystem.Interfaces
{
	public interface IServerGroupManager
	{
		Task<List<ServerGroupId>> GetUserGroups(TSUser user);
		Task<bool> AddUserToGroup(ServerGroupId groupId, TSUser user);
		Task<bool> RemoveUserFromGroup(ServerGroupId groupId, TSUser user);
	}
}

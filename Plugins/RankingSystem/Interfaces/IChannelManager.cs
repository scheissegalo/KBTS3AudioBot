using RankingSystem.Models;
using System.Threading.Tasks;
using TSLib;
using TSLib.Full;

namespace RankingSystem.Interfaces
{
	public interface IChannelManager
	{
		Task<bool> DoesChannelExist(ChannelId channelId);
		Task<ChannelId?> CreateChannel(string channelName);
		Task<bool> DeleteChannel(ChannelId channelId);
		Task<int> GetUserCountFromChannelId(ChannelId channelId);
		Task KickAllUsersFromChannel(TSUser user);
	}
}

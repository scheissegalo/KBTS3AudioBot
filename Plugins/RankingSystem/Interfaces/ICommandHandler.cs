using System.Threading.Tasks;
using TSLib.Messages;

namespace RankingSystem.Interfaces
{
	public interface ICommandHandler
	{
		Task<bool> TryHandleCommand(TextMessage message);
	}
}

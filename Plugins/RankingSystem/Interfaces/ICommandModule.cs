using RankingSystem.Services;
using System.Threading.Tasks;
using TSLib.Messages;

namespace RankingSystem.Interfaces
{
	public interface ICommandModule
	{
		void RegisterCommands(CommandManager commandManager);
	}
}

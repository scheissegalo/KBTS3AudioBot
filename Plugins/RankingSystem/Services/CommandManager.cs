using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TSLib.Messages;
using RankingSystem.Interfaces;

namespace RankingSystem.Services
{
	public class CommandManager
	{
		private readonly Dictionary<string, Func<TextMessage, Task>> _commands;

		public CommandManager()
		{
			_commands = new Dictionary<string, Func<TextMessage, Task>>(StringComparer.OrdinalIgnoreCase);
		}

		public void RegisterCommand(string command, Func<TextMessage, Task> handler)
		{
			if (!_commands.ContainsKey(command))
			{
				_commands.Add(command, handler);
			}
		}

		public async Task<bool> TryHandleCommand(TextMessage message)
		{
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string baseCommand = parts[0].ToLower();

			if (_commands.TryGetValue(baseCommand, out var commandHandler))
			{
				// Await the command handler to ensure it executes in the correct context
				await commandHandler(message);
				return true;
			}

			return false; // Command not found
		}

		public void LoadModule(ICommandModule module)
		{
			module.RegisterCommands(this);
		}
	}
}

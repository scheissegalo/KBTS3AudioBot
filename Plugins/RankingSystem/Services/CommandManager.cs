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

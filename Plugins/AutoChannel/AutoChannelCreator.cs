// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;

namespace AutoChannel
{
	public class AutoChannelCreator : IBotPlugin
	{
		public static AutoChannelCreator? Instance { get; private set; }
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		// Create by Player count Channel ID to create subchannels in (Remote: 589 | Local: 506
		private ChannelId parentChannelId;// = (ChannelId)589;
		private Dictionary<int, List<ChannelId>> occupancyChannels = new Dictionary<int, List<ChannelId>>();
		private Dictionary<int, List<ChannelId>> parentChannels = new Dictionary<int, List<ChannelId>>();
		private bool isCheckingChannels = false;
		private bool looping = true;


		public AutoChannelCreator(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			Instance = this;
		}

		private async Task LoadParentChannels()
		{
			// Retrieve all channels under the main parent channel
			//var channels = serverView.Channels; // Replace with actual method to get channels
			var channels = await tsFullClient.ChannelList();

			//Console.WriteLine($"Iteration over {channels.Value.Count()} channels");
			Log.Debug($"Iteration over {channels.Value.Count()} channels");

			foreach (var channel in channels.Value)
			{
				// Check if channel matches your parent naming pattern
				if (channel.Name.StartsWith("▪ ■ ┐ (") && channel.Name.Contains("Player-Channels"))
				{
					// Extract the maxClients number from the name (assuming it's always formatted this way)
					var maxClientsString = channel.Name.Split('(')[1].Split(')')[0];
					if (int.TryParse(maxClientsString, out int maxClients))
					{
						if (!parentChannels.ContainsKey(maxClients))
						{
							parentChannels[maxClients] = new List<ChannelId>();
						}
						parentChannels[maxClients].Add(channel.ChannelId);
					}
				}
			}
			//Console.WriteLine($"{parentChannels.Count.ToString()} Parent channels Loaded");
			Log.Debug($"{parentChannels.Count.ToString()} Parent channels Loaded");
		}

		private async Task CreateParentChannels()
		{
			for (int maxClients = 2; maxClients <= 8; maxClients++)
			{
				var parentChannel = await tsFullClient.ChannelCreate(
					name: $"▪ ■ ┐ ({maxClients}) Player-Channels ▪ ↓",
					topic: $"Main Channel for up to {maxClients} players",
					maxClients: 0,
					maxClientsUnlimited: false,
					parent: parentChannelId,
					type: ChannelType.Permanent);

				parentChannels[maxClients] = new List<ChannelId> { parentChannel.Value.ChannelId };
				await AddSubchannel(maxClients);
				await AddSubchannel(maxClients);
				//await CheckChan(maxClients);
				//channelName = $"┐ ■ ({maxClients}) Players-Channel ▪ ↓";
			}
		}

		private async Task DeleteAllParentChannels()
		{
			try
			{
				var channels = await tsFullClient.ChannelList();

				foreach (var channel in channels.Value)
				{
					if (channel.ParentChannelId == parentChannelId)
					{
						try
						{
							// Delete the channel (both parent and subchannels)
							await tsFullClient.ChannelDelete(channel.ChannelId);
							//Console.WriteLine($"Deleted Channel: {channel.Name}");
						}
						catch (Exception ex)
						{
							//Console.WriteLine($"Error deleting channel {channel.Name}: {ex.Message}");
							Log.Error($"Error deleting channel {channel.Name}: {ex.Message}");
						}
						//Console.WriteLine($"Delete Channel {channel.Name}");
						//await tsFullClient.ChannelDelete(channel.ChannelId);
					}
				}
			}
			catch (Exception ex)
			{
				//Console.WriteLine($"Error fetching channel list: {ex.Message}");
				Log.Error($"Error fetching channel list: {ex.Message}");
			}
		}

		// Existing method for creating a subchannel, with minor adjustments
		private async Task<ChannelId> AddSubchannel(int maxClients)
		{
			var generator = new RandomNameGenerator();
			string channelName;
			bool nameExists;

			do
			{
				// Generate a new channel name
				channelName = $"│→ ({maxClients}) Players-{generator.GenerateRandomName()}";
				//channelName = $"│ ■ ({maxClients}) Players-Channel ▪ ↓";

				// Check if the channel name already exists
				nameExists = await CheckIfChannelExists(channelName);

			} while (nameExists); // Loop until a unique name is found

			var parentChannelId = parentChannels[maxClients][0];

			var subChannel = await tsFullClient.ChannelCreate(
				name: channelName,
				topic: "Subchannel for active players",
				maxClients: maxClients,
				maxClientsUnlimited: false,
				parent: parentChannelId,
				type: ChannelType.Permanent);

			// Add the subchannel ID to occupancyChannels
			if (!occupancyChannels.ContainsKey(maxClients))
			{
				occupancyChannels[maxClients] = new List<ChannelId>();
			}
			occupancyChannels[maxClients].Add(subChannel.Value.ChannelId);

			// Return the Channel ID of the created subchannel
			return subChannel.Value.ChannelId;
		}

		private async Task CheckSubChannels()
		{
			//Console.WriteLine($"Started Channel Check");
			occupancyChannels.Clear();
			var chanList = await tsFullClient.ChannelList();
			//Dictionary<ChannelId, Channel> serverChannels = serverView.Channels;

			// Pre-build a lookup dictionary of parent channel IDs for quick access
			var parentChannelLookup = new Dictionary<ChannelId, int>();
			foreach (var entry in parentChannels)
			{
				int maxClients = entry.Key;
				foreach (var parentChannelId in entry.Value)
				{
					parentChannelLookup[parentChannelId] = maxClients;
				}
			}

			foreach (var chan in chanList.Value)
			{
				//Console.WriteLine($"Channel: {chan.Name}");		
				//				
				var channelInfoResult = await tsFullClient.ChannelInfo(chan.ChannelId);
				if (!channelInfoResult.Ok || channelInfoResult.Value == null)
				{
					continue;
				}
				foreach (var channelInfo in channelInfoResult.Value)
				{
					// Check if this channel's parent is in the parent channel lookup
					if (parentChannelLookup.TryGetValue(channelInfo.ParentChannelId, out int maxClients))
					{
						//Console.WriteLine($"{channelInfo.Name} is added to list!");

						// Ensure the maxClients entry exists in occupancyChannels
						if (!occupancyChannels.ContainsKey(maxClients))
						{
							occupancyChannels[maxClients] = new List<ChannelId>();
						}

						// Add the channel to the appropriate occupancyChannels list
						occupancyChannels[maxClients].Add(chan.ChannelId);
						break; // Optional: Stop after the first match to avoid duplicate entries
					}
				}
			}

			var allClients = await tsFullClient.ClientList();
			//Console.WriteLine($"Finished Refresh list {occupancyChannels.Count} channels");
			Log.Debug($"Finished Refresh list {occupancyChannels.Count} channels");
			// Ensure each occupancy level has exactly 2 free channels
			foreach (var entry in parentChannels)
			{
				//Console.WriteLine($"Key: {entry.Key} Value: {entry.Value}");
				int maxClients = entry.Key;

				// Check if occupancyChannels has an entry for this maxClients value; if not, initialize it
				if (!occupancyChannels.ContainsKey(maxClients))
				{
					occupancyChannels[maxClients] = new List<ChannelId>();
				}

				var channelList = occupancyChannels[maxClients];
				//Console.WriteLine($"{maxClients} has {channelList.Count} channel(s) available");

				// Count the free channels
				int freeChannels = channelList.Count(channelId =>
					!allClients.Value.Any(client => client.ChannelId == channelId));

				// If there are fewer than 2 free channels, create more
				int channelsToCreate = 2 - freeChannels;
				for (int i = 0; i < channelsToCreate; i++)
				{
					var newChannelId = await AddSubchannel(maxClients);
					channelList.Add(newChannelId);  // Add the new subchannel ID to the occupancyChannels list
				}

				//If there are more than 2 free channels, delete extras
				if (freeChannels > 2)
				{
					int channelsToDelete = freeChannels - 2;
					foreach (var channelId in channelList.Take(channelsToDelete).ToList())  // Use ToList() to avoid modifying collection while iterating
					{
						await tsFullClient.ChannelDelete(channelId);
						occupancyChannels[maxClients].Remove(channelId); // Remove deleted channel from occupancyChannels list
					}
				}
			}

		}

		// Method to check if a channel with the given name already exists
		private async Task<bool> CheckIfChannelExists(string channelName)
		{
			// Replace this with your actual method to retrieve channels and check for existence
			var channels = await tsFullClient.ChannelList(); // Hypothetical method to get all channels
			return channels.Value.Any(channel => channel.Name == channelName);
		}

		[Command("createparentchannels")]
		public async Task CommandCreateParentChannel(ClientCall invoker)
		{
			if (Instance == null)
			{
				Log.Error("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.CreateParentChannels();
		}

		[Command("checkchannel")]
		public async Task CommandCheckSubChannels(ClientCall invoker)
		{
			if (Instance == null)
			{
				Log.Error("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.CheckSubChannels();
		}

		[Command("loadparentchannels")]
		public async Task CommandLoadParentChannel(ClientCall invoker)
		{
			if (Instance == null)
			{
				Log.Error("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.LoadParentChannels();
		}

		[Command("deletechannel")]
		public async Task DeleteChannel(ClientCall invoker)
		{
			if (Instance == null)
			{
				//Console.WriteLine("AutoChannelCreator instance is not initialized.");
				Log.Error("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.DeleteAllParentChannels();
		}

		[Command("rebuildchannel")]
		public async Task ComandRebuildChannels(ClientCall invoker)
		{
			if (Instance == null)
			{
				Log.Error("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.DeleteAllParentChannels();
			await Instance.CreateParentChannels();
		}

		public async void Initialize()
		{
			// Check if the file 'local.txt' exists in the working directory
			if (System.IO.File.Exists("local.txt"))
			{
				parentChannelId = (ChannelId)506; // Local setting
				Log.Warn("Loading Auto Channel Creator - Local Setting");
			}
			else
			{
				parentChannelId = (ChannelId)589; // Remote setting
			}

			try
			{
				tsFullClient.OnClientMoved += OnUserMoved;
				
				await LoadParentChannels();
				Log.Info("Auto Channel Creator - Initialized");
				await StartLoop();

				//await CheckSubChannels();
			}
			catch (Exception ex)
			{
				//Console.WriteLine($"Error during initialization: {ex.Message}");
				Log.Error($"Error during initialization: {ex.Message}");
			}

		}

		private void OnUserMoved(object sender, IEnumerable<ClientMoved> e)
		{

			foreach (var clientMoved in e)
			{
				var channelId = clientMoved.TargetChannelId;

				// Check if the new channel is in our occupancy channels
				if (occupancyChannels.Values.SelectMany(list => list).Contains(channelId))
				{
					//Console.WriteLine($"User {clientMoved.ClientId} moved to channel {channelId}. Checking channels...");
					_ = CheckSubChannels(); // Call CheckChannels asynchronously
				}
			}

		}

		private async Task StartLoop()
		{
			while (looping)
			{
				await Task.Delay(TimeSpan.FromMinutes(1));
				try
				{
					await CheckSubChannels();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
				
			}
		}

		public void Dispose()
		{
			tsFullClient.OnClientMoved -= OnUserMoved;
			//tsFullClient.OnClientMoved -= OnUserMoved;
			looping = false;
		}

	}

	public class RandomNameGenerator
	{
		private static readonly List<string> ThreeLetterWords = new List<string>
		{
			"ALPHA",   // Loot or items
			"BRAVO",   // Group mission
			"CHARLIE",   // Boss fight
			"DELTA",    // Player vs. Player
			"ECHO",   // Cooperative play
			"FOXTROT",    // Role-Playing Game
			"GOLF",    // Non-Player Character
			"HOTEL",    // First-Person Shooter
			"INDIA",  // In-game quest
			"HERO",   // Main character
			"CLAN",   // Player groups
			"DUEL",   // One-on-one battle
			"RUN",    // Movement or quest
			"HP",     // Health Points
			"XP",     // Experience Points
			"DPS",    // Damage Per Second
			"LIMA",    // Heads-Up Display
			"BLIZ",   // Game mode
			"TANGO",    // Game environment
			"RACE",   // Racing games or mode
			"TEAM",   // Team-based play
			"ZONE",   // Game region or level
			"RELOAD",    // Modifications
			"CHARGE",    // Tagging mechanics
			"MED",    // Health or medkit
			"SQUAD",  // Team or group
			"INFANTRY",  // Fighting or combat
			"ARENA",  // PvP area
			"FORT",   // Defensive structure
			"BATTALION",   // Waves of enemies
			"COMPANY",  // Player level or stage
			"WIN",    // Winning a game
			"SQUAD",   // Losing a game
			"HUNTER",   // Losing a game
			"WIZARD",   // Losing a game
			"MYSTIC",   // Losing a game
			"TITAN",   // Losing a game
			"SLAYER",   // Losing a game
			"PHANTOM",   // Losing a game
			"DRAGON",   // Losing a game
			"NEXUS",   // Losing a game
			"GOBLIN",   // Losing a game
			"PORTAL",   // Losing a game
			"EMPIRE",   // Losing a game
			"DRUID",   // Losing a game
			"GLADIATOR",   // Losing a game
			"INFERNO",   // Losing a game
			"SHADOW"    // Losing a game
		};

		private static readonly List<string> emojis = new List<string>
		{
			"🎮",
			"☢️",
			"💀",
			"❤️",
			"☣️",
			"🔥",
			"😈",
			"👽",
			"⚔️",
			"⭐",
			"💥",
			"✨",
			"🍻",
			"👺",
			"⚡",
			"👻",
			"💲",
			"🚨",
			"🌸",
			"👮‍",
			"🤙",
			"👿",
			"❄️",
			"💎",
			"🥇",
			"🌍",
			"🍌",
			"🎲",
			"🕹️",
			"🍀",
			"💯",
			"👍",
			"😂",
			"🤖",
			"🚀",
			"🚬",
			"💪",
			"🔪",
			"💬",
			"🥴",
			"🤟",
			"🤼🏽",
			"🍕",
			"💣",
			"💡",
			"🍒",
			"🐜",
			"⚽",
			"🦐",
			"🍍",
			"🧙‍",
			"🤝",
			"🐱",
			"🐭",
			"🌲",
			"🔧",
			"🏋",
			"👑",
			"🐼",
			"🤮",
			"🎯",
			"⛏",
			"👁️‍🗨️",
			"🦢",
			"🦙",
			"🦅",
			"✈",
			"🐘",
			"𐐘💥╾━╤デ╦︻ඞා",
			"╾━╤デ╦︻",
			"⋆༺𓆩☠︎𓆪༻⋆",
			"▄︻デ══━一💥",

		};

		private static readonly Random random = new Random();

		public string GenerateRandomName()
		{
			// Get a random index to select a word from the list
			int index = random.Next(ThreeLetterWords.Count);
			int emojiIndex = random.Next(emojis.Count);
			return ThreeLetterWords[index]+" "+emojis[emojiIndex];
		}
	}
}

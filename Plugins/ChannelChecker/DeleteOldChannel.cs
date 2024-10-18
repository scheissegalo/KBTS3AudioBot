using Newtonsoft.Json; // Make sure to install this via NuGet.
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using LiteDB;
using TS3AudioBot.CommandSystem;

namespace ChannelChecker
{
	public class DeleteOldChannel : IBotPlugin
	{
		// Singleton instance to make it accessible from the command method.dsf
		public static DeleteOldChannel? Instance { get; private set; }

		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;

		// Store ignored channels in a HashSet for fast lookups.
		private HashSet<ulong> ignoredChannelIds = new();

		// Store channel visit counts in a dictionary.
		private Dictionary<ulong, int> channelVisits = new();

		// Path to the JSON file for storing visit data.
		private const string DataFilePath = "ChannelVisits.json";
		private const string IgnoreListPath = "IgnoredChannels.json";


		public DeleteOldChannel(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			//ts channel max characters is 40 chars
			//take original name add  " - delete" and check if we hit 40 chars
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			// Assign the singleton instance.
			Instance = this;
		}

		public void Initialize()
		{
			tsFullClient.OnClientMoved += TsFullClient_OnChannelChanged;
			LoadVisitData(); // Load existing data on startup.
			LoadIgnoredChannels();
		}

		// Command method must be static and access the singleton instance.
		[Command("channelcheck")]
		public static async Task<string> CommandRank(ClientCall invoker)
		{
			// Use the singleton instance to call the method.
			if (Instance == null)
				return "Plugin not initialized.";

			Instance.CheckAndMarkChannelsForDeletion();
			return "Checked!";
		}

		[Command("addignore")]
		public static async Task<string> AddIgnoredChannel(ClientCall invoker, ulong channelId)
		{
			if (Instance == null)
				return "Plugin not initialized.";

			Instance.ignoredChannelIds.Add(channelId);
			Instance.SaveIgnoredChannels();
			return $"Channel {channelId} added to ignore list.";
		}

		[Command("removeignore")]
		public static async Task<string> RemoveIgnoredChannel(ClientCall invoker, ulong channelId)
		{
			if (Instance == null)
				return "Plugin not initialized.";

			if (Instance.ignoredChannelIds.Remove(channelId))
			{
				Instance.SaveIgnoredChannels();
				return $"Channel {channelId} removed from ignore list.";
			}
			else
			{
				return $"Channel {channelId} was not in the ignore list.";
			}
		}

		[Command("channelvisits")]
		public static async Task<string> GetChannelVisits(ClientCall invoker)
		{
			if (Instance == null)
				return "Plugin not initialized.";

			string report = Instance.GenerateVisitReport();
			await Instance.SendPaginatedReport(invoker, report);
			return "Channel visit report sent.";
		}

		private async Task SendPaginatedReport(ClientCall invoker, string report)
		{
			const int maxMessageLength = 8000; // Leave room for safety.
			var reportChunks = SplitStringByLine(report, maxMessageLength);

			foreach (var chunk in reportChunks)
			{
				await ts3Client.SendMessage(chunk, invoker.ClientId.Value);
				 //invoker.SendMessageAsync(chunk);
			}
		}

		private List<string> SplitString(string str, int chunkSize)
		{
			var chunks = new List<string>();
			for (int i = 0; i < str.Length; i += chunkSize)
			{
				chunks.Add(str.Substring(i, Math.Min(chunkSize, str.Length - i)));
			}
			return chunks;
		}

		private List<string> SplitStringByLine(string str, int maxChunkSize)
		{
			var chunks = new List<string>();
			var currentChunk = "";

			foreach (var line in str.Split('\n'))
			{
				// If adding the new line exceeds the limit, start a new chunk.
				if (currentChunk.Length + line.Length + 1 > maxChunkSize)
				{
					chunks.Add(currentChunk);
					currentChunk = "";
				}

				// Add the line to the current chunk.
				currentChunk += (currentChunk.Length > 0 ? "\n" : "") + line;
			}

			// Add the last chunk if there's any content left.
			if (!string.IsNullOrEmpty(currentChunk))
				chunks.Add(currentChunk);

			return chunks;
		}

		private string GenerateVisitReport()
		{
			var channels = serverView.Channels.Values;
			if (!channels.Any())
				return "No channels found.";

			string report = "Channel Visits:\n";
			foreach (var channel in channels)
			{
				ulong channelId = channel.Id.Value;
				//ChannelInfoResponse[] channelInfo = tsFullClient.ChannelInfo(channel.Id).Result.Value;
				int visits = channelVisits.TryGetValue(channelId, out var count) ? count : 0;
				bool isIgnored = ignoredChannelIds.Contains(channelId);

				report += $"[color=green]{channel.Name}[/color] : [color=red]{visits} visits[/color]";
				if (isIgnored)
					report += " [color=yellow](Ignored)[/color]";
				report += "\n";
			}

			return report;
		}

		private void TsFullClient_OnChannelChanged(object sender, IEnumerable<ClientMoved> e)
		{
			var movedClient = e.First();
			var targetChannelId = movedClient.TargetChannelId.Value;

			//if (ignoredChannelIds.Contains(targetChannelId))
			//	return; // Ignore this channel.

			if (channelVisits.ContainsKey(targetChannelId))
				channelVisits[targetChannelId]++;
			else
				channelVisits[targetChannelId] = 1;

			SaveVisitData();
		}

		// Add " - delete" to channels with zero visits.
		public void CheckAndMarkChannelsForDeletion()
		{
			var channels = serverView.Channels.Values;

			foreach (var channel in channels)
			{
				ulong channelId = channel.Id.Value;

				if (ignoredChannelIds.Contains(channelId))
				{
					//Console.WriteLine($"Skipping ignored channel: {channel.Name}");
					continue;
				}

				if (!channelVisits.ContainsKey(channelId) || channelVisits[channelId] == 0)
				{
					
					string newName = GenerateChannelName(channel.Name);
					//Console.WriteLine($"Renaming channel {channel.Name} to {newName}");
					string originalChannelDescription =
"[size=40][color=red]This channel is marked for deletion![/color][/size]\n\n" +
"[size=15][color=green]Channels without activity for several months are marked for deletion and will be removed soon.[/color][/size]\n" +
"[size=12][color=yellow]If this is your channel and you believe this is a mistake, please contact one of the admins or moderators.\n\nHappy gaming![/color][/size]";

					// Check if OptionalData and Description are not null
					if (channel.OptionalData != null && channel.OptionalData.Description != null)
					{
						originalChannelDescription += channel.OptionalData.Description;
					}
					
					// Perform the channel edit
					//string deletionDate = DateTime.Now.AddMonths(1).ToString("dd-MM-yyyy");
					tsFullClient.ChannelEdit(
						channel.Id,
						name: newName,
						description: originalChannelDescription,
						topic: $"This channel is marked for deletion!"
					);
					
					//Console.WriteLine("Current Channel: " + currentChannel.ToString());
					//tsFullClient.ChannelEdit(channel.Id, name: newName, description: originalChannelDescription, topic: $"This Channel is marked for deletion!");
				}
			}
		}

		// Ensure the name fits within the 40-character limit.
		private string GenerateChannelName(string originalName)
		{
			string deletionDate = DateTime.Now.AddDays(7).ToString("dd-MM-yyyy");
			string suffix = " - delete " + deletionDate;
			if (originalName.Length + suffix.Length <= 40)
				return originalName + suffix;

			int maxLength = 40 - suffix.Length;
			string truncatedName = originalName.Substring(0, maxLength);
			return truncatedName + suffix;
		}

		private void SaveVisitData()
		{
			string json = JsonConvert.SerializeObject(channelVisits, Formatting.Indented);
			System.IO.File.WriteAllText(DataFilePath, json);
		}

		private void LoadVisitData()
		{
			if (System.IO.File.Exists(DataFilePath))
			{
				string json = System.IO.File.ReadAllText(DataFilePath);
				channelVisits = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json)
								?? new Dictionary<ulong, int>();
			}
		}

		private void SaveIgnoredChannels()
		{
			string json = JsonConvert.SerializeObject(ignoredChannelIds.ToList(), Formatting.Indented);
			System.IO.File.WriteAllText(IgnoreListPath, json);
		}

		private void LoadIgnoredChannels()
		{
			if (System.IO.File.Exists(IgnoreListPath))
			{
				string json = System.IO.File.ReadAllText(IgnoreListPath);
				var channels = JsonConvert.DeserializeObject<List<ulong>>(json) ?? new List<ulong>();
				ignoredChannelIds = new HashSet<ulong>(channels);
			}
		}

		public void Dispose()
		{
			tsFullClient.OnClientMoved -= TsFullClient_OnChannelChanged;
		}
	}
}

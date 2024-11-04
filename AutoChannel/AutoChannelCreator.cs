using TS3AudioBot;
//using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Text;

namespace AutoChannel
{
	public class AutoChannelCreator : IBotPlugin
	{
		public static AutoChannelCreator? Instance { get; private set; }
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;

		// Create by Player count Channel ID to create subchannels in (Remote: 589 | Local: 506
		private ChannelId parentChannelId;// = (ChannelId)589;
		private Dictionary<int, List<ChannelId>> occupancyChannels = new Dictionary<int, List<ChannelId>>();
		private bool isCheckingChannels = false;


		public AutoChannelCreator(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			Instance = this;
		}

		private async Task CheckChannels()
		{
			if (isCheckingChannels) return;
			isCheckingChannels = true;

			try
			{
				// Clear and update the occupancy channels dictionary
				occupancyChannels.Clear();

				// Fetch all channels and filter based on the parent channel ID

				Dictionary<ChannelId, Channel> serverChannels = serverView.Channels;
				foreach (var channel in serverChannels)
				{
					var channelInfos = await tsFullClient.ChannelInfo(channel.Value.Id);
					foreach (var channelInfo in channelInfos.Value)
					{
						// get all channels in By Player count channel
						if (channelInfo.ParentChannelId == parentChannelId)
						{
							int maxClients = channelInfo.MaxClients;
							//Console.WriteLine($"Channel: {channel.Value.Name} Max:{maxClients}");
							if (!occupancyChannels.ContainsKey(maxClients))
								occupancyChannels[maxClients] = new List<ChannelId>();

							occupancyChannels[maxClients].Add(channel.Value.Id);

						}
						//Console.WriteLine($"Name: {channel.Value.Name} - Info: {channelInfo.ParentChannelId.Value}");
					}
				}

				// ** Add this block to create initial channels if none are found **
				if (occupancyChannels.Count == 0)
				{
					for (int i = 2; i <= 8; i++) // Creating channels for 2 to 8 clients
					{
						await CreateNewChannel(i);
						await CreateNewChannel(i);
					}
					Console.WriteLine("Initial channels created for each occupancy level from 2 to 8 clients.");
				}

				// Ensure each occupancy level has exactly 2 free channels
				foreach (var entry in occupancyChannels)
				{
					int maxClients = entry.Key;
					List<ChannelId> channels = entry.Value;

					// Count the free channels
					int freeChannels = channels.Count(channelId =>
						!serverView.Clients.Values.Any(client => client.Channel == channelId));

					//Console.WriteLine($"Free channel count: {freeChannels}");

					// If there are fewer than 2 free channels, create more
					int channelsToCreate = 2 - freeChannels;
					for (int i = 0; i < channelsToCreate; i++)
					{
						await CreateNewChannel(maxClients);
					}

					// If there are more than 2 free channels, delete extras
					if (freeChannels > 2)
					{
						int channelsToDelete = freeChannels - 2;
						foreach (var channelId in channels.Take(channelsToDelete))
						{
							await tsFullClient.ChannelDelete(channelId);
						}
					}

					// ** Ensure channels are ordered correctly **
					//await OrderChannels(entry.Value);
					await OrderChannelsByMaxClients();

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			finally
			{
				isCheckingChannels = false;
			}


			//initialized = true;
		}

		private async Task OrderChannelsByMaxClients()
		{
			foreach (var group in occupancyChannels.OrderBy(g => g.Key))
			{
				// Sort the channels for each occupancy level
				List<ChannelId> channels = group.Value.OrderBy(id => (int)id.Value).ToList();

				// Only move channels if the ordering differs from the current one
				for (int i = 1; i < channels.Count; i++)
				{
					if (channels[i] != channels[i - 1])
					{
						await MoveChannel(channels[i], channels[i - 1]);
					}
				}
			}
		}

		private async Task DeleteChannels()
		{
			// Sort channels by max client groups and then within each group by ChannelId
			foreach (var group in occupancyChannels.OrderBy(g => g.Key))
			{
				List<ChannelId> channels = group.Value.OrderBy(id => (int)id.Value).ToList();

				// Move each channel in this occupancy level group to be sequential
				for (int i = 0; i < channels.Count; i++)
				{
					//await MoveChannel(channels[i], channels[i - 1]); // Move each channel below the previous one in the ordered list
					await tsFullClient.ChannelDelete(channels[i]);
				}
			}

		}

		private async Task CreateNewChannel(int maxClients)
		{
			var newChannel = await tsFullClient.ChannelCreate(
				name: $"╠-({maxClients}) Players-Channel_{GenerateRandomName(3)}",
				topic: "Dynamic Channel",
				description: $"Dynamic Channel for max {maxClients} clients",
				maxClients: maxClients,
				maxClientsUnlimited: false,
				parent:parentChannelId, type:
				ChannelType.Permanent);

			// Add the new channel to occupancyChannels
			if (!occupancyChannels.ContainsKey(maxClients))
			{
				occupancyChannels[maxClients] = new List<ChannelId>();
			}
			
			occupancyChannels[maxClients].Add(newChannel.Value.ChannelId);
		}

		private async Task MoveChannel(ChannelId channelID, ChannelId orderChannelID)
		{
			await tsFullClient.ChannelEdit(channelID, order: orderChannelID);
		}


		private string GenerateRandomName(int length = 3)
		{
			var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			var output = new StringBuilder();
			var random = new Random();


			for (int i = 0; i < length; i++)
			{
				output.Append(chars[random.Next(chars.Length)]);
			}
			return output.ToString();
		}

		[Command("createchannel")]
		public async Task CreateChannel(ClientCall invoker)
		{
			if (Instance == null)
			{
				Console.WriteLine("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.CheckChannels();
		}

		[Command("deletechannel")]
		public async Task DeleteChannel(ClientCall invoker)
		{
			if (Instance == null)
			{
				Console.WriteLine("AutoChannelCreator instance is not initialized.");
				return;  // Or handle the error as needed
			}

			await Instance.DeleteChannels();
		}

		public async void Initialize()
		{
			// Check if the file 'local.txt' exists in the working directory
			if (System.IO.File.Exists("local.txt"))
			{
				parentChannelId = (ChannelId)506; // Local setting
			}
			else
			{
				parentChannelId = (ChannelId)589; // Remote setting
			}

			try
			{
				tsFullClient.OnClientMoved += OnUserMoved;
				await CheckChannels(); // Await this if you make CheckChannels() async
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during initialization: {ex.Message}");
			}
		}

		private void OnUserMoved(object sender, IEnumerable<ClientMoved> e)
		{

			foreach (var clientMoved in e)
			{
				// Check if the client moved to a channel in the occupancyChannels
				var channelId = clientMoved.TargetChannelId;

				// Check if the new channel is in our occupancy channels
				if (occupancyChannels.Values.SelectMany(list => list).Contains(channelId))
				{
					//Console.WriteLine($"User {clientMoved.ClientId} moved to channel {channelId}. Checking channels...");
					_ = CheckChannels(); // Call CheckChannels asynchronously
				}
			}

		}

		public void Dispose()
		{
			tsFullClient.OnClientMoved -= OnUserMoved;
			//tsFullClient.OnClientMoved -= OnUserMoved;
		}

	}
}

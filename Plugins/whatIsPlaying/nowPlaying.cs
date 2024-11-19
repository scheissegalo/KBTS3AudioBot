using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
//using TS3AudioBot.Localization;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
//using TS3AudioBot.Web.Api;

namespace whatIsPlaying
{
	public class nowPlaying : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Player playerConnection;
		private Codec defaultCodec;
		private int defaultCodecQuality = 6;
		private Codec MusicCodec;
		private int MusicCodecQuality = 10;
		private ChannelId currentChannel;
		private string msgFoot;
		//private readonly ConfBot config;

		// Your dependencies will be injected into the constructor of your class.
		public nowPlaying(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, Player playerConnection)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
			this.playerConnection = playerConnection;
		}

		// The Initialize method will be called when all modules were successfully injected.
		public async void Initialize()
		{
			// Store the default codec type (can be set to a different value)
			defaultCodec = Codec.OpusVoice;
			MusicCodec = Codec.OpusMusic;

			playManager.AfterResourceStarted += Start;
			tsFullClient.OnClientMoved += OnBotMoved;
			//tsFullClient.OnDisconnected += OnDisconnect;

			await setChannelCommander();
			await GetCurrentChannelId();

			msgFoot = @"

-----------------------------------------

[b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] - Secure Gaming Services
[URL=https://north-industries.com]Home[/URL] | [URL=https://north-industries.com/ts-viewer/]TS-Viewer[/URL] | [URL=https://north-industries.com/teamspeak-connect/#rules]Rules[/URL]";
		}

		//private async void OnDisconnect(object sender, DisconnectEventArgs e)
		//{
		//	await Task.Delay(5000);
		//	await ts3Client.SetChannelCommander(true);
		//}

		private async Task GetCurrentChannelId()
		{
			var me = await tsFullClient.WhoAmI();
			ChannelId channelId = new ChannelId(me.Value.ChannelId.Value);
			currentChannel = channelId;
			//Console.WriteLine("Current Channel Changed "+ channelId);
		}

		private async Task setChannelCommander()
		{
			await ts3Client.SetChannelCommander(true);
		}

		private async Task Start(object sender, EventArgs e)
		{
			var self = serverView.OwnClient;
			try
			{
				string currentSong;

				// Check if the file is an MP3 by its extension
				if (playManager.CurrentPlayData.SourceLink.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
				{
					// Extract the filename without the path and extension
					
					//currentSong = Path.GetFileNameWithoutExtension(playManager.CurrentPlayData.SourceLink);
					currentSong = Path.GetFileName(playManager.CurrentPlayData.SourceLink);
					await ts3Client.SendChannelMessage($"Now playing: {currentSong}");
				}
				else
				{
					// For YouTube links
					currentSong = await YouTube.GetTitleFromUrlAsync(playManager.CurrentPlayData.SourceLink);
					await ts3Client.SendChannelMessage($"Now playing: {currentSong} | Today: {YouTube.requests} requests");
				}

				//await Task.Delay(500);
				//string currentTitle = await YouTube.GetTitleFromUrlAsync(playManager.CurrentPlayData.SourceLink);
				//if (!string.IsNullOrEmpty(currentTitle))
				//{
				//	await ts3Client.SendChannelMessage($"Now playing: {currentTitle} | Today: {YouTube.requests} requests");
				//}
				//else
				//{
				//	await ts3Client.SendChannelMessage($"Now playing: {playManager.CurrentPlayData.SourceLink}");
					
				//}
			}
			catch (Exception ex)
			{
				await ts3Client.SendChannelMessage("Error resolving trackname " + ex.Message);
			}



			//await ts3Client.SendChannelMessage("Song wird abgespielt");
		}

		private async void OnBotMoved(object sender, IEnumerable<ClientMoved> clients)
		{
			string helpMessage = @"
[b][color=green]To play music use the following commands:[/color][/b]
[b][color=red]!play[/color] [color=blue]<your link>[/color][/b] or [b][color=red]!yt [/color][color=blue]<your link>[/color][/b] - Attention the song will be played directly!
[b][color=red]!add [/color][color=blue]<your link>[/color][/b]The song is appended to the current playlist.
[b][color=red]!yts [/color][color=blue]'your searchtext'[/color][/b] To search on YouTube. 
[b][color=red]!ytp [/color][color=blue]'your searchtext'[/color][/b] To search on YouTube and play the first result.
[b][color=red]!ytpl [/color][color=blue]<your link>[/color][/b] to play the entire youtube playlist

[b][color=green]Local MP3's:[/color][/b]
[b][color=red]!listmp3[/color][/b] To list all MP3's on the Server.
[b][color=red]!datei [/color][color=blue]'filename'[/color][/b] To play an MP3.
[b][color=red]!randommp3[/color][/b] To play a random MP3.

[b][color=green]WatchParty:[/color][/b]
[b][color=red]!watch[/color][/b] Generate a watchparty link.

[b][color=red]!help[/color][/b] or https://north-industries.com/teamspeak-help/#musicbots for detailed help." + msgFoot;
			try
			{
				var me = await tsFullClient.WhoAmI();
				foreach (var client in clients)
				{
					if (client.ClientId == me.Value.ClientId)
					{
						if (currentChannel == client.TargetChannelId)
						{
							// Same Channel
						}
						else
						{
							ChannelId channelId = new ChannelId(client.TargetChannelId.Value);

							//await tsFullClient.ChannelEdit(currentChannel, codec: defaultCodec, codecQuality: defaultCodecQuality);
							//await tsFullClient.ChannelEdit(channelId, codec: MusicCodec, codecQuality: MusicCodecQuality);

							//Console.WriteLine("Bot was Moved to channel: " + client.TargetChannelId + "/" + channelId);

						}
						await ts3Client.SendChannelMessage(helpMessage);
						await GetCurrentChannelId();
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error editing channel properties: " + ex.Message);
			}
		}

		[Command("local")]
		public static async Task<string> CommandLocal(PlayManager playManager, ClientCall invoker, string query)
		{
			var file = Directory.GetFiles("mp3", $"*{query}*", SearchOption.TopDirectoryOnly).FirstOrDefault();
			if (file != null)
			{
				var filename = Path.GetFileName(file);
				//Console.WriteLine("File found:"+ filename);
				await playManager.Play(invoker, filename);
				return filename + " found and playing!";
			}
			else
			{
				//Ts3Client.SendPrivateMessage(Ts3Client.WhoAmI().UnwrapThrow().Uid, "No matching audio file found.");
				return "Nothing found!";
			}
		}

		[Command("datei")]
		public static async Task<string> CommandDatei(PlayManager playManager, Player playerConnection, ClientCall invoker, string query)
		{
			var file = Directory.GetFiles("mp3", $"*{query}*", SearchOption.TopDirectoryOnly).FirstOrDefault();
			if (file != null)
			{
				playerConnection.Volume = 100f;
				//playerConnection.Volume
				var filename = Path.GetFileName(file);
				var fullPath = Path.GetFullPath(file);

				//Console.WriteLine("File found: " + filename + " Full Path: " + fullPath);

				await playManager.Play(invoker, fullPath);
				return filename + " found and playing!";
			}
			else
			{
				//Ts3Client.SendPrivateMessage(Ts3Client.WhoAmI().UnwrapThrow().Uid, "No matching audio file found.");
				return "Nichts gefunden!";
			}
		}

		[Command("listmp3")]
		public static Task<string> CommandListFiles(ClientCall invoker)
		{
			var files = Directory.GetFiles("mp3", "*.mp3", SearchOption.TopDirectoryOnly);

			if (files.Length > 0)
			{
				var fileList = string.Join(Environment.NewLine, files.Select(Path.GetFileName));
				//Console.WriteLine("Files found: " + fileList);
				return Task.FromResult(Environment.NewLine + "Files Found: " + Environment.NewLine + fileList + Environment.NewLine + Environment.NewLine + "Play with !datei filename");
			}
			else
			{
				return Task.FromResult("No files found.");
			}
		}

		[Command("randommp3")]
		public static async Task<string> CommandRandomFile(PlayManager playManager, ClientCall invoker)
		{
			var files = Directory.GetFiles("mp3", "*.mp3", SearchOption.TopDirectoryOnly);

			if (files.Length > 0)
			{
				// Select a random file
				var random = new Random();
				var randomFile = files[random.Next(files.Length)];
				var fullPath = Path.GetFullPath(randomFile);
				var filename = Path.GetFileName(randomFile);

				//Console.WriteLine("Playing random file: " + filename);
				
				await playManager.Play(invoker, fullPath);
				return "Random file " + filename + " is playing!";
			}
			else
			{
				return "No files Found.";
			}
		}


		// You should prefer static methods which get the modules injected via parameter unless
		// you actually need objects from your plugin in your method.
		[Command("hello")]
		public static string CommandHello(PlayManager playManager, string name)
		{
			if (playManager.CurrentPlayData != null)
				return "hello " + name + ". We are currently playing: " + playManager.CurrentPlayData.ResourceData.ResourceTitle;
			else
				return "hello " + name + ". We are currently not playing.";
		}

		public void Dispose()
		{
			// Don't forget to unregister everything you have subscribed to,
			// otherwise your plugin will remain in a zombie state
			playManager.AfterResourceStarted -= Start;
			tsFullClient.OnClientMoved -= OnBotMoved;
		}
	}
}

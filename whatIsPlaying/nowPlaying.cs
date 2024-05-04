using System;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Commands;
using TSLib.Full;
using TSLib.Messages;
using TS3AudioBot.Config;
using System.Collections.Generic;
using Heijden.DNS;
using System.IO;
using System.Linq;
using LiteDB;

namespace whatIsPlaying
{
	public class nowPlaying : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Codec defaultCodec;
		private int defaultCodecQuality = 6;
		private Codec MusicCodec;
		private int MusicCodecQuality = 10;
		private ChannelId currentChannel;
		private string msgFoot;
		//private readonly ConfBot config;

		// Your dependencies will be injected into the constructor of your class.
		public nowPlaying(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		// The Initialize method will be called when all modules were successfully injected.
		public void Initialize()
		{
			// Store the default codec type (can be set to a different value)
			defaultCodec = Codec.OpusVoice;
			MusicCodec = Codec.OpusMusic;

			playManager.AfterResourceStarted += Start;
			playManager.PlaybackStopped += Stop;
			tsFullClient.OnClientMoved += OnBotMoved;

			setChannelCommander();
			GetCurrentChannelId();

			msgFoot = @"

-----------------------------------------

Dein [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] - Secure Gaming Services
[URL=https://north-industries.com]Home[/URL] | [URL=https://north-industries.com/ts-viewer/]TS-Viewer[/URL] | [URL=north-industries.com/ts-invites/#regeln]Guidelines/Help[/URL]";
		}

		private async void GetCurrentChannelId()
		{
			var me = await tsFullClient.WhoAmI();
			ChannelId channelId = new ChannelId(me.Value.ChannelId.Value);
			currentChannel = channelId;
			//Console.WriteLine("Current Channel Changed "+ channelId);
		}

		private async void setChannelCommander()
		{
			await ts3Client.SetChannelCommander(true);
		}

		private async Task Start(object sender, EventArgs e)
		{
			var self = serverView.OwnClient;
			try
			{
				//await Task.Delay(500);
				string currentTitle = await YouTube.getTitleFromUrl(playManager.CurrentPlayData.SourceLink);
				if (!string.IsNullOrEmpty(currentTitle))
				{
					await ts3Client.SendChannelMessage("Playing [b]" + currentTitle + "[/b]");
				}
			}
			catch (Exception ex)
			{
				//await ts3Client.SendChannelMessage("Error resolving trackname " + ex.Message);
			}



			//await ts3Client.SendChannelMessage("Song wird abgespielt");
		}

		private async Task Stop(object sender, EventArgs e)
		{
			//if (lastName != null) await ts3Client.ChangeName(lastName);
		}

		private async void OnBotMoved(object sender, IEnumerable<ClientMoved> clients)
		{
			string helpMessage = @"
To play music use the following commands:
[b][color=red]!play[/color] [color=blue]<your link>[/color][/b] or [b][color=red]!yt [/color][color=blue]<your link>[/color][/b] - Attention the song will be played directly!
[b][color=red]!add [/color][color=blue]<your link>[/color][/b]The song is appended to the current playlist.
[b][color=red]!yts [/color][color=blue]'your searchtext'[/color][/b] To search on YouTube. [b][color=green][/color][/b]
[b][color=red]!ytp [/color][color=blue]'your searchtext'[/color][/b] To search on YouTube and play the first result. [b][color=green][/color][/b]

[b]Please use the new method insead for better audio:[/b]
[b][color=red]!byt [/color][color=blue]'your youtube link'[/color][/b][b][color=green] To play YouTube link. [/color][/b]
[b][color=red]!byts [/color][color=blue]'your searchtext'[/color][/b][b][color=green] To search on YouTube and play the first result. [/color][/b]

[b][color=red]!help[/color][/b] for detailed help." + msgFoot;
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

							await tsFullClient.ChannelEdit(currentChannel, codec: defaultCodec, codecQuality: defaultCodecQuality);
							await tsFullClient.ChannelEdit(channelId, codec: MusicCodec, codecQuality: MusicCodecQuality);

							//Console.WriteLine("Bot was Moved to channel: " + client.TargetChannelId + "/" + channelId);

						}
						await ts3Client.SendChannelMessage(helpMessage);
						GetCurrentChannelId();
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
				return filename + " gefunden und wird abgespielt!";
			}
			else
			{
				//Ts3Client.SendPrivateMessage(Ts3Client.WhoAmI().UnwrapThrow().Uid, "No matching audio file found.");
				return "Nichts gefunden!";
			}
		}

		[Command("datei")]
		public static async Task<string> CommandDatei(PlayManager playManager, ClientCall invoker, string query)
		{
			var file = Directory.GetFiles("mp3", $"*{query}*", SearchOption.TopDirectoryOnly).FirstOrDefault();
			if (file != null)
			{
				var filename = Path.GetFileName(file);
				Console.WriteLine("File found:" + filename + " Path: " + file);
				await playManager.Play(invoker, filename);
				return filename + " gefunden und wird abgespielt!";
			}
			else
			{
				//Ts3Client.SendPrivateMessage(Ts3Client.WhoAmI().UnwrapThrow().Uid, "No matching audio file found.");
				return "Nichts gefunden!";
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
			playManager.PlaybackStopped -= Stop;
			tsFullClient.OnClientMoved -= OnBotMoved;
		}
	}
}

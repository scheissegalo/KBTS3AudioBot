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
			string currentTitle = await YouTube.getTitleFromUrl(playManager.CurrentPlayData.SourceLink);
			await ts3Client.SendChannelMessage("[b]"+currentTitle + "[/b] wird abgespielt");
			//await ts3Client.SendChannelMessage("Song wird abgespielt");
		}

		private async Task Stop(object sender, EventArgs e)
		{
			//if (lastName != null) await ts3Client.ChangeName(lastName);
		}

		private async void OnBotMoved(object sender, IEnumerable<ClientMoved> clients)
		{
			string helpMessage = @"Hallo! Um Musik ab zu spielen einfach mit dem Befehl:
[b]!play <dein link>[/b] oder [b]!yt <dein link>[/b] - Achtung der Song wird direkt abgespielt!
Willst du statdessen das der Song an die aktuelle Playliste angehangen wird benutze:
[b]!add <dein link>[/b]

[b]!help[/b] für eine Ausführliche Hilfe.";
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

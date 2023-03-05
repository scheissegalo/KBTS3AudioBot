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

namespace whatIsPlaying
{
	public class nowPlaying : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		//private readonly ConfBot config;

		// Your dependencies will be injected into the constructor of your class.
		public nowPlaying(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		//const string NowPlayingTag = " - Wiedergabe";
		//string lastName = null;

		// The Initialize method will be called when all modules were successfully injected.
		public void Initialize()
		{
			playManager.AfterResourceStarted += Start;
			playManager.PlaybackStopped += Stop;
			setChannelCommander();
		}

		private async void setChannelCommander()
		{
			await ts3Client.SetChannelCommander(true);
		}

		private async Task Start(object sender, EventArgs e)
		{
			//Console.WriteLine("Start Playing!");
			//Console.WriteLine(playManager.CurrentPlayData.SourceLink);
			//var me = await tsFullClient.WhoAmI();
			var self = serverView.OwnClient;
			//self.Value
			//ts3Client.GetClientInfoById()
			//var myself = await tsFullClient.ClientInfo(tsFullClient.ClientId);
			//var self = serverView.OwnClient;
			//if (self == null) return;
			//lastName = "Mr. Music";
			
			//await ts3Client.ChangeName(lastName.EndsWith(NowPlayingTag) ? lastName : lastName + NowPlayingTag);
			string currentTitle = await YouTube.getTitleFromUrl(playManager.CurrentPlayData.SourceLink);
			await ts3Client.SendChannelMessage("[b]"+currentTitle + "[/b] wird abgespielt");
			//await ts3Client.SendChannelMessage("Song wird abgespielt");
		}

		private async Task Stop(object sender, EventArgs e)
		{
			//if (lastName != null) await ts3Client.ChangeName(lastName);
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
		}
	}
}

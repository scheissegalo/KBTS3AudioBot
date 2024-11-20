// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TS3AudioBot.Config;
using System.Threading.Tasks;
//using Org.BouncyCastle.Asn1;

namespace AloneMode
{
	public class Alone : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private ConfBot config;
		private ulong botDefaultChannel;

		public Alone(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, ConfBot configs)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
			this.config = configs;
		}

		public void Initialize()
		{
			string cs = config.Connect.Channel.ToString();
			Console.WriteLine("Config: " + cs);
			int newcs = Int32.Parse(cs.Replace("/", ""));
			Console.WriteLine(newcs.ToString());
			botDefaultChannel = (ulong)newcs;
			ts3Client.OnAloneChanged += OnAloneChanged;
		}

		private async Task OnAloneChanged(object sender, AloneChanged args)
		{
			if (args.Alone)
			{
				var me = await tsFullClient.WhoAmI();
				//Console.WriteLine("Alone Changed: " + args.Alone.ToString());

				if (botDefaultChannel == ((uint)me.Value.ChannelId.Value))
				{
					//Console.WriteLine("Already in Channel");
				}
				else
				{
					//Console.WriteLine("Changing to default channel");
					ChannelId channelId = new ChannelId(botDefaultChannel);
					await ts3Client.MoveTo(channelId);
				}
			}
		}

		public void Dispose()
		{
			ts3Client.OnAloneChanged -= OnAloneChanged;
		}
	}
}

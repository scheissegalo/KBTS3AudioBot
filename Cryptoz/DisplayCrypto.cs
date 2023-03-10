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
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Net.Http;

namespace Cryptoz
{
	public class DisplayCrypto : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		// endpoints
		private string BTC = "http://north-industries.com/getcry.php?cry=BTC";
		private readonly ulong BTCchannel = 267; // replace with the ID of the channel to update

		private string ETH = "http://north-industries.com/getcry.php?cry=ETH";
		private readonly ulong ETHchannel = 268; // replace with the ID of the channel to update

		private string Gold = "http://north-industries.com/getcry.php?cry=GOLD";
		private readonly ulong GOLDchannel = 269; // replace with the ID of the channel to update

		private readonly int UpdateInterval = 90; //min

		public DisplayCrypto(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public void Initialize()
		{
			StartLoop();
		}

		private async void StartLoop()
		{
			int update = UpdateInterval;
			while (true)
			{
				//Console.WriteLine($"Tick: Update:{update}");
				if (update <= 0)
				{
					// Timer end
					GetBTC();
					GetETH();
					GetGold();
					update = UpdateInterval;
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}

		private async void GetBTC()
		{
			ChannelId channelId = new ChannelId(BTCchannel);
			HttpClient client = new HttpClient();

			string btcData = await client.GetStringAsync(BTC);

			await tsFullClient.ChannelEdit(channelId, name: btcData+" USD");
		}

		private async void GetETH()
		{
			ChannelId channelId = new ChannelId(ETHchannel);
			HttpClient client = new HttpClient();

			string ethData = await client.GetStringAsync(ETH);

			await tsFullClient.ChannelEdit(channelId, name: ethData + " USD");
		}

		private async void GetGold()
		{
			ChannelId channelId = new ChannelId(GOLDchannel);
			HttpClient client = new HttpClient();

			string goldData = await client.GetStringAsync(Gold);

			await tsFullClient.ChannelEdit(channelId, name: goldData + " EUR");
		}

		public void Dispose()
		{

		}
	}
}

using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System;
using Heijden.DNS;

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

		private readonly ulong ServerVotesChannel = 466; // 449 local | 466 Remote | replace with the ID of the channel to update

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
			//GetVotes();
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
					GetVotes();
					update = UpdateInterval;
				}

				update--;
				await Task.Delay(60000); // 60000 1 min
			}
		}


		private async void GetVotes()
		{
			using (var client = new WebClient())
			{
				string NewChannelName = "";
				string userVotesList = "";
				var responseVotes = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=detail&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA");
				dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
				int serverRank = dataVotes.rank;
				int serverVotes = dataVotes.votes;
				NewChannelName = "[cspacer1231]Server Rank: "+ serverRank +" | Votes: "+ serverVotes;
				//Console.WriteLine("Server Rank: {0}, Votes: {1}", serverRank, serverVotes);

				var response = client.DownloadString("https://teamspeak-servers.org/api/?object=servers&element=voters&key=s5b78c4OcL5UV6pDxTMnDeaMjNEotEUN6iA&month=current&format=json");
				dynamic data = JsonConvert.DeserializeObject(response);
				var voters = data.voters;

				foreach (var voter in voters)
				{
					string nickname = voter.nickname;
					int votes = voter.votes;
					//Console.WriteLine("Nickname: {0}, Votes: {1}", nickname, votes);
					userVotesList = userVotesList + nickname + " = "+ votes+"\n";
				}

				userVotesList = userVotesList + "\n\n[url=https://teamspeak-servers.org/server/12137/vote/]Vote for US[/url]";
				//Console.WriteLine(userVotesList);
				string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
				ChannelId channelId = new ChannelId(ServerVotesChannel);
				await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);

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

using System;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using System.Threading.Tasks;

namespace GameServer
{
	public class Empyrion : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		private string empyrionServerApiKey = "fEyVHKHbjxIUkad33XJaQ1PJoWJuOUUNLck"; // Replace with your API key
		private string empyrionServerID = "59988"; // ID of the server
		private readonly ulong empyrionChannel = 470; // replace with the ID of the channel to update
		private readonly int UpdateInterval = 10; //min

		// endpoints
		private string empyrionServerUrl;
		private string empyrionServerVotesUrl;

		public Empyrion(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}
		public void Initialize()
		{
			//GetVotes();
			empyrionServerVotesUrl = "https://empyrion-servers.com/api/?object=servers&element=votes&key="+ empyrionServerApiKey+ "&format=json";
			empyrionServerUrl = "https://empyrion-servers.com/api/?object=servers&element=detail&key="+empyrionServerApiKey;

			//And start the Timer
			StartLoop();
			GetVotes();
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

				var responseVotes = client.DownloadString(empyrionServerUrl);
				dynamic dataVotes = JsonConvert.DeserializeObject(responseVotes);
				int serverRank = dataVotes.rank;
				int serverVotes = dataVotes.votes;
				int players = dataVotes.players;
				int maxplayers = dataVotes.maxplayers;
				NewChannelName = "Player: "+ players + "/"+ maxplayers + " | Rank: " + serverRank + " | Votes: " + serverVotes;
				//Console.WriteLine("Server Rank: {0}, Votes: {1}", serverRank, serverVotes);

				var response = client.DownloadString(empyrionServerVotesUrl);
				dynamic data = JsonConvert.DeserializeObject(response);
				var voters = data.votes;

				foreach (var voter in voters)
				{
					string nickname = voter.nickname;
					int votes = voter.claimed;
					//Console.WriteLine("Nickname: {0}, Votes: {1}", nickname, votes);
					userVotesList = userVotesList + nickname + " = " + votes + "\n";
				}

				userVotesList = userVotesList + "\n\n[url=https://empyrion-servers.com/server/" + empyrionServerID + "/vote/]Vote for US[/url]";
				//Console.WriteLine(userVotesList);
				string newChanDis = $"[b]User Vote List:[/b]\n{userVotesList}";
				ChannelId channelId = new ChannelId(empyrionChannel);
				await tsFullClient.ChannelEdit(channelId, name: NewChannelName, description: newChanDis);

			}
		}

		public void Dispose()
		{

		}
	}
}

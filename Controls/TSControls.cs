using System;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using TSLib.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading.Channels;


namespace Controls
{
	public class TSControls : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		private readonly ulong channelToWatch = 482; // 438 - Local | 482 remote | replace with the ID of the channel to update
		private readonly ulong channelHome = 11; // replace with the ID of the channel to update
		private readonly string helptext = @"
To create a channel use the following commands:
[b][color=red]!create[/color] [color=blue]<name of the channel>[/color][/b]
[b]Example:[/b] [color=red]!create[/color] [color=blue]Mikes CSRoom[/color]
Will create a channel called Mikes CSRoom.";
		private readonly string msgFoot = @"

-----------------------------------------

Dein [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] - Secure Gaming Services
[URL=https://north-industries.com]Home[/URL] | [URL=https://north-industries.com/ts-viewer/]TS-Viewer[/URL] | [URL=north-industries.com/ts-invites/#regeln]Guidelines/Help[/URL]";

		public TSControls(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public void Initialize()
		{
			tsFullClient.OnClientMoved += OnUserMoved;
		}

		private void OnUserMoved(object sender, IEnumerable<ClientMoved> e)
		{
			
			foreach (var user in e)
			{
				//var clientID = getUserID(user.ClientId);
				//var clientID = tsFullClient.GetClientUidFromClientId(user.ClientId);
				//Console.WriteLine("User: "+ user.ClientId + " moved to channel: " + user.TargetChannelId);
				ChannelId channelId = new ChannelId(channelToWatch);
				
				if (user.ClientId != tsFullClient.ClientId && user.TargetChannelId == channelId)
				{
					// User in "create" channel
					//Console.WriteLine("In Channel");
					ts3Client.MoveTo(channelId);
					ts3Client.SendChannelMessage(helptext+msgFoot);
				}

				CheckIfChannelEmpty(channelId);
			}
		}

		private async void CheckIfChannelEmpty(ChannelId channelId)
		{
			var clientList = await tsFullClient.ClientList();
			var clientsInChannel = clientList.Value.Count(client => client.ChannelId.Value == channelId.Value);

			if (clientsInChannel == 1)
			{
				var singleClientInChannel = clientList.Value.SingleOrDefault(client => client.ChannelId.Value == channelId.Value);

				if (singleClientInChannel != null)
				{
					// Execute your code for the single client in the channel
					await ts3Client.MoveTo((ChannelId)channelHome);
					//Console.WriteLine("Client: " + singleClientInChannel.ClientId + " is in channel: " + singleClientInChannel.ChannelId.Value);
				}
			}

		}

		public async void CreateChannelForUser(ClientCall invoker, string channelName, string password)
		{
			var response = await tsFullClient.ChannelCreate(channelName, channelName,"Type your topic here", channelName, password, codec: Codec.OpusVoice, parent: (ChannelId)68, type: ChannelType.Permanent);

			if (!response.Ok)
			{
				//Console.WriteLine("error while creating channel: " + response.Error.ErrorFormat());
				await ts3Client.SendChannelMessage("error while creating channel: " + response.Error.ErrorFormat() + msgFoot);
				return;
			}
			else
			{
				var chanlist = await tsFullClient.ChannelList();
				foreach (var channel in chanlist.Value)
				{
					if (channel.Name == channelName)
					{
						await tsFullClient.ClientMove(invoker.ClientId.Value, channel.ChannelId);
						ChannelGroupId groupId = new ChannelGroupId(5); // replace with the ID of the channel group to add the client to

						await tsFullClient.ChannelGroupAddClient(groupId, channel.ChannelId, invoker.DatabaseId.Value);

						await tsFullClient.PokeClient("Your channel " + channelName + " is ready. Password: " + password, invoker.ClientId.Value);
						await tsFullClient.PokeClient("Change by right click on your channel, choose edit channel.", invoker.ClientId.Value);
						await tsFullClient.PokeClient("Happy Gaming!", invoker.ClientId.Value);

						CheckIfChannelEmpty((ChannelId)channelToWatch);
					}
				}
			}

		}

		[Command("create")]
		public string CreateChannel(ClientCall invoker, string channelName)
		{
			Random random = new Random();
			// Generate a random 4-digit number
			int password = random.Next(1000, 10000);
			// Pass necessary parameters to the static method
			CreateChannelForUser(invoker, channelName, password.ToString());
			//var clientID = tsFullClient.GetClientIds(invoker.ClientUid);
			//if (invoker.ClientUid)
			//string response = AddOrUpdateUserInDatabase(invoker.ClientUid.Value, steamid);
			return "[b][color=red]User (" + invoker.NickName + ") created channel (" + channelName + "). Password: "+ password + "[/color][/b]";
		}

		public void Dispose()
		{
			tsFullClient.OnClientMoved -= OnUserMoved;
		}


	}
}

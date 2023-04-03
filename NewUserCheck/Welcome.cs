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

namespace NewUserCheck
{
	public class Welcome : IBotPlugin
	{

		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		public Welcome(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public void Initialize()
		{
			tsFullClient.OnClientEnterView += OnUserConnected;
			//tsFullClient.OnClientLeftView += OnUserDisconnected;
			//ResetCountPeriodically();
			//lastResetTime = DateTime.UtcNow;
			//CheckOnlineUsers(true);

			//var userData = GetTsUserData();
			//foreach (TSuserDB data in userData)
			//{
			//	Console.WriteLine($"OLD Data Deleted: Id: {data.Id}, Name: {data.Name}, Age: {data.ID}");
			//}
			//TSuserDB newData = new TSuserDB();
			//TSuserDB.DeleteAllData(jsonFilePath);

		}

		private async Task<string> CheckAdminsOnline(ushort cID)
		{
			string allAdmins = "";
			try
			{
				var allUsers = await tsFullClient.ClientList();
				foreach (var tsUser in allUsers.Value)
				{
					var fulluser = await tsFullClient.ClientInfo(tsUser.ClientId);

					//Console.WriteLine("Lient ID send: " + cID + " | Client id Get: " + tsUser.ClientId.Value);

					if (fulluser.Value.ClientType.Equals(ClientType.Full) && cID != tsUser.ClientId.Value)
					{
						bool isBot = false;
						foreach (var sg in fulluser.Value.ServerGroups)
						{
							if (sg.Value == 11)
							{
								// Is Bot
								//allAdmins = "Niemand Online";
								isBot = true;
								continue;
							}
						}
						if (!isBot)
						{
							allAdmins = allAdmins + " - " + fulluser.Value.Name + "\n";
						}

						//return allAdmins;

					}
				}
			}
			catch
			{
				return "Error";
			}

			return allAdmins;
		}

		private async void OnUserConnected(object sender, IEnumerable<ClientEnterView> clients)
		{
			foreach (var user in clients)
			{
				string allAdmins = await CheckAdminsOnline(user.ClientId.Value);
				if (user.ClientType.Equals(ClientType.Full))
				{
					foreach (var sg in user.ServerGroups)
					{
						if (sg.Value == 8 || sg.Value == 23)
						{
							//Console.WriteLine("Fresh Meat or Level 1 User");
							await ts3Client.SendMessage(@"
Willkommen auf [b][color=#24336b]K[/color][color=#0095db]B[/color][/b] - [color=#24336b]Teamspeak[/color][color=#0095db]Server[/color]. 
Hier wird gezockt, diskutiert und viel gelacht. Da wir in letzter Zeit oft Besuch von Trolls bekommen die sehr laut irgendwelche
sound Dateien abspielen, sind wir gezwungen alle neuen Benutzer erstmal genau unter die Lupe zu nehmen. Schreib dazu einfach Jemand an.

[b][color=red]Um in einer der Kanäle zu gelangen musst du jemand der Online ist anschreiben.[/color][/b]
Momentan Online:
"+ allAdmins + @"

[URL=https://klabausterbeere.xyz]Home[/URL] | [URL=https://klabausterbeere.xyz/ts-viewer/]TS-Viewer[/URL] | [URL=https://klabausterbeere.xyz/ts-invites/#regeln]Regeln[/URL] | [URL=https://sinusbot.klabausterbeere.xyz/]Sinusbot GUI[/URL] | [URL=https://klabausterbeere.xyz/kb-chat-free/]KB-Chat[/URL] | [URL=https://meet.klabausterbeere.xyz/]KB-Meet[/URL]

[b]SHARE LINK: [URL]https://klabausterbeere.xyz/ts-invites[/URL][/b]

!!! WICHTIG !!!! Hallo Frischling, bitte sieh dir die Nachricht im Chatfenter an!", user.ClientId);
							await tsFullClient.PokeClient("Hallo Frischling, bitte sieh dir die Nachricht im Chatfenter an!", user.ClientId);

						}
						//Console.WriteLine("Server Group: "+ sg.Value.ToString());
					}
					//Console.WriteLine(clients); 8 23
					//CheckOnlineUsers(true);
					//try
					//{
					//	var allUsers = await tsFullClient.ClientList();
					//	foreach (var tsUser in allUsers.Value)
					//	{
					//		var fulluser = await tsFullClient.ClientInfo(user.ClientId);

					//		if (fulluser.Value.ClientType.Equals(ClientType.Full))
					//		{

					//		}
					//	}
					//}
					//catch
					//{

					//}
				}
				else
				{
					//DateTime now = DateTime.Now;
					//Console.WriteLine(now + " | " +user.Name + " Query");
					continue;
				}
			}
		}

		//private void OnUserDisconnected(object sender, IEnumerable<ClientLeftView> clients)
		//{
		//	//CheckOnlineUsers(false);

		//	foreach (var client in clients)
		//	{
		//		//client.
		//	}
		//}



		public void Dispose()
		{
			tsFullClient.OnClientEnterView -= OnUserConnected;
			//tsFullClient.OnClientLeftView -= OnUserDisconnected;
		}
	}
}

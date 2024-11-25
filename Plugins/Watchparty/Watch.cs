// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
//using RankingSystem;
using System.Text.RegularExpressions;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib;
using TSLib.Full;
using TSLib.Messages;

namespace Watchparty
{
	public class Watch : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		//private Constants constants = new Constants();
		private static readonly HttpClient client = new HttpClient();
		private const string HOST_NAME = "https://w.karich.design";
		private const string API_NAME = "https://w.karich.design";
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public readonly string messageHeader = @$"
[b]══════════════════════════════════════════════[/b]
[b]  [color=#24336b]███[/color][color=#0095db]██[/color]  [color=#24336b]North[/color][color=#0095db]Industries[/color] - Free Secure Gaming Services  [color=#0095db]██[/color][color=#24336b]███[/color]  [/b]
[b]══════════════════════════════════════════════[/b]

";
		public readonly string messageFooter = $@"

[b]══════════════════════════════════════════════[/b]
[b][url=https://north-industries.com]HOME[/url] | [url=https://north-industries.com/news/]NEWS[/url] | [url=https://north-industries.com/teamspeak-connect/#rules]RULES[/url] | [url=https://north-industries.com/teamspeak-help]HELP[/url] | [url=https://teamspeak-servers.org/server/12137/vote/]VOTE[/url] | [url=https://north-industries.com/ts-viewer/]TS-VIEWER[/url] | [url=https://north-industries.com/teamspeak-connect/]SHARE[/url][/b]

[b]Need help? Just type ""[color=#00FF00]help[/color]""[/b]

[color=#24336b]North[/color][color=#0095db]Industries[/color] [i]""Your Gaming Journey Starts Here!""[/i]
";

		public Watch(Ts3Client ts3Client, TsFullClient tsFull)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
		}


		// This method will create a WatchParty room and return the URL
		public static async Task<string> CreateWatchPartyRoomAsync(string video = "")
		{
			//Log.Info($"Try parse video: {video}");

			var requestData = new
			{
				video = video
			};

			var requestBody = new StringContent(JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");

			try
			{
				// Send a POST request to create a new WatchParty room
				var response = await client.PostAsync(API_NAME + "/createRoom", requestBody);

				// Check if the request was successful
				if (response.IsSuccessStatusCode)
				{
					// Parse the response JSON to get the room name
					var responseBody = await response.Content.ReadAsStringAsync();
					var responseData = JsonConvert.DeserializeObject<dynamic>(responseBody);
					string roomName = responseData?.name ?? "error";

					// Generate the room URL
					string roomUrl = HOST_NAME + "/watch" + roomName;

					Log.Info($"New WatchParty room was created: {roomUrl}");
					return $"[b][color=red]Created a new WatchParty room[/color][/b]!\n{roomUrl}";
				}
				else
				{
					//Log.Error($"An error occurred: {ex.Message}");
					return "Failed to create a WatchParty room. Please try again.";
				}
			}
			catch (Exception ex)
			{
				// Handle any exceptions
				Log.Error($"An error occurred: {ex.Message}");
				return $"An error occurred: {ex.Message}";
			}
		}

		private async Task<bool> SendPrivateMessage(string message, ClientId client, bool format = false)
		{
			if (format)
			{
				message = $@"[b][color=red]{message}[/color][/b]";
			}

			string formattetMessage = $@"{messageHeader} {message} {messageFooter}";
			var result = await tsFullClient.SendPrivateMessage(formattetMessage, client);
			await tsFullClient.SendChannelMessage(formattetMessage);

			if (result.Ok)
			{
				return true;
			}

			return false;
		}

		//[Command("watch")]
		//public async static Task<string> GenerateWatchLink(string query = "empty")
		//{
		//	string result = "Error!";
		//	var pattern = @"\[URL\](.+?)\[/URL\]";
		//	var match = Regex.Match(query, pattern);

		//	if (match.Success)
		//	{
		//		Log.Info("Match success!");
		//		var url = match.Groups[1].Value;        // Extract the URL
		//		var displayText = match.Groups[2].Value; // Extract the display text
		//		result = await CreateWatchPartyRoomAsync(url);                                 //return (url, displayText)
		//	}
		//	else
		//	{
		//		Log.Info("No match, calling without args");
		//		result = await CreateWatchPartyRoomAsync();
		//	}

		//	return $"{result}";
		//}

		public void Initialize()
		{
			//fix because unable to have arguments optional in commands
			tsFullClient.OnEachTextMessage += TsFullClient_OnEachTextMessage;

			Log.Info("Watchparty - Initialized");
		
		}

		private async void TsFullClient_OnEachTextMessage(object? sender, TextMessage e)
		{
			// fix because no OPTIONAL subcommands possible!
			if (tsFullClient.ClientId == e.InvokerId)
			{
				return;
			}
			string[] parts = e.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string baseCommand = parts[0].ToLower();

			if (baseCommand == "!watch" || baseCommand == "watch" || baseCommand == "w" || baseCommand == "!w")
			{
				string result = "Error!";
				if (parts.Length > 1)
				{
					//Log.Info($"Valid Command: {baseCommand} Subcommand:{parts[1]}");

					var pattern = @"\[URL\](.+?)\[/URL\]";
					var match = Regex.Match(parts[1], pattern);
					if (match.Success)
					{
						var url = match.Groups[1].Value;
						result = await CreateWatchPartyRoomAsync(url);
						await SendPrivateMessage(result, e.InvokerId, true);
					}
					else
					{
						result = await CreateWatchPartyRoomAsync();
						await SendPrivateMessage(result, e.InvokerId, true);
					}

					
				}
				else
				{
					result = await CreateWatchPartyRoomAsync();
					await SendPrivateMessage(result, e.InvokerId, true);
				}
			}
		}

		public void Dispose()
		{
			tsFullClient.OnEachTextMessage -= TsFullClient_OnEachTextMessage;
		}

	}
}

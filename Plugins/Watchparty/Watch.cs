// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full;

namespace Watchparty
{
	public class Watch : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		private static readonly HttpClient client = new HttpClient();
		private const string HOST_NAME = "https://w.karich.design";
		private const string API_NAME = "https://w.karich.design";

		public Watch(Ts3Client ts3Client, TsFullClient tsFull)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
		}

		// This method will create a WatchParty room and return the URL
		public static async Task<string> CreateWatchPartyRoomAsync(string video = "")
		{
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

					return $"[b][color=red]Created a new WatchParty room[/color][/b]{(video != null ? $" with video {video}" : string.Empty)}!\n{roomUrl}";
				}
				else
				{
					return "Failed to create a WatchParty room. Please try again.";
				}
			}
			catch (Exception ex)
			{
				// Handle any exceptions
				return $"An error occurred: {ex.Message}";
			}
		}

		[Command("watch")]
		public async static Task<string> GenerateWatchLink()
		{
			string result = await CreateWatchPartyRoomAsync();

			return $"{result}";
		}

		public void Initialize()
		{

		}

		public void Dispose()
		{

		}

	}
}

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerVotes
{
	internal class SteamQuery
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private static readonly string apiKey = "1E0D3783986A81844EC1B7FF2961E334"; // Your Steam API key
		private static readonly string serverIP = "152.53.64.213:2457"; //Valheim


		public static async Task QueryServer()
		{
			try
			{
				string url = $"https://api.steampowered.com/IGameServersService/GetServerList/v1/?key={apiKey}&filter=addr\\{serverIP}";

				HttpResponseMessage response = await httpClient.GetAsync(url);
				response.EnsureSuccessStatusCode();

				string responseBody = await response.Content.ReadAsStringAsync();
				using JsonDocument doc = JsonDocument.Parse(responseBody);
				JsonElement root = doc.RootElement.GetProperty("response").GetProperty("servers")[0];

				// Extracting the details
				string name = root.GetProperty("name").GetString();
				int players = root.GetProperty("players").GetInt32();
				int maxPlayers = root.GetProperty("max_players").GetInt32();
				string map = root.GetProperty("map").GetString();
				string version = root.GetProperty("version").GetString();

				Console.WriteLine($"Server Name: {name}");
				Console.WriteLine($"Players: {players}/{maxPlayers}");
				Console.WriteLine($"Map: {map}");
				Console.WriteLine($"Version: {version}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error fetching server data: {ex.Message}");
			}
		}
	}
}

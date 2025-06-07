using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace ServerVotes
{
    class EmpyrionQuery
    {
		// Regular expression pattern to capture key-value pairs
		private static readonly Regex playerInfoRegex = new Regex(
			@"id=(\d+)\s+name=([^\s]+)\s+fac=\[([^\]]+)\]\s+role=([^\s]+)(?:\s+online=(\d+))?",
			RegexOptions.Compiled);

		private static readonly Regex playerOnlineInfoRegex = new Regex(
			@"(?<=\bid=)(\d+)\s+name=([\w\s]+)\s+fac=\[([\w\s]+)\]\s+role=([\w]+)(?:\s+online=(\d+))?",
			RegexOptions.Compiled);

		public async Task<ServerDataModel> GetPlayers()
		{
			string serverIp = "176.57.153.77";
			int serverPort = 8180;
			string password = "j182iG1QtnSbQCm"; // Replace with the actual password
			try
			{
				using (TcpClient client = new TcpClient())
				{
					// Connect asynchronously
					await client.ConnectAsync(serverIp, serverPort);

					using (NetworkStream stream = client.GetStream())
					using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
					using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
					{
						//Console.WriteLine("Connected to server.");

						// Read initial prompt and send the password
						await reader.ReadLineAsync();
						await writer.WriteLineAsync(password);

						// Wait for successful login
						string response;
						while ((response = await reader.ReadLineAsync()) != null)
						{
							//Console.WriteLine(response);
							if (response.Contains("Logged in successfully"))
								break;
						}

						// Send 'plys' command
						await writer.WriteLineAsync("plys");

						ServerDataModel serverData = await ParseServerResponseAsync(reader);

						// Print results


						// Quit
						await writer.WriteLineAsync("quit");
						//Console.WriteLine("Disconnected from server.");
						return serverData;
						//return new ServerDataModel();

					}
				}


			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
			return new ServerDataModel ();
		}

		//private void getData()
		//{

		//}

		private static async Task<ServerDataModel> ParseServerResponseAsync(StreamReader reader)
		{
			var serverData = new ServerDataModel();
			string line;
			var lastLineTime = DateTime.Now;
			var timeout = TimeSpan.FromSeconds(5); // Adjust timeout as needed

			while ((line = await reader.ReadLineAsync()) != null)
			{
				lastLineTime = DateTime.Now;
				//Console.WriteLine($"MainLine: {line}");

				if (string.IsNullOrWhiteSpace(line))
					continue;

				if (line.StartsWith("Players connected"))
				{
					int connectedCount = int.Parse(line.Split('(', ')')[1]);
					if (connectedCount > 0)
					{
						await reader.ReadLineAsync(); // Skip column headers
						await reader.ReadLineAsync(); // Skip separator line

						while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
						{
							var parts = line.Split(',', StringSplitOptions.TrimEntries);
							if (parts.Length >= 4)
							{
								serverData.ConnectedPlayers.Add(new OnlinePlayerModel
								{
									Id = int.Parse(parts[0].Split(':')[1]),
									Name = parts[1],
									PlayfieldName = parts[2],
									IPAddress = parts[3]
								});
							}
						}
					}
				}
				else if (line.StartsWith("Global online players list:"))
				{
					while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
					{
						var match = playerInfoRegex.Match(line);
						if (match.Success)
						{
							serverData.GlobalOnlinePlayers.Add(new PlayerModel
							{
								Id = int.Parse(match.Groups[1].Value),
								Name = match.Groups[2].Value.Trim(),
								Faction = match.Groups[3].Value.Trim(),
								Role = match.Groups[4].Value.Trim(),
								OnlineTime = match.Groups[5].Success ? long.Parse(match.Groups[5].Value) : 0
							});
						}
					}
				}
				else if (line.StartsWith("Global players list:"))
				{
					while ((line = await reader.ReadLineAsync()) != null)
					{
						if (line.StartsWith("INFO:"))
							break;

						var match = playerInfoRegex.Match(line);
						if (match.Success)
						{
							serverData.GlobalPlayers.Add(new PlayerModel
							{
								Id = int.Parse(match.Groups[1].Value),
								Name = match.Groups[2].Value,
								Faction = match.Groups[3].Value,
								Role = match.Groups[4].Value,
								OnlineTime = match.Groups[5].Success ? long.Parse(match.Groups[5].Value) : 0
							});
						}

						if ((DateTime.Now - lastLineTime) > timeout)
							return serverData;
					}
				}
				else if (line.StartsWith("INFO:"))
				{
					return serverData;
				}

				if ((DateTime.Now - lastLineTime) > timeout)
				{
					return serverData;
				}
			}

			return serverData;
		}

		static ServerDataModel ParseServerResponse(StreamReader reader)
		{
			//Console.WriteLine("Parsing data");
			var serverData = new ServerDataModel();
			string line;
			var lastLineTime = DateTime.Now;
			var timeout = TimeSpan.FromSeconds(5); // Adjust timeout as needed

			while ((line = reader.ReadLine()) != null)
			{
				//Console.WriteLine($"MainLine: {line}");

				// Update the time of the last received line
				lastLineTime = DateTime.Now;

				// Skip empty lines
				if (string.IsNullOrWhiteSpace(line))
					continue;

				if (line.StartsWith("Players connected"))
				{
					//Console.WriteLine($"Players found");

					int connectedCount = int.Parse(line.Split('(', ')')[1]);
					if (connectedCount > 0)
					{
						//Console.WriteLine($"Reading Connected");

						reader.ReadLine(); // Skip column headers
						reader.ReadLine(); // Skip separator line

						while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
						{
							var parts = line.Split(',', StringSplitOptions.TrimEntries);
							if (parts.Length >= 4) // Ensure we have all required parts
							{
								serverData.ConnectedPlayers.Add(new OnlinePlayerModel
								{
									Id = int.Parse(parts[0].Split(':')[1]),
									Name = parts[1],
									PlayfieldName = parts[2],
									IPAddress = parts[3]
								});
							}
						}
					}
				}
				else if (line.StartsWith("Global online players list:"))
				{
					//Console.WriteLine($"Global Players found");
					//playerOnlineInfoRegex

					while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
					{
						//Console.WriteLine($"Line: {line}");

						//var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
						var match = playerInfoRegex.Match(line);

						if (match.Success)
						{
							serverData.GlobalOnlinePlayers.Add(new PlayerModel
							{
								Id = int.Parse(match.Groups[1].Value),
								Name = match.Groups[2].Value.Trim(),
								Faction = match.Groups[3].Value.Trim(),
								Role = match.Groups[4].Value.Trim(),
								OnlineTime = match.Groups[5].Success ? long.Parse(match.Groups[5].Value) : 0
							});
						}
						else
						{
							Console.WriteLine("Line did not match expected format.");
						}
					}
				}
				else if (line.StartsWith("Global players list:"))
				{
					//Console.WriteLine($"Global Players list found");

					var players = new List<PlayerModel>();

					while ((line = reader.ReadLine()) != null)
					{
						//Console.WriteLine($"Parsing {line}");

						if (line.StartsWith("INFO:"))
						{
							//Console.WriteLine("Encountered INFO line, exiting loop.");
							break;
						}

						var match = playerInfoRegex.Match(line);
						if (match.Success)
						{
							serverData.GlobalPlayers.Add(new PlayerModel
							{
								Id = int.Parse(match.Groups[1].Value),
								Name = match.Groups[2].Value,
								Faction = match.Groups[3].Value,
								Role = match.Groups[4].Value,
								OnlineTime = match.Groups[5].Success ? long.Parse(match.Groups[5].Value) : 0
							});
						}
						if ((DateTime.Now - lastLineTime) > timeout)
						{
							//Console.WriteLine("Timeout reached while parsing data. Ending response parsing.");
							return serverData;
						}
					}
				}
				else if (line.StartsWith("INFO:")) // Skip server info lines
				{
					return serverData;

				}

				// Exit the loop if timeout occurs
				if ((DateTime.Now - lastLineTime) > timeout)
				{
					//Console.WriteLine("Timeout reached while parsing data. Ending response parsing.");
					return serverData;
				}
			}

			return serverData;
		}

	}

	// Models
	public class PlayerModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Faction { get; set; }
		public string Role { get; set; }
		public long OnlineTime { get; set; }
	}

	public class OnlinePlayerModel : PlayerModel
	{
		public string PlayfieldName { get; set; }
		public string IPAddress { get; set; }
	}

	public class ServerDataModel
	{
		public List<OnlinePlayerModel> ConnectedPlayers { get; set; } = new();
		public List<PlayerModel> GlobalOnlinePlayers { get; set; } = new();
		public List<PlayerModel> GlobalPlayers { get; set; } = new();
	}
}

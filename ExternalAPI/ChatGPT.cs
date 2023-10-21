using System;
using System.IO;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib;
using TSLib.Full;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TS3AudioBot.CommandSystem;

namespace ExternalAPI
{
	public class ChatGPT : IBotPlugin
	{
		private TsFullClient tsFullClient;
		//private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		private static string apiKey = "no key";

		public ChatGPT(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
		}

		public void Initialize()
		{
			// Specify the path to your text file
			string filePath = "api_key.txt";

			try
			{
				// Read the API key from the text file
				apiKey = System.IO.File.ReadAllText(filePath);

				// Now you can use the apiKey as needed
				//Console.WriteLine("API Key: " + apiKey);
			}
			catch (IOException e)
			{
				Console.WriteLine("Error reading the file: " + e.Message);
			}
		}

		[Command("gpt")]
		public static string CommandGPT(string prompt)
		{
			var openAIApiClient = new OpenAIApiClient(apiKey);
			var model = "gpt-4"; // E.g., "text-davinci-002"

			Task<string> responseTask = Task.Run(() => openAIApiClient.SendPrompt(prompt, model));
			responseTask.Wait(); // This blocks the current thread until the task completes.

			string response = responseTask.Result;

			JObject jsonResponse = JObject.Parse(response);

			JArray choices = jsonResponse["choices"] as JArray;
			if (choices != null && choices.Count > 0)
			{
				var firstChoice = choices[0];
				if (firstChoice != null)
				{
					var message = firstChoice["message"];
					if (message != null)
					{
						var content = message["content"];
						if (content != null)
						{
							string answer = content.ToString();
							return $"[b][color=#24336b]{answer}[/color][/b]";
						}
					}
				}
			}

			return $"[b][color=red]No answer received![/color][/b]";

		}


		public void Dispose()
		{

		}

	}


	public class OpenAIApiClient
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private readonly string _gpt35TurboEndpoint = "https://api.openai.com/v1/chat/completions";

		public OpenAIApiClient(string apiKey)
		{
			_apiKey = apiKey;
			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
		}

		public async Task<string> SendPrompt(string prompt, string model)
		{
			// Modify the request body to include the model name
			//Console.WriteLine("Sending Prompt: "+prompt+" | Model name: "+model);
			var requestBody = new
			{
				messages = new[]
				{
				new
				{
					role = "system",
					content = "You are a helpful assistant."
				},
				new
				{
					role = "user",
					content = prompt
				}
			},
				model = model // Add the model name here
			};

			var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody);
			var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(_gpt35TurboEndpoint, content);

			response.EnsureSuccessStatusCode();

			var responseBody = await response.Content.ReadAsStringAsync();
			//Console.WriteLine("Response: " + responseBody);
			return responseBody;
		}
	}
}

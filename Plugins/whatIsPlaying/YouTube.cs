using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NLog.LayoutRenderers.Wrappers.ReplaceLayoutRendererWrapper;

namespace whatIsPlaying
{
	internal class YouTube
	{
		public static async Task<string> getTitleFromUrl(string videoUrl)
		{
			//string videoId = GetVideoIdFromUrl(videoUrl);
			//string videoPageUrl = $"https://www.youtube.com/watch?v={videoId}";
			//Console.WriteLine("Video URL: "+videoUrl);
			string returnValue = "Not Found";


			//string url = "https://youtu.be/HlI_C5hpcsk"; // replace with the actual video or track URL

			string title = await GetTitleFromUrl(videoUrl);

			returnValue = title;

			return returnValue;
		}
		static async Task<string> GetTitleFromUrl(string url)
		{
			if (IsYouTubeUrl(url))
			{
				string videoId = GetVideoIdFromUrl(url);
				string videoPageUrl = $"https://www.youtube.com/watch?v={videoId}";

				using (var client = new HttpClient())
				{
					var response = await client.GetAsync(videoPageUrl);
					var content = await response.Content.ReadAsStringAsync();

					var regex = new Regex("<title>(.*?)</title>");
					var match = regex.Match(content);

					if (match.Success)
					{
						string title = match.Groups[1].Value.Trim();
						// Remove unwanted text at beginning and end of title
						title = title.Replace(" - YouTube", "").Replace("&#39;", "'").Trim();
						return "You[color=red]Tube[/color]: " + title;
					}
				}
			}
			else if (IsSoundCloudUrl(url))
			{
				string title = await GetTitleFromSoundCloudUrl(url);

				return "SoundCloud: Kein Titel!";

			}

			throw new ArgumentException("Invalid YouTube or SoundCloud URL");
		}

		static bool IsYouTubeUrl(string url)
		{
			var uri = new Uri(url);
			return uri.Host.ToLower() == "youtu.be" || uri.Host.ToLower() == "www.youtube.com";
		}

		static bool IsSoundCloudUrl(string url)
		{
			var uri = new Uri(url);
			return uri.Host.ToLower() == "soundcloud.com";
		}

		static string GetVideoIdFromUrl(string url)
		{
			var uri = new Uri(url);
			if (uri.Host.ToLower() == "youtu.be")
			{
				var parts = uri.AbsolutePath.Trim('/').Split('/');
				return parts[parts.Length - 1];
			}
			else if (uri.Host.ToLower() == "www.youtube.com")
			{
				var query = uri.Query.TrimStart('?');
				var parameters = query.Split('&');

				foreach (var parameter in parameters)
				{
					var parts = parameter.Split('=');
					if (parts.Length == 2 && parts[0].ToLower() == "v")
					{
						return parts[1];
					}
				}
			}

			throw new ArgumentException("Invalid YouTube video URL");
		}

		static async Task<string> GetTrackIdFromUrl(string url)
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(url);
				var content = await response.Content.ReadAsStringAsync();

				var regex = new Regex(@"data-sc-track=""(\d+)""");
				var match = regex.Match(content);

				if (match.Success)
				{
					return match.Groups[1].Value;
				}
			}

			throw new ArgumentException("Invalid SoundCloud track URL");
		}

		static async Task<string> GetTitleFromSoundCloudUrl(string url)
		{
			//Console.WriteLine("Get title from: " + url);
			var httpClient = new HttpClient();
			var html = await httpClient.GetStringAsync(url);

			var titleMatch = Regex.Match(html, @"<title>([^<]*)</title>");
			if (titleMatch.Success)
			{
				return titleMatch.Groups[1].Value.Trim();
			}

			throw new ArgumentException("Invalid SoundCloud track URL");
		}

		class SoundCloudTrackMetadata
		{
			public string title { get; set; }
			// add any additional metadata properties you want to retrieve here
		}

	}
}

using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace whatIsPlaying
{
	internal class YouTube
	{
		private static readonly HttpClient httpClient = new HttpClient();
		//API Key AIzaSyAFZHRQL7HQH6ZnqBHPmGpSFzCdTb_EOjc
		private static readonly string ApiKey = "AIzaSyAFZHRQL7HQH6ZnqBHPmGpSFzCdTb_EOjc"; // Replace with your YouTube API Key
		public static int requests = 0;

		public static async Task<string> GetTitleFromUrlAsync(string videoUrl)
		{
			requests++;
			string videoId = GetVideoIdFromUrl(videoUrl);
			if (videoId == null)
				throw new ArgumentException("Invalid YouTube video URL.");

			string apiUrl = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={ApiKey}&part=snippet,contentDetails";

			using (HttpClient client = new HttpClient())
			{
				var response = await client.GetStringAsync(apiUrl);
				var json = JObject.Parse(response);

				var contentDetails = json["items"]?[0]?["contentDetails"];
				var snippet = json["items"]?[0]?["snippet"];

				string title = (string)snippet?["title"];
				string duration = (string)contentDetails?["duration"];
				string durationFormatted = ParseYouTubeDuration(duration);

				string formattedOutput = $"[b][color=black]YOU[/color][color=red]TUBE[/color][/b] - {title} | Duration: {durationFormatted} - [url={videoUrl}](LINK)[/url]";
				return formattedOutput ?? "Title Not Found";
			}
		}

		public static string ParseYouTubeDuration(string duration)
		{
			var parsedDuration = XmlConvert.ToTimeSpan(duration);
			return $"{parsedDuration.Hours:D2};{parsedDuration.Minutes:D2};{parsedDuration.Seconds:D2}";
		}

		private static string GetVideoIdFromUrl(string url)
		{
			var uri = new Uri(url);
			if (uri.Host.Contains("youtu.be"))
			{
				return uri.AbsolutePath.Trim('/');
			}
			else if (uri.Host.Contains("youtube.com"))
			{
				var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
				return query["v"];
			}
			return null;
		}

	}
}

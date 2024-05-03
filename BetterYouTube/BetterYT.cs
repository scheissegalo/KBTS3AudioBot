using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TS3AudioBot;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
//using TSLib.Full.Book;
//using TSLib;
//using TSLib.Commands;
//using TSLib.Full;
//using TSLib.Messages;
using System.Text.RegularExpressions;
using System.IO;

namespace BetterYouTube
{
	public class BetterYouTube : IBotPlugin
	{
		//private TsFullClient tsFullClient;
		private PlayManager playManager;
		//private Ts3Client ts3Client;
		//private Connection serverView;

		//Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, 
		public BetterYouTube(PlayManager playManager)
		{
			//this.ts3Client = ts3Client;
			//this.tsFullClient = tsFull;
			//this.serverView = serverView;
			this.playManager = playManager;
		}

		//Default Youtube Link play Command
		[Command("byt")]
		public static async Task<string> CommandBYT(PlayManager playManager, InvokerData invoker, string ytlink)
		{
			string cleanYTLink = LinkCleaner.CleanLink(ytlink);
			YTDlpHandler hdl = new YTDlpHandler();
			string filename = hdl.DownloadAudio(cleanYTLink);
			//return filename;
			if (!string.IsNullOrEmpty(filename))
			{
				// Assuming you have a method in PlayManager to play a file
				await playManager.Play(invoker, filename);
				//playManager.Play(filename); // Play the audio file
				return $"Successfully downloaded and started playing: [b][color=Green]{filename}[/color][/b]";
			}
			else
			{
				return "[b][color=Red]Failed to download the audio[/color][/b]";
			}
		}

		//Search on youtube and play
		[Command("byts")]
		public static async Task<string> CommandBYTS(PlayManager playManager, InvokerData invoker, string searchQuery)
		{
			//DownloadAndPlayFirstSearchResult
			YTDlpHandler hdl = new YTDlpHandler();
			string filename = hdl.DownloadAndPlayFirstSearchResult(searchQuery);
			//return filename;
			if (!string.IsNullOrEmpty(filename))
			{
				// Assuming you have a method in PlayManager to play a file
				await playManager.Play(invoker, filename);
				//playManager.Play(filename); // Play the audio file
				return $"Successfully downloaded and started playing: [b][color=Green]{filename}[/color][/b]";
			}
			else
			{
				return "[b][color=Red]Failed to download the audio[/color][/b]";
			}
		}

		public void Initialize()
		{

		}

		public void Dispose()
		{

		}
	}


	public class YTDlpHandler
	{
		public string DownloadAndPlayFirstSearchResult(string searchQuery)
		{
			string searchFormatted = searchQuery.Replace(" ", "+"); // Format the search query for URL
			string ytSearchCommand = $"ytsearch1:\"{searchQuery}\""; // yt-dlp command to search and take the first result

			//return ytSearchCommand;
			return DownloadAudio(ytSearchCommand); // Use your existing DownloadAudio method
		}


		public string DownloadAudio(string videoUrl)
		{
			string filename = null;
			const int timeout = 30000; // Timeout in milliseconds (10 seconds)
			string downloadDirectory = "mp3"; // Directory where files will be saved

			try
			{
				// Ensure the download directory exists
				Directory.CreateDirectory(downloadDirectory);

				// First process to get the output filename
				ProcessStartInfo startInfo = new ProcessStartInfo()
				{
					FileName = "yt-dlp.exe",
					Arguments = $"--get-filename -o \"{downloadDirectory}\\%(title)s.%(ext)s\" {videoUrl}", // Get filename with directory
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using (Process process = new Process() { StartInfo = startInfo })
				{
					process.Start();
					if (!process.WaitForExit(timeout)) // Wait for 10 seconds
					{
						process.Kill(); // Terminate process if it runs longer than the timeout
						throw new TimeoutException("The process exceeded the time limit for getting the filename.");
					}
					filename = process.StandardOutput.ReadLine().Trim(); // Read the filename from output
				}

				// Modify the filename to reflect the desired output format
				if (!string.IsNullOrEmpty(filename))
				{
					filename = Regex.Replace(filename, @"\.\w+$", ".mp3"); // Replace existing extension with .mp3

					startInfo.Arguments = $"-x --audio-format mp3 -o \"{filename}\" {videoUrl}"; // Download audio with full path
					using (Process process = new Process() { StartInfo = startInfo })
					{
						process.Start();
						if (!process.WaitForExit(timeout)) // Wait for 10 seconds
						{
							process.Kill(); // Terminate process if it runs longer than the timeout
							throw new TimeoutException("The process exceeded the time limit for downloading.");
						}
					}
				}

				// Extract only the filename from the path
				string onlyFileName = Path.GetFileName(filename);

				Console.WriteLine("Downloaded file: " + onlyFileName);
				return onlyFileName; // Return only the filename without the directory
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("An error occurred: " + ex.Message);
				return null;
			}
		}
	}



	public class LinkCleaner
	{
		public static string CleanLink(string bbCodeLink)
		{
			// Regular expression to find URL in BBCode
			string pattern = @"\[URL\](.*?)\[/URL\]";
			Match match = Regex.Match(bbCodeLink, pattern, RegexOptions.IgnoreCase);

			// Check if the match was successful
			if (match.Success)
			{
				// Return the first captured group, which is the URL
				return match.Groups[1].Value;
			}
			else
			{
				// Return original string if no URL is found
				return bbCodeLink;
			}
		}
	}
}

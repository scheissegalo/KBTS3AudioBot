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
using System.Runtime.InteropServices;

namespace BetterYouTube
{
	public class BetterYouTube : IBotPlugin
	{
		//private TsFullClient tsFullClient;
		private PlayManager playManager;
		//private Ts3Client ts3Client;
		//private Connection serverView;
		static string myOS;

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
			//return "my OS: " + myos;
			string cleanYTLink = LinkCleaner.CleanLink(ytlink);
			YTDlpHandler hdl = new YTDlpHandler(myOS);
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
			YTDlpHandler hdl = new YTDlpHandler(myOS);
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
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				//Console.WriteLine("Running on Windows");
				myOS = "WINDOWS";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				//Console.WriteLine("Running on Linux");
				myOS = "LINUX";
			}
			else
			{
				//Console.WriteLine("Running on an unsupported platform");
				myOS = "NA";
			}
		}

		public void Dispose()
		{

		}
	}


	public class YTDlpHandler
	{
		string myOS;
		public YTDlpHandler(string myos)
		{
			myOS = myos;
			//Console.WriteLine("Initializes with OS: " + myOS);
		}

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
			const int timeout = 90000; // 1 min 30 sec
			string downloadDirectory = "mp3"; // Directory where files will be saved

			try
			{
				// Ensure the download directory exists
				Directory.CreateDirectory(downloadDirectory);


				if (myOS == "WINDOWS")
				{
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
						if (!process.WaitForExit(timeout))
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
				}
				else if (myOS == "LINUX")
				{
					// Define the output path using the title of the video
					string outputPath = $"{downloadDirectory}/%(title)s.%(ext)s";

					ProcessStartInfo startInfo = new ProcessStartInfo()
					{
						FileName = "/home/berni/.local/bin/yt-dlp",
						Arguments = $"--get-filename -o \"{outputPath}\" {videoUrl}", // Adjusted for Linux
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};

					using (Process process = new Process() { StartInfo = startInfo })
					{
						process.Start();
						if (!process.WaitForExit(timeout))
						{
							process.Kill(); // Terminate process if it runs longer than the timeout
							throw new TimeoutException("The process exceeded the time limit for getting the filename.");
						}
						filename = process.StandardOutput.ReadLine().Trim(); // Read the filename from output
																			 // Replace the incorrect directory separator if any residue
						filename = filename.Replace('\\', '/');
					}

					// Modify the filename to reflect the desired output format
					if (!string.IsNullOrEmpty(filename))
					{
						filename = Regex.Replace(filename, @"\.\w+$", ".mp3"); // Replace existing extension with .mp3

						startInfo.Arguments = $"-x --audio-format mp3 -o \"{filename}\" {videoUrl}"; // Download audio with full path
						using (Process process = new Process() { StartInfo = startInfo })
						{
							process.Start();
							if (!process.WaitForExit(timeout))
							{
								process.Kill(); // Terminate process if it runs longer than the timeout
								throw new TimeoutException("The process exceeded the time limit for downloading.");
							}
						}
					}
				}
				else
				{
					throw new Exception("OS not Supported");
				}

				// Extract only the filename from the path
				string onlyFileName = Path.GetFileName(filename);
				//string onlyFileName = Path.GetFileName(filename);

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

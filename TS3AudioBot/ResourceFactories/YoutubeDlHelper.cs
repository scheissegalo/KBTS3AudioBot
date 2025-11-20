// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS3AudioBot.Config;
using TS3AudioBot.Helper;
using TS3AudioBot.Localization;

namespace TS3AudioBot.ResourceFactories
{
	public static class YoutubeDlHelper
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public static ConfPath? DataObj { private get; set; }
		private static string? YoutubeDlPath => DataObj?.Path.Value;

		// New configuration properties for cookies and extractor-args
		public static ConfigValue<string>? CookieFile { private get; set; }
		public static ConfigValue<string>? ExtractorArgs { private get; set; }

		private const string ParamGetSingleVideo = " --no-warnings --dump-json --id --";
		private const string ParamGetPlaylist = "--no-warnings --yes-playlist --flat-playlist --dump-single-json --id --";
		private const string ParamGetSearch = "--no-warnings --flat-playlist --dump-single-json -- ytsearch10:";

		/// <summary>
		/// Builds yt-dlp command arguments with optional cookie file and extractor-args support.
		/// </summary>
		/// <param name="baseParams">Base parameters for the yt-dlp command</param>
		/// <param name="target">Target URL or video ID</param>
		/// <returns>Complete argument string for yt-dlp execution</returns>
		private static string BuildArguments(string baseParams, string target)
		{
			var sb = new StringBuilder();
			
			// We need to insert cookies and extractor-args BEFORE any "--" separator
			// because "--" tells yt-dlp that everything after it is a URL, not an option
			
			// Find the position of " --" (the separator)
			int separatorIndex = baseParams.IndexOf(" --");
			
			if (separatorIndex >= 0)
			{
				// Insert our options before the "--" separator
				sb.Append(baseParams.Substring(0, separatorIndex));
				
				// Add cookies if configured
				var cookieFilePath = CookieFile?.Value;
				if (!string.IsNullOrEmpty(cookieFilePath))
				{
					if (File.Exists(cookieFilePath))
					{
						sb.Append($" --cookies \"{cookieFilePath}\"");
					}
					else
					{
						Log.Warn("Cookie file not found: {0}", cookieFilePath);
					}
				}

				// Add extractor args if configured
				var extractorArgs = ExtractorArgs?.Value;
				if (!string.IsNullOrEmpty(extractorArgs))
				{
					sb.Append($" --extractor-args \"{extractorArgs}\"");
				}
				
				// Add the rest of baseParams (including the "--" separator)
				sb.Append(baseParams.Substring(separatorIndex));
			}
			else
			{
				// No separator found, just append everything
				sb.Append(baseParams);
				
				// Add cookies if configured
				var cookieFilePath = CookieFile?.Value;
				if (!string.IsNullOrEmpty(cookieFilePath))
				{
					if (File.Exists(cookieFilePath))
					{
						sb.Append($" --cookies \"{cookieFilePath}\"");
					}
					else
					{
						Log.Warn("Cookie file not found: {0}", cookieFilePath);
					}
				}

				// Add extractor args if configured
				var extractorArgs = ExtractorArgs?.Value;
				if (!string.IsNullOrEmpty(extractorArgs))
				{
					sb.Append($" --extractor-args \"{extractorArgs}\"");
				}
			}

			sb.Append($" {target}");
			return sb.ToString();
		}

		public static async Task<JsonYtdlDump> GetSingleVideo(string id)
		{
			var ytdlPath = FindYoutubeDl();
			if (ytdlPath is null)
				throw Error.LocalStr(strings.error_ytdl_not_found);

			var baseArgs = $"{ytdlPath.Value.param}{ParamGetSingleVideo}";
			var args = BuildArguments(baseArgs, id);
			return await RunYoutubeDl<JsonYtdlDump>(ytdlPath.Value.ytdlpath, args);
		}

		public static async Task<JsonYtdlPlaylistDump> GetPlaylistAsync(string url)
		{
			var ytdlPath = FindYoutubeDl();
			if (ytdlPath is null)
				throw Error.LocalStr(strings.error_ytdl_not_found);

			var baseArgs = $"{ytdlPath.Value.param}{ParamGetPlaylist}";
			var args = BuildArguments(baseArgs, url);
			return await RunYoutubeDl<JsonYtdlPlaylistDump>(ytdlPath.Value.ytdlpath, args);
		}

		public static async Task<JsonYtdlPlaylistDump> GetSearchAsync(string text)
		{
			var ytdlPath = FindYoutubeDl();
			if (ytdlPath is null)
				throw Error.LocalStr(strings.error_ytdl_not_found);

			var baseArgs = $"{ytdlPath.Value.param}{ParamGetSearch}";
			var args = BuildArguments(baseArgs, $"\"{text}\"");
			return await RunYoutubeDl<JsonYtdlPlaylistDump>(ytdlPath.Value.ytdlpath, args);
		}

		/// <summary>
		/// Downloads a YouTube video to a temporary file for direct playback.
		/// </summary>
		/// <param name="id">The video ID to download</param>
		/// <param name="tempDir">The temporary directory to store the downloaded file</param>
		/// <returns>Success with file path, or error message on failure</returns>
		public static async Task<R<string, string>> DownloadVideo(string id, string tempDir)
		{
			var ytdlPath = FindYoutubeDl();
			if (ytdlPath is null)
			{
				Log.Error("yt-dlp not found for download operation");
				return R<string, string>.Err("yt-dlp not found");
			}

			try
			{
				// Create temp directory if it doesn't exist
				if (!Directory.Exists(tempDir))
				{
					Log.Info("Creating temp download directory: {0}", tempDir);
					Directory.CreateDirectory(tempDir);
				}

				// Generate temp file path with unique GUID to prevent collisions
				var tempFile = Path.Combine(tempDir, $"ytdl_{id}_{Guid.NewGuid()}.m4a");
				Log.Info("Downloading video {0} to {1}", id, tempFile);

				// Build download arguments
				var args = new StringBuilder();
				args.Append(ytdlPath.Value.param);
				args.Append(" --no-warnings");

				// Add cookies if configured
				var cookieFilePath = CookieFile?.Value;
				if (!string.IsNullOrEmpty(cookieFilePath))
				{
					if (File.Exists(cookieFilePath))
					{
						args.Append($" --cookies \"{cookieFilePath}\"");
						Log.Debug("Using cookie file: {0}", cookieFilePath);
					}
					else
					{
						Log.Warn("Cookie file not found: {0}", cookieFilePath);
					}
				}

				// Add extractor args if configured
				var extractorArgs = ExtractorArgs?.Value;
				if (!string.IsNullOrEmpty(extractorArgs))
				{
					args.Append($" --extractor-args \"{extractorArgs}\"");
					Log.Debug("Using extractor args: {0}", extractorArgs);
				}

				// Download best audio format
				args.Append(" -f bestaudio");
				args.Append($" -o \"{tempFile}\"");
				args.Append($" -- {id}");

				var fullArgs = args.ToString();
				Log.Debug("Executing yt-dlp download: {0} {1}", ytdlPath.Value.ytdlpath, fullArgs);

				// Execute yt-dlp download
				using var process = new Process();
				process.StartInfo.FileName = ytdlPath.Value.ytdlpath;
				process.StartInfo.Arguments = fullArgs;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;

				var stdOut = new StringBuilder();
				var stdErr = new StringBuilder();
				process.OutputDataReceived += (s, e) =>
				{
					if (e.Data != null)
					{
						stdOut.AppendLine(e.Data);
					}
				};
				process.ErrorDataReceived += (s, e) =>
				{
					if (e.Data != null)
					{
						stdErr.AppendLine(e.Data);
					}
				};

				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				// Wait for download with 5 minute timeout
				await process.WaitForExitAsync(TimeSpan.FromMinutes(5));

				if (!process.HasExitedSafe())
				{
					Log.Warn("Download timed out after 5 minutes for video {0}", id);
					try { process.Kill(); }
					catch (Exception ex) { Log.Debug(ex, "Failed to kill download process"); }
					return R<string, string>.Err("Download timed out after 5 minutes");
				}

				if (process.ExitCode != 0)
				{
					var error = stdErr.ToString();
					Log.Warn("yt-dlp download failed for video {0}. Exit code: {1}. Error: {2}", 
						id, process.ExitCode, error);
					return R<string, string>.Err("Download failed: " + error);
				}

				// Verify downloaded file exists
				if (!File.Exists(tempFile))
				{
					Log.Error("Downloaded file not found at expected location: {0}", tempFile);
					return R<string, string>.Err("Downloaded file not found");
				}

				var fileInfo = new FileInfo(tempFile);
				Log.Info("Successfully downloaded video {0} to {1} ({2} bytes)", 
					id, tempFile, fileInfo.Length);

				return R<string, string>.OkR(tempFile);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to download video {0}", id);
				return R<string, string>.Err("Download exception: " + ex.Message);
			}
		}

		public static (string ytdlpath, string param)? FindYoutubeDl()
		{
			var youtubeDlPath = YoutubeDlPath;
			if (string.IsNullOrEmpty(youtubeDlPath))
			{
				// Default path youtube-dl is suggesting to install
				const string defaultYtDlPath = "/usr/local/bin/youtube-dl";
				if (File.Exists(defaultYtDlPath))
					return (defaultYtDlPath, "");

				// Default path most package managers install to
				const string defaultPkgManPath = "/usr/bin/youtube-dl";
				if (File.Exists(defaultPkgManPath))
					return (defaultPkgManPath, "");

				youtubeDlPath = Directory.GetCurrentDirectory();
			}

			string fullCustomPath;
			try { fullCustomPath = Path.GetFullPath(youtubeDlPath); }
			catch (ArgumentException ex)
			{
				Log.Warn(ex, "Your youtube-dl path may contain invalid characters");
				return null;
			}

			// Example: /home/teamspeak/youtube-dl where 'youtube-dl' is the binary
			if (File.Exists(fullCustomPath) || File.Exists(fullCustomPath + ".exe"))
				return (fullCustomPath, "");

			// Example: /home/teamspeak where the binary 'youtube-dl' lies in ./teamspeak/
			string fullCustomPathWithoutFile = Path.Combine(fullCustomPath, "youtube-dl");
			if (File.Exists(fullCustomPathWithoutFile) || File.Exists(fullCustomPathWithoutFile + ".exe"))
				return (fullCustomPathWithoutFile, "");

			// Example: /home/teamspeak/youtube-dl where 'youtube-dl' is the github project folder
			string fullCustomPathGhProject = Path.Combine(fullCustomPath, "youtube_dl", "__main__.py");
			if (File.Exists(fullCustomPathGhProject))
				return ("python", $"\"{fullCustomPathGhProject}\"");

			return null;
		}

		public static async Task<T> RunYoutubeDl<T>(string path, string args) where T : notnull
		{
			try
			{
				// Log the command being executed for debugging
				Log.Debug("Executing yt-dlp: {0} {1}", path, args);
				
				bool stdOutDone = false;
				var stdOut = new StringBuilder();
				var stdErr = new StringBuilder();

				using var tmproc = new Process();
				tmproc.StartInfo.FileName = path;
				tmproc.StartInfo.Arguments = args;
				tmproc.StartInfo.UseShellExecute = false;
				tmproc.StartInfo.CreateNoWindow = true;
				tmproc.StartInfo.RedirectStandardOutput = true;
				tmproc.StartInfo.RedirectStandardError = true;
				tmproc.EnableRaisingEvents = true;
				tmproc.Start();
				tmproc.OutputDataReceived += (s, e) =>
				{
					if (e.Data is null)
						stdOutDone = true;
					else
						stdOut.Append(e.Data);
				};
				tmproc.ErrorDataReceived += (s, e) => stdErr.Append(e.Data);
				tmproc.BeginOutputReadLine();
				tmproc.BeginErrorReadLine();
				await tmproc.WaitForExitAsync(TimeSpan.FromSeconds(20));

				if (!tmproc.HasExitedSafe())
				{
					try { tmproc.Kill(); }
					catch (Exception ex) { Log.Debug(ex, "Failed to kill"); }
				}

				var timeout = Stopwatch.StartNew();
				while (!stdOutDone)
				{
					if (timeout.Elapsed >= TimeSpan.FromSeconds(5))
					{
						stdErr.Append(strings.error_ytdl_empty_response).Append(" (timeout)");
						break;
					}
					await Task.Delay(50);
				}

				if (stdErr.Length > 0)
				{
					var errorOutput = stdErr.ToString();
					Log.Debug("youtube-dl failed to load the resource:\n{0}", errorOutput);
					
					// Transform the error into a user-friendly message
					var friendlyError = TransformYtdlError(errorOutput);
					throw Error.LocalStr(friendlyError);
				}

				return ParseResponse<T>(stdOut.ToString());
			}
			catch (Win32Exception ex)
			{
				Log.Error(ex, "Failed to run youtube-dl: {0}", ex.Message);
				throw Error.Exception(ex).LocalStr(strings.error_ytdl_failed_to_run);
			}
		}

		public static T ParseResponse<T>(string? json) where T : notnull
		{
			if (string.IsNullOrEmpty(json))
				throw Error.LocalStr(strings.error_ytdl_empty_response);

			try
			{

				return JsonConvert.DeserializeObject<T>(json);
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Failed to read youtube-dl json data");
				throw Error.Exception(ex).LocalStr(strings.error_media_internal_invalid);
			}
		}

		public static JsonYtdlFormat? FilterBest(IEnumerable<JsonYtdlFormat>? formats)
		{
			Log.Debug("Picking from options: {@formats}", formats);

			if (formats is null)
				return null;

			JsonYtdlFormat? best = null;
			foreach (var format in formats)
			{
				if (format.acodec == "none")
					continue;
				if (best == null
					|| format.abr > best.abr
					|| (format.vcodec == "none" && format.abr >= best.abr))
				{
					best = format;
				}
			}

			Log.Debug("Picked: {@format}", best);
			return best;
		}

		/// <summary>
		/// Enhanced format selection that handles modern YouTube formats with null bitrates.
		/// Prioritizes direct URLs over HLS manifests for reliability, then selects the best 
		/// audio format based on codec quality, stream type, and resolution.
		/// </summary>
		/// <param name="formats">Available formats from yt-dlp</param>
		/// <returns>The best audio format, or null if no suitable format found</returns>
		public static JsonYtdlFormat? FilterBestEnhanced(IEnumerable<JsonYtdlFormat>? formats)
		{
			Log.Debug("FilterBestEnhanced: Picking from available formats: {@formats}", formats);

			if (formats is null)
			{
				Log.Warn("FilterBestEnhanced: No formats provided (null)");
				return null;
			}

			var formatList = formats.ToList();
			if (!formatList.Any())
			{
				Log.Warn("FilterBestEnhanced: No formats provided (empty list)");
				return null;
			}

			// Filter to only formats with audio (acodec != "none")
			var audioFormats = formatList.Where(f => f.acodec != "none").ToList();
			
			if (!audioFormats.Any())
			{
				Log.Error("FilterBestEnhanced: No audio formats available. All formats: {@formats}", formatList);
				return null;
			}

			Log.Debug("FilterBestEnhanced: Found {0} formats with audio out of {1} total formats", 
				audioFormats.Count, formatList.Count);

			// Categorize formats into direct URLs and HLS manifests
			var directFormats = audioFormats.Where(f => !IsHlsManifest(f.url)).ToList();
			var hlsFormats = audioFormats.Where(f => IsHlsManifest(f.url)).ToList();

			Log.Debug("FilterBestEnhanced: Categorized formats - {0} direct URLs, {1} HLS manifests",
				directFormats.Count, hlsFormats.Count);

			// Prioritize direct URLs over HLS manifests for reliability
			var preferredFormats = directFormats.Any() ? directFormats : hlsFormats;
			var formatType = directFormats.Any() ? "direct URL" : "HLS manifest";

			Log.Info("FilterBestEnhanced: Prioritizing {0} formats for selection", formatType);

			// Sort by quality criteria within the preferred category:
			// 1. Codec quality (descending) - prefer AAC-LC and Opus
			// 2. Audio-only preference (descending) - prefer audio-only over combined
			// 3. Video resolution (ascending) - prefer lower resolution for combined streams
			// 4. Bitrate if available (descending) - prefer higher bitrate
			var sorted = preferredFormats
				.OrderByDescending(f => GetCodecQuality(f.acodec))
				.ThenByDescending(f => IsAudioOnly(f))
				.ThenBy(f => GetVideoResolution(f))
				.ThenByDescending(f => f.abr ?? 0)
				.ToList();

			var best = sorted.FirstOrDefault();

			if (best != null)
			{
				var selectedFormatType = IsHlsManifest(best.url) ? "HLS" : "Direct";
				Log.Info("FilterBestEnhanced: Selected {0} format {1} - codec: {2}, audio-only: {3}, resolution: {4}x{5}, bitrate: {6}",
					selectedFormatType,
					best.format_id,
					best.acodec,
					IsAudioOnly(best),
					best.width ?? 0,
					best.height ?? 0,
					best.abr?.ToString() ?? "null");
			}
			else
			{
				Log.Error("FilterBestEnhanced: Failed to select a format from {0} audio formats", audioFormats.Count);
			}

			return best;
		}

		/// <summary>
		/// Returns a quality ranking for audio codecs.
		/// Higher values indicate better quality.
		/// </summary>
		/// <param name="codec">The audio codec string (e.g., "mp4a.40.2", "opus")</param>
		/// <returns>Quality ranking: 3 for AAC-LC/Opus, 2 for HE-AAC, 1 for unknown, 0 for null</returns>
		private static int GetCodecQuality(string? codec)
		{
			if (codec == null)
				return 0;

			// AAC-LC (Advanced Audio Coding - Low Complexity) - highest quality AAC profile
			if (codec.Contains("mp4a.40.2"))
				return 3;

			// Opus - high quality, efficient codec
			if (codec.Contains("opus"))
				return 3;

			// HE-AAC (High-Efficiency AAC) - medium quality, optimized for low bitrates
			if (codec.Contains("mp4a.40.5"))
				return 2;

			// Unknown codec - lowest priority but still valid
			return 1;
		}

		/// <summary>
		/// Determines if a format is audio-only (no video stream).
		/// </summary>
		/// <param name="format">The format to check</param>
		/// <returns>True if the format is audio-only, false otherwise</returns>
		private static bool IsAudioOnly(JsonYtdlFormat format)
		{
			return format.vcodec == "none";
		}

		/// <summary>
		/// Calculates the video resolution as width * height.
		/// Returns 0 for audio-only formats.
		/// </summary>
		/// <param name="format">The format to calculate resolution for</param>
		/// <returns>Resolution in pixels (width * height), or 0 for audio-only</returns>
		private static int GetVideoResolution(JsonYtdlFormat format)
		{
			if (format.vcodec == "none")
				return 0;

			return (format.width ?? 0) * (format.height ?? 0);
		}

		/// <summary>
		/// Determines if a URL is an HLS (HTTP Live Streaming) manifest.
		/// HLS manifests use .m3u8 playlists and are commonly served from manifest.googlevideo.com.
		/// </summary>
		/// <param name="url">The URL to check</param>
		/// <returns>True if the URL is an HLS manifest, false otherwise</returns>
		public static bool IsHlsManifest(string? url)
		{
			if (string.IsNullOrEmpty(url))
				return false;

			// Check for Google Video manifest domain
			if (url.Contains("manifest.googlevideo.com"))
				return true;

			// Check for .m3u8 extension (HLS playlist format)
			if (url.Contains(".m3u8"))
				return true;

			// Check for hls_playlist in URL
			if (url.Contains("hls_playlist"))
				return true;

			return false;
		}

		/// <summary>
		/// Transforms yt-dlp error output into user-friendly error messages.
		/// Detects common error patterns and provides helpful guidance.
		/// </summary>
		/// <param name="errorOutput">The raw error output from yt-dlp</param>
		/// <returns>A user-friendly error message</returns>
		public static string TransformYtdlError(string errorOutput)
		{
			if (string.IsNullOrEmpty(errorOutput))
			{
				Log.Warn("TransformYtdlError: Empty error output provided");
				return strings.error_ytdl_song_failed_to_load;
			}

			var lowerError = errorOutput.ToLowerInvariant();

			// Detect timeout errors
			if (lowerError.Contains("timeout") || lowerError.Contains("timed out"))
			{
				Log.Info("Detected timeout error in yt-dlp output");
				return "Request timed out while fetching video information. Please try again later.";
			}

			// Detect video not found errors
			if (lowerError.Contains("video unavailable") || 
			    lowerError.Contains("video not available") ||
			    lowerError.Contains("this video is unavailable") ||
			    lowerError.Contains("video has been removed") ||
			    lowerError.Contains("video is not available") ||
			    lowerError.Contains("private video") ||
			    lowerError.Contains("video is private"))
			{
				Log.Info("Detected video not found/unavailable error in yt-dlp output");
				return "Video not found or unavailable. It may be private, removed, or region-locked.";
			}

			// Detect authentication/sign-in errors
			if (lowerError.Contains("sign in") || 
			    lowerError.Contains("login required") ||
			    lowerError.Contains("members-only") ||
			    lowerError.Contains("this video requires payment") ||
			    lowerError.Contains("confirm your age") ||
			    lowerError.Contains("age-restricted"))
			{
				Log.Info("Detected authentication/age-restriction error in yt-dlp output");
				return "Video requires authentication or age verification. Please configure a cookie file in bot settings to access this content.";
			}

			// Detect empty format list
			if (lowerError.Contains("no formats found") || 
			    lowerError.Contains("no video formats") ||
			    lowerError.Contains("requested format not available"))
			{
				Log.Info("Detected empty format list error in yt-dlp output");
				return "No playable formats found for this video. The video may use an unsupported format or be unavailable in your region.";
			}

			// Detect network errors
			if (lowerError.Contains("unable to download") ||
			    lowerError.Contains("http error") ||
			    lowerError.Contains("connection") ||
			    lowerError.Contains("network"))
			{
				Log.Info("Detected network error in yt-dlp output");
				return $"Network error while fetching video: {errorOutput.Substring(0, Math.Min(200, errorOutput.Length))}";
			}

			// Log the full error for debugging and return generic message
			Log.Warn("Unknown yt-dlp error pattern. Full error output: {0}", errorOutput);
			return $"youtube-dl failed to load the resource. Error: {errorOutput.Substring(0, Math.Min(200, errorOutput.Length))}";
		}

		public static SongInfo MapToSongInfo(JsonYtdlDump dump)
		{
			return new SongInfo
			{
				Title = dump.title,
				Track = dump.track,
				Artist = dump.artist,
				Length = TimeSpan.FromSeconds(dump.duration)
			};
		}

		// https://stackoverflow.com/a/50461641/2444047
		/// <summary>
		/// Waits asynchronously for the process to exit.
		/// </summary>
		/// <param name="process">The process to wait for cancellation.</param>
		/// <param name="timeout">The maximum time to wait for exit before returning anyway.</param>
		/// <param name="cancellationToken">A cancellation token. If invoked, the task will return
		/// immediately as canceled.</param>
		/// <returns>A Task representing waiting for the process to end.</returns>
		public static async Task WaitForExitAsync(this Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			void Process_Exited(object? sender, EventArgs e)
			{
				tcs.TrySetResult(true);
			}

			process.EnableRaisingEvents = true;
			process.Exited += Process_Exited;

			try
			{
				if (process.HasExited)
				{
					return;
				}

				var timoutTask = Task.Delay(timeout, cancellationToken);

				using (cancellationToken.Register(() => tcs.TrySetCanceled()))
				{
					await Task.WhenAny(tcs.Task, timoutTask);
				}
			}
			finally
			{
				process.Exited -= Process_Exited;
			}
		}
	}

#pragma warning disable CS0649, CS0169, IDE1006
	public abstract class JsonYtdlBase
	{
		public string? extractor { get; set; }
		public string? extractor_key { get; set; }
	}

	public class JsonYtdlDump : JsonYtdlBase
	{
		public string? title { get; set; }
		public string? track { get; set; }
		public string? artist { get; set; }
		// TODO int -> timespan converter
		public float duration { get; set; }
		public string? id { get; set; }
		public JsonYtdlFormat[]? formats { get; set; }
		public JsonYtdlFormat[]? requested_formats { get; set; }

		public string? AutoTitle => track ?? title;
	}

	public class JsonYtdlFormat
	{
		public string? vcodec { get; set; }
		public string? acodec { get; set; }
		/// <summary>audioBitRate</summary>
		public float? abr { get; set; }
		/// <summary>audioSampleRate</summary>
		public float? asr { get; set; }
		/// <summary>totalBitRate</summary>
		public float? tbr { get; set; }
		//public object http_headers { get; set; }
		public string? format { get; set; }
		public string? format_id { get; set; }
		public string? url { get; set; }
		public string? ext { get; set; }
		public int? width { get; set; }
		public int? height { get; set; }
	}

	public class JsonYtdlPlaylistDump : JsonYtdlBase
	{
		public string? id { get; set; }
		public string? title { get; set; }
		public JsonYtdlPlaylistEntry[]? entries { get; set; }
	}

	public class JsonYtdlPlaylistEntry
	{
		public string? title { get; set; }
		public string? id { get; set; }
	}
#pragma warning restore CS0649, CS0169, IDE1006
}

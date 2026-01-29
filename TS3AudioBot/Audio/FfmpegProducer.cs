// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS3AudioBot.Config;
using TS3AudioBot.Helper;
using TSLib.Audio;
using TSLib.Helper;
using TSLib.Scheduler;

namespace TS3AudioBot.Audio
{
	public class FfmpegProducer : IPlayerSource, ISampleInfo, IDisposable
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private readonly Id id;
		private static readonly Regex FindDurationMatch = new Regex(@"^\s*Duration: (\d+):(\d\d):(\d\d).(\d\d)", Util.DefaultRegexConfig);
		private static readonly Regex IcyMetadataMacher = new Regex("((\\w+)='(.*?)';\\s*)+", Util.DefaultRegexConfig);
		private const string PreLinkConf = "-hide_banner -nostats -threads 1 -i \"";
		private const string PostLinkConf = "\" -ac 2 -ar 48000 -f s16le -acodec pcm_s16le pipe:1";
		private const string LinkConfIcy = "-hide_banner -nostats -threads 1 -i pipe:0 -ac 2 -ar 48000 -f s16le -acodec pcm_s16le pipe:1";
		private static readonly TimeSpan retryOnDropBeforeEnd = TimeSpan.FromSeconds(10);

		private readonly ConfToolsFfmpeg config;
		
		// Static property to hold HLS options from configuration
		public static Config.ConfigValue<string>? HlsOptions { private get; set; }
		
		// Static property to hold cookie file path for YouTube HLS access
		public static Config.ConfigValue<string>? CookieFile { private get; set; }

		public event EventHandler? OnSongEnd;
		public event EventHandler<SongInfoChanged>? OnSongUpdated;

		private readonly DedicatedTaskScheduler scheduler;
		private FfmpegInstance? ffmpegInstance;

		public int SampleRate { get; } = 48000;
		public int Channels { get; } = 2;
		public int BitsPerSample { get; } = 16;

		public FfmpegProducer(ConfToolsFfmpeg config, DedicatedTaskScheduler scheduler, Id id)
		{
			this.config = config;
			this.scheduler = scheduler;
			this.id = id;
		}

		public Task AudioStart(string url, TimeSpan? startOff = null)
		{
			StartFfmpegProcess(url, startOff ?? TimeSpan.Zero);
			return Task.CompletedTask;
		}

		public async Task AudioStartIcy(string url) => await StartFfmpegProcessIcy(url);

		public void AudioStop()
		{
			StopFfmpegProcess();
		}

		public TimeSpan? Length => GetCurrentSongLength();

		public TimeSpan? Position => ffmpegInstance?.AudioTimer.SongPosition;

		public Task Seek(TimeSpan position) { SetPosition(position); return Task.CompletedTask; }

		public int Read(byte[] buffer, int offset, int length, out Meta? meta)
		{
			meta = default;
			int read;

			var instance = ffmpegInstance;

			if (instance is null)
				return 0;

			try
			{
				read = instance.FfmpegProcess.StandardOutput.BaseStream.Read(buffer, 0, length);
			}
			catch (Exception ex)
			{
				read = 0;
				Log.Debug(ex, "Can't read ffmpeg");
			}

			if (read == 0)
			{
				AssertNotMainScheduler();

				var (ret, triggerEndSafe) = instance.IsIcyStream
					? OnReadEmptyIcy(instance)
					: OnReadEmpty(instance);
				if (ret)
					return 0;

				if (instance.FfmpegProcess.HasExitedSafe())
				{
					Log.Trace("Ffmpeg has exited");
					
					// Log playback completion status for HLS streams
					if (instance.IsHlsPlayback)
					{
						if (instance.HasDetectedPositionJump)
						{
							Log.Warn("HLS playback ended with detected position jumps. Chunks may have played out of order.");
						}
						else
						{
							Log.Info("HLS playback ended successfully with no detected position jumps.");
						}
					}
					
					AudioStop();
					triggerEndSafe = true;
				}

				if (triggerEndSafe)
				{
					OnSongEnd?.Invoke(this, EventArgs.Empty);
					return 0;
				}
			}

			instance.HasTriedToReconnect = false;
			instance.AudioTimer.PushBytes(read);

			// Track position for HLS playback to detect unexpected jumps
			if (instance.IsHlsPlayback)
			{
				var currentPosition = instance.AudioTimer.SongPosition;
				
				// Check for backward position jumps (chunk ordering issues)
				// Allow small variations due to timing precision, but flag significant jumps
				if (currentPosition < instance.LastReportedPosition - TimeSpan.FromMilliseconds(500))
				{
					Log.Warn("HLS playback position jumped backward! Previous: {0}, Current: {1}, Jump: {2}ms",
						instance.LastReportedPosition,
						currentPosition,
						(instance.LastReportedPosition - currentPosition).TotalMilliseconds);
					instance.HasDetectedPositionJump = true;
				}
				
				// Update last reported position periodically (every ~1 second of playback)
				if (currentPosition - instance.LastReportedPosition > TimeSpan.FromSeconds(1))
				{
					instance.LastReportedPosition = currentPosition;
				}
			}

			return read;
		}

		private (bool ret, bool trigger) OnReadEmpty(FfmpegInstance instance)
		{
			if (instance.FfmpegProcess.HasExitedSafe() && !instance.HasTriedToReconnect)
			{
				var expectedStopLength = GetCurrentSongLength();
				Log.Trace("Expected song length {0}", expectedStopLength);
				if (expectedStopLength != TimeSpan.Zero)
				{
					var actualStopPosition = instance.AudioTimer.SongPosition;
					Log.Trace("Actual song position {0}", actualStopPosition);
					
					// For HLS streams with sequential processing flags, disable automatic reconnection
					// The sequential flags should ensure complete playback without drops
					// Reconnecting can cause the ending to play twice
					if (instance.IsHlsPlayback)
					{
						Log.Debug("HLS stream ended at position {0} (expected: {1}). Not retrying due to sequential processing.", 
							actualStopPosition, expectedStopLength);
					}
					else if (actualStopPosition + retryOnDropBeforeEnd < expectedStopLength)
					{
						Log.Debug("Connection to song lost, retrying at {0}", actualStopPosition);
						instance.HasTriedToReconnect = true;
						var newInstance = SetPosition(actualStopPosition);
						if (newInstance.Ok)
						{
							newInstance.Value.HasTriedToReconnect = true;
							return (true, false);
						}
						else
						{
							Log.Debug("Retry failed {0}", newInstance.Error);
							return (false, true);
						}
					}
				}

				// Log playback completion with ordering status for HLS streams
				if (instance.IsHlsPlayback)
				{
					if (instance.HasDetectedPositionJump)
					{
						Log.Warn("HLS playback completed with detected position jumps. Chunks may have played out of order.");
					}
					else
					{
						Log.Info("HLS playback completed successfully with no detected position jumps.");
					}
				}
			}
			return (false, false);
		}

		private (bool ret, bool trigger) OnReadEmptyIcy(FfmpegInstance instance)
		{
			AssertNotMainScheduler();

			if (instance.FfmpegProcess.HasExitedSafe() && !instance.HasTriedToReconnect)
			{
				Log.Debug("Connection to stream lost, retrying...");
				instance.HasTriedToReconnect = true;
				var newInstance = StartFfmpegProcessIcy(instance.ReconnectUrl).Result;
				if (newInstance.Ok)
				{
					newInstance.Value.HasTriedToReconnect = true;
					return (true, false);
				}
				else
				{
					Log.Debug("Retry failed {0}", newInstance.Error);
					return (false, true);
				}
			}
			return (false, false);
		}

		private R<FfmpegInstance, string> SetPosition(TimeSpan value)
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));
			var instance = ffmpegInstance;
			if (instance is null)
				return "No instance running";
			if (instance.IsIcyStream)
				return "Cannot seek icy stream";
			var lastLink = instance.ReconnectUrl;
			if (lastLink is null)
				return "No current url active";
			return StartFfmpegProcess(lastLink, value);
		}

		private R<FfmpegInstance, string> StartFfmpegProcess(string url, TimeSpan? offsetOpt)
		{
			StopFfmpegProcess();
			Log.Trace("Start request {0}", url);

			string arguments;
			var offset = offsetOpt ?? TimeSpan.Zero;
			
			// Check if this is an HLS URL and use appropriate argument building
			bool isHls = IsHlsUrl(url);
			
			if (isHls)
			{
				Log.Info("HLS URL detected, using sequential processing flags");
				arguments = BuildHlsArguments(url, offset);
			}
			else
			{
				Log.Debug("Non-HLS URL, using standard arguments");
				if (offset > TimeSpan.Zero)
				{
					var seek = string.Format(CultureInfo.InvariantCulture, @"-ss {0:hh\:mm\:ss\.fff}", offset);
					arguments = string.Concat(seek, " ", PreLinkConf, url, PostLinkConf, " ", seek);
				}
				else
				{
					arguments = string.Concat(PreLinkConf, url, PostLinkConf);
				}
			}

			var newInstance = new FfmpegInstance(
				url,
				new PreciseAudioTimer(this)
				{
					SongPositionOffset = offset,
				})
			{
				IsHlsPlayback = isHls
			};

			return StartFfmpegProcessInternal(newInstance, arguments);
		}

		private async Task<R<FfmpegInstance, string>> StartFfmpegProcessIcy(string url)
		{
			StopFfmpegProcess();
			Log.Trace("Start icy-stream request {0}", url);

			try
			{
				var response = await WebWrapper
					.Request(url)
					.WithHeader("Icy-MetaData", "1")
					.UnsafeResponse();

				if (!int.TryParse(response.Headers.GetSingle("icy-metaint"), out var metaint))
				{
					response.Dispose();
					return "Invalid icy stream tags";
				}

				var stream = await response.Content.ReadAsStreamAsync();
				var newInstance = new FfmpegInstance(
					url,
					new PreciseAudioTimer(this),
					stream,
					metaint)
				{
					OnMetaUpdated = e => OnSongUpdated?.Invoke(this, e)
				};

				new Thread(() => newInstance.ReadStreamLoop(id))
				{
					Name = $"IcyStreamReader[{id}]",
				}.Start();

				return StartFfmpegProcessInternal(newInstance, LinkConfIcy);
			}
			catch (Exception ex)
			{
				var error = $"Unable to create icy-stream ({ex.Message})";
				Log.Warn(ex, error);
				return error;
			}
		}

		private R<FfmpegInstance, string> StartFfmpegProcessInternal(FfmpegInstance instance, string arguments)
		{
			try
			{
				instance.FfmpegProcess.StartInfo = new ProcessStartInfo
				{
					FileName = config.Path.Value,
					Arguments = arguments,
					RedirectStandardOutput = true,
					RedirectStandardInput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				instance.FfmpegProcess.EnableRaisingEvents = true;

				Log.Debug("Starting ffmpeg with {0}", arguments);
				instance.FfmpegProcess.ErrorDataReceived += instance.FfmpegProcess_ErrorDataReceived;
				instance.FfmpegProcess.Start();
				instance.FfmpegProcess.BeginErrorReadLine();

				instance.AudioTimer.Start();

				var oldInstance = Interlocked.Exchange(ref ffmpegInstance, instance);
				oldInstance?.Close();

				return instance;
			}
			catch (Exception ex)
			{
				var error = ex is Win32Exception
					? $"Ffmpeg could not be found ({ex.Message})"
					: $"Unable to create stream ({ex.Message})";
				Log.Error(ex, error);
				instance.Close();
				StopFfmpegProcess();
				return error;
			}
		}

		private void StopFfmpegProcess()
		{
			var oldInstance = Interlocked.Exchange(ref ffmpegInstance, null);
			if (oldInstance != null)
			{
				oldInstance.OnMetaUpdated = null;
				oldInstance.Close();
			}
		}

		private TimeSpan? GetCurrentSongLength() => ffmpegInstance?.ParsedSongLength;

		private void AssertNotMainScheduler()
		{
			if (TaskScheduler.Current == scheduler)
				throw new Exception("Cannot read on own scheduler. Throwing to prevent deadlock");
		}

		private static bool IsHlsUrl(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				Log.Trace("IsHlsUrl: URL is null or empty");
				return false;
			}

			bool isHls = url.Contains("manifest.googlevideo.com")
				|| url.Contains(".m3u8")
				|| url.Contains("hls_playlist");

			Log.Debug("IsHlsUrl: URL '{0}' is {1}HLS", url, isHls ? "" : "not ");

			return isHls;
		}

		private static string BuildHlsArguments(string url, TimeSpan offset)
		{
			var sb = new System.Text.StringBuilder();

			Log.Debug("Building HLS arguments for URL: {0}, offset: {1}", url, offset);

			// For HLS streams, do NOT use -ss before input as it can cause FFmpeg to start from a middle segment
			// Instead, we'll use -ss after input if seeking is needed, and use -live_start_index to ensure
			// we start from the beginning when offset is zero

			// Base FFmpeg options
			sb.Append("-hide_banner -nostats -threads 1 ");

			// HLS-specific options for sequential playback
			// These flags enforce sequential chunk processing to prevent out-of-order playback
			sb.Append("-http_persistent 0 ");       // Disable persistent connections (forces sequential)
			sb.Append("-multiple_requests 0 ");     // Disable parallel requests
			sb.Append("-fflags +discardcorrupt ");  // Discard corrupt packets
			
			// Add HTTP headers to prevent 403 Forbidden errors from YouTube
			// YouTube requires proper User-Agent and Referer headers to access HLS segments
			// These headers mimic a browser request to avoid bot detection
			// FFmpeg expects headers in format: "Header1: value1\r\nHeader2: value2\r\n"
			// We need to escape quotes in the header value for the command line
			var userAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
			var referer = "https://www.youtube.com/";
			// Build header string with \r\n line breaks
			var headerValue = $"User-Agent: {userAgent}\r\nReferer: {referer}\r\n";
			// Escape quotes for command line (FFmpeg needs the header value in quotes)
			var escapedHeaderValue = headerValue.Replace("\"", "\\\"");
			sb.Append("-headers \"").Append(escapedHeaderValue).Append("\" ");
			Log.Debug("Added HTTP headers for YouTube HLS access (User-Agent and Referer)");
			
			// Add cookies if configured - YouTube requires cookies for HLS segment access
			// FFmpeg uses Netscape cookie file format (same as yt-dlp)
			var cookieFilePath = CookieFile?.Value;
			if (!string.IsNullOrEmpty(cookieFilePath))
			{
				if (File.Exists(cookieFilePath))
				{
					sb.Append("-cookies \"").Append(cookieFilePath).Append("\" ");
					Log.Debug("Added cookie file for YouTube HLS access: {0}", cookieFilePath);
				}
				else
				{
					Log.Warn("Cookie file specified but not found: {0}. HLS playback may fail with 403 errors.", cookieFilePath);
				}
			}
			else
			{
				Log.Debug("No cookie file configured. YouTube HLS playback may require cookies for some videos.");
			}
			
			// Force FFmpeg to start from the beginning of the HLS stream
			// -live_start_index 0 forces starting from the first segment (segment 0)
			// This prevents playback from starting in the middle of the stream
			// Note: This works for both live and VOD HLS streams
			if (offset <= TimeSpan.Zero)
			{
				sb.Append("-live_start_index 0 ");     // Start from segment 0 (first segment)
				Log.Debug("Added -live_start_index 0 to start from beginning of HLS stream");
			}

			Log.Debug("Added HLS sequential processing flags: -http_persistent 0 -multiple_requests 0 -fflags +discardcorrupt");

			// Custom HLS options from configuration
			var customHlsOptions = GetCustomHlsOptions();
			if (!string.IsNullOrEmpty(customHlsOptions))
			{
				sb.Append(customHlsOptions).Append(" ");
				Log.Info("Added custom HLS options from configuration: {0}", customHlsOptions);
			}

			// Input URL
			sb.Append("-i \"").Append(url).Append("\" ");

			// Always add -ss after input (but before output) for HLS streams to ensure we start from the correct position
			// For offset zero, explicitly seek to 0 to force starting from the beginning
			// For non-zero offset, seek to the specified time
			// IMPORTANT: -ss must come after -i but before the output format specification
			var seekTime = offset > TimeSpan.Zero ? offset : TimeSpan.Zero;
			var seek = string.Format(CultureInfo.InvariantCulture, @"-ss {0:hh\:mm\:ss\.fff} ", seekTime);
			sb.Append(seek);
			Log.Debug("Adding post-input seek to: {0}", seekTime);

			// Output format
			sb.Append("-ac 2 -ar 48000 -f s16le -acodec pcm_s16le pipe:1");

			var arguments = sb.ToString();
			Log.Info("Built FFmpeg HLS command: ffmpeg {0}", arguments);

			return arguments;
		}

		private static string GetCustomHlsOptions()
		{
			// Get custom HLS options from configuration
			var options = HlsOptions?.Value ?? string.Empty;
			
			if (string.IsNullOrEmpty(options))
			{
				return string.Empty;
			}
			
			try
			{
				// Validate HLS options
				var validatedOptions = ValidateHlsOptions(options);
				
				if (!string.IsNullOrEmpty(validatedOptions))
				{
					Log.Debug("Retrieved custom HLS options from configuration: {0}", validatedOptions);
					return validatedOptions;
				}
				else
				{
					Log.Warn("Invalid HLS options provided: '{0}'. Using default options.", options);
					return string.Empty;
				}
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Error parsing HLS options: '{0}'. Using default options.", options);
				return string.Empty;
			}
		}
		
		private static string ValidateHlsOptions(string options)
		{
			if (string.IsNullOrWhiteSpace(options))
			{
				return string.Empty;
			}
			
			// Trim the options
			options = options.Trim();
			
			// Basic validation: check for potentially dangerous characters or patterns
			// that could cause command injection or FFmpeg errors
			
			// Check for null bytes (command injection attempt)
			if (options.Contains('\0'))
			{
				Log.Warn("HLS options contain null bytes, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for newline characters (could break command parsing)
			if (options.Contains('\n') || options.Contains('\r'))
			{
				Log.Warn("HLS options contain newline characters, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for pipe characters (could be used for command chaining)
			if (options.Contains('|'))
			{
				Log.Warn("HLS options contain pipe characters, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for semicolons (could be used for command chaining)
			if (options.Contains(';'))
			{
				Log.Warn("HLS options contain semicolons, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for backticks (command substitution)
			if (options.Contains('`'))
			{
				Log.Warn("HLS options contain backticks, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for dollar signs followed by parentheses (command substitution)
			if (options.Contains("$(") || options.Contains("${"))
			{
				Log.Warn("HLS options contain command substitution patterns, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Check for ampersands (background execution or command chaining)
			if (options.Contains("&&") || options.Contains("&"))
			{
				Log.Warn("HLS options contain ampersands, rejecting: '{0}'", options);
				return string.Empty;
			}
			
			// Validate that options start with a dash (FFmpeg options should start with -)
			// Split by spaces and check each token
			var tokens = options.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			
			foreach (var token in tokens)
			{
				// Skip empty tokens
				if (string.IsNullOrWhiteSpace(token))
					continue;
					
				// If token starts with a dash, it's likely a valid FFmpeg option
				// If it doesn't start with a dash and isn't a value (previous token was an option),
				// it might be valid (e.g., "-option value")
				// We'll allow tokens that don't start with dash as they could be option values
				
				// Check for obviously invalid patterns in individual tokens
				if (token.Contains(".."))
				{
					Log.Warn("HLS options contain suspicious path traversal pattern, rejecting: '{0}'", options);
					return string.Empty;
				}
			}
			
			// If all validation passes, return the options
			Log.Debug("HLS options validated successfully: '{0}'", options);
			return options;
		}

		public void Dispose()
		{
			StopFfmpegProcess();
		}

		private class FfmpegInstance
		{
			public Process FfmpegProcess { get; }
			public bool HasTriedToReconnect { get; set; }
			public string ReconnectUrl { get; }
			public bool IsIcyStream => IcyStream != null;

			public PreciseAudioTimer AudioTimer { get; }
			public TimeSpan? ParsedSongLength { get; set; } = null;

			public Stream? IcyStream { get; }
			public int IcyMetaInt { get; }
			public bool Closed { get; set; }

			public Action<SongInfoChanged>? OnMetaUpdated;

			// Position tracking for chunk ordering diagnostics
			public TimeSpan LastReportedPosition { get; set; } = TimeSpan.Zero;
			public bool IsHlsPlayback { get; set; }
			public bool HasDetectedPositionJump { get; set; }

			public FfmpegInstance(string url, PreciseAudioTimer timer) : this(url, timer, null!, 0) { }
			public FfmpegInstance(string url, PreciseAudioTimer timer, Stream icyStream, int icyMetaInt)
			{
				FfmpegProcess = new Process();
				ReconnectUrl = url;
				AudioTimer = timer;
				IcyStream = icyStream;
				IcyMetaInt = icyMetaInt;

				HasTriedToReconnect = false;
			}

			public void Close()
			{
				Closed = true;

				try
				{
					if (!FfmpegProcess.HasExitedSafe())
						FfmpegProcess.Kill();
				}
				catch { }
				try { FfmpegProcess.CancelErrorRead(); } catch { }
				try { FfmpegProcess.StandardInput.Dispose(); } catch { }
				try { FfmpegProcess.StandardOutput.Dispose(); } catch { }
				try { FfmpegProcess.Dispose(); } catch { }

				IcyStream?.Dispose();
			}

			public void FfmpegProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
			{
				if (e.Data is null)
					return;

				if (sender != FfmpegProcess)
					throw new InvalidOperationException("Wrong process associated to event");

				if (ParsedSongLength is null)
				{
					var match = FindDurationMatch.Match(e.Data);
					if (!match.Success)
						return;

					int hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
					int minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
					int seconds = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
					int millisec = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) * 10;
					ParsedSongLength = new TimeSpan(0, hours, minutes, seconds, millisec);
				}

				//if (!HasIcyTag && e.Data.AsSpan().TrimStart().StartsWith("icy-".AsSpan()))
				//{
				//	HasIcyTag = true;
				//}
			}

			public void ReadStreamLoop(Id id)
			{
				if (IcyStream is null)
					throw new InvalidOperationException("Instance is not an icy stream");

				Tools.SetLogId(id.ToString());
				const int IcyMaxMeta = 255 * 16;
				const int ReadBufferSize = 4096;

				int errorCount = 0;
				var buffer = new byte[Math.Max(ReadBufferSize, IcyMaxMeta)];
				int readCount = 0;

				while (!Closed)
				{
					try
					{
						while (readCount < IcyMetaInt)
						{
							int read = IcyStream.Read(buffer, 0, Math.Min(ReadBufferSize, IcyMetaInt - readCount));
							if (read == 0)
							{
								Close();
								return;
							}
							readCount += read;
							FfmpegProcess.StandardInput.BaseStream.Write(buffer, 0, read);
							errorCount = 0;
						}
						readCount = 0;

						var metaByte = IcyStream.ReadByte();
						if (metaByte < 0)
						{
							Close();
							return;
						}

						if (metaByte > 0)
						{
							metaByte *= 16;
							while (readCount < metaByte)
							{
								int read = IcyStream.Read(buffer, 0, metaByte - readCount);
								if (read == 0)
								{
									Close();
									return;
								}
								readCount += read;
							}
							readCount = 0;

							var metaString = Tools.Utf8Encoder.GetString(buffer, 0, metaByte).TrimEnd('\0');
							Log.Debug("Meta: {0}", metaString);
							OnMetaUpdated?.Invoke(ParseIcyMeta(metaString));
						}
					}
					catch (Exception ex)
					{
						errorCount++;
						if (errorCount >= 50)
						{
							Log.Error(ex, "Failed too many times trying to access ffmpeg. Closing stream.");
							Close();
							return;
						}

						if (ex is InvalidOperationException)
						{
							Log.Debug(ex, "Waiting for ffmpeg");
							Thread.Sleep(100);
						}
						else
						{
							Log.Debug(ex, "Stream read/write error");
						}
					}
				}
			}

			private static SongInfoChanged ParseIcyMeta(string metaString)
			{
				var songInfo = new SongInfoChanged();
				var match = IcyMetadataMacher.Match(metaString);
				if (match.Success)
				{
					for (int i = 0; i < match.Groups[1].Captures.Count; i++)
					{
						switch (match.Groups[2].Captures[i].Value.ToUpperInvariant())
						{
						case "STREAMTITLE":
							songInfo.Title = match.Groups[3].Captures[i].Value;
							break;
						}
					}
				}
				return songInfo;
			}
		}
	}
}

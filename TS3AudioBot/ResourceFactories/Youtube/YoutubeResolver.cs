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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TS3AudioBot.Audio;
using TS3AudioBot.Config;
using TS3AudioBot.Helper;
using TS3AudioBot.Localization;
using TS3AudioBot.Playlists;
using TS3AudioBot.ResourceFactories.AudioTags;
using TSLib.Helper;

namespace TS3AudioBot.ResourceFactories.Youtube
{
	public sealed class YoutubeResolver : IResourceResolver, IPlaylistResolver, IThumbnailResolver, ISearchResolver, IDisposable
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private static readonly Regex IdMatch = new Regex(@"(?:(?:&|\?)v=|youtu\.be\/)([\w\-_]{11})", Util.DefaultRegexConfig);
		private static readonly Regex YtTimestampMatch = new Regex(@"(?:&|\?)t=(\d+)", Util.DefaultRegexConfig);
		private static readonly Regex LinkMatch = new Regex(@"^(https?\:\/\/)?(www\.|m\.)?(youtube\.|youtu\.be)", Util.DefaultRegexConfig);
		private static readonly Regex ListMatch = new Regex(@"(&|\?)list=([\w\-_]+)", Util.DefaultRegexConfig);
		private static readonly Regex StreamCodecMatch = new Regex(@"CODECS=""([^""]*)""", Util.DefaultRegexConfig);
		private static readonly Regex StreamBitrateMatch = new Regex(@"BANDWIDTH=(\d+)", Util.DefaultRegexConfig);
		private string YoutubeProjectId => conf.ApiKey.Value;
		private readonly ConfResolverYoutube conf;
		private readonly string tempDownloadDir;

		public YoutubeResolver(ConfResolverYoutube conf)
		{
			this.conf = conf;
			// Configure YoutubeDlHelper with cookie and extractor-args settings
			YoutubeDlHelper.CookieFile = conf.CookieFile;
			YoutubeDlHelper.ExtractorArgs = conf.ExtractorArgs;
			
			// Configure FfmpegProducer with HLS options and cookies
			Audio.FfmpegProducer.HlsOptions = conf.HlsOptions;
			Audio.FfmpegProducer.CookieFile = conf.CookieFile;
			
			// Set up temp download directory
			tempDownloadDir = Path.Combine(Path.GetTempPath(), "ts3audiobot_ytdl");
			Log.Debug("Temp download directory configured: {0}", tempDownloadDir);
		}

		public string ResolverFor => "youtube";

		public MatchCertainty MatchResource(ResolveContext? _, string uri) =>
			LinkMatch.IsMatch(uri) || IdMatch.IsMatch(uri)
				? MatchCertainty.Always
				: MatchCertainty.Never;

		public MatchCertainty MatchPlaylist(ResolveContext? _, string uri) => ListMatch.IsMatch(uri) ? MatchCertainty.Always : MatchCertainty.Never;

		public async Task<PlayResource> GetResource(ResolveContext? _, string uri)
		{
			Match matchYtId = IdMatch.Match(uri);
			if (!matchYtId.Success)
				throw Error.LocalStr(strings.error_media_failed_to_parse_id);

			var play = await GetResourceById(null, new AudioResource(matchYtId.Groups[1].Value, null, ResolverFor));
			Match matchTimestamp = YtTimestampMatch.Match(uri);
			if (matchYtId.Success && int.TryParse(matchTimestamp.Groups[1].Value, out var secs))
			{
				play.PlayInfo ??= new PlayInfo();
				play.PlayInfo.StartOffset = TimeSpan.FromSeconds(secs);
			}
			return play;
		}

		public async Task<PlayResource> GetResourceById(ResolveContext? _, AudioResource resource)
		{
			var priority = conf.ResolverPriority.Value;
			
			// Deprecate internal scraper - always use yt-dlp
			if (priority == LoaderPriority.Internal)
			{
				Log.Warn("Internal YouTube scraper is deprecated and has been removed. Using yt-dlp instead.");
			}
			
			// Always use yt-dlp regardless of priority setting
			return await YoutubeDlWrapped(resource);
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private async Task<PlayResource> ResolveResourceInternal(AudioResource resource)
		{
			var resulthtml = await WebWrapper.Request($"https://www.youtube.com/get_video_info?video_id={resource.ResourceId}").AsString();

			var videoTypes = new List<VideoData>();
			var dataParse = ParseQueryString(resulthtml);

			if (dataParse.TryGetValue("player_response", out var playerData))
			{
				var parsed = JsonConvert.DeserializeObject<JsonPlayerResponse>(playerData[0]);
				Log.Debug("Extracted data: {@playerData}", parsed);

				if (parsed?.videoDetails != null)
				{
					resource.ResourceTitle ??= parsed.videoDetails.title;

					bool isLive = parsed.videoDetails.isLive ?? false;
					if (isLive && parsed.streamingData?.hlsManifestUrl != null)
					{
						return await ParseLiveData(resource, parsed.streamingData.hlsManifestUrl);
					}
					else if (isLive)
					{
						Log.Warn("Live stream without hls stream data");
					}

					ParsePlayerData(parsed, videoTypes);
				}
			}

			if (dataParse.TryGetValue("url_encoded_fmt_stream_map", out var videoDataUnsplit))
				ParseEncodedFmt(videoDataUnsplit, videoTypes);

			if (dataParse.TryGetValue("adaptive_fmts", out videoDataUnsplit))
				ParseAdaptiveFmt(videoDataUnsplit, videoTypes);

			// Validation Process

			if (videoTypes.Count <= 0)
				throw Error.LocalStr(strings.error_media_no_stream_extracted);

			int codec = SelectStream(videoTypes);
			if (codec < 0)
				throw Error.LocalStr(strings.error_media_no_stream_extracted);

			await ValidateMedia(videoTypes[codec]);

			resource.ResourceTitle ??= $"<YT - no title : {resource.ResourceId}>";

			return new PlayResource(videoTypes[codec].Link, resource);
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static async Task<PlayResource> ParseLiveData(AudioResource resource, string requestUrl)
		{
			List<M3uEntry>? webList = null;
			try
			{
				webList = await WebWrapper.Request(requestUrl).ToAction(async response =>
					await M3uReader.TryGetData(await response.Content.ReadAsStreamAsync())
				);
			}
			catch (Exception ex) { throw Error.Exception(ex).LocalStr(strings.error_media_internal_invalid); }

			const string AacHe = "mp4a.40.5";
			const string AacLc = "mp4a.40.2";

			var streamPref = from item in webList
							 let codecs = item.StreamMeta != null ? StreamCodecMatch.Match(item.StreamMeta).Groups[1].Value : ""
							 let codecPref = codecs.Contains(AacLc) ? 0
								 : codecs.Contains(AacHe) ? 1
								 : 2
							 let bitrate = item.StreamMeta != null ? int.Parse(StreamBitrateMatch.Match(item.StreamMeta).Groups[1].Value) : int.MaxValue
							 orderby codecPref, bitrate ascending
							 select item;
			var streamSelect = streamPref.FirstOrDefault();
			if (streamSelect is null)
				throw Error.LocalStr(strings.error_media_no_stream_extracted);
			return new PlayResource(streamSelect.TrackUrl, resource);
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static void ParsePlayerData(JsonPlayerResponse data, List<VideoData> videoTypes)
		{
			// TODO
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static void ParseEncodedFmt(List<string> videoDataUnsplit, List<VideoData> videoTypes)
		{
			if (videoDataUnsplit.Count == 0)
				return;
			string[] videoData = videoDataUnsplit[0].Split(',');

			foreach (string vdat in videoData)
			{
				var videoparse = ParseQueryString(vdat);

				if (!videoparse.TryGetValue("url", out var vLink))
					continue;

				if (!videoparse.TryGetValue("type", out var vType))
					continue;

				if (!videoparse.TryGetValue("quality", out var vQuality))
					continue;

				var vt = new VideoData(vLink[0], vQuality[0], GetCodec(vType[0]));
				videoTypes.Add(vt);
			}
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static void ParseAdaptiveFmt(List<string> videoDataUnsplit, List<VideoData> videoTypes)
		{
			if (videoDataUnsplit.Count == 0)
				return;

			string[] videoData = videoDataUnsplit[0].Split(',');

			foreach (string vdat in videoData)
			{
				var videoparse = ParseQueryString(vdat);

				if (!videoparse.TryGetValue("type", out var vTypeArr))
					continue;
				var vType = vTypeArr[0];

				bool audioOnly = false;
				if (vType.StartsWith("video/", StringComparison.Ordinal))
					continue;
				else if (vType.StartsWith("audio/", StringComparison.Ordinal))
					audioOnly = true;

				if (!videoparse.TryGetValue("url", out var vLink))
					continue;

				var vt = new VideoData(vLink[0], vType, GetCodec(vType), audioOnly, !audioOnly);
				videoTypes.Add(vt);
			}
		}

		public string RestoreLink(ResolveContext _, AudioResource resource) => "https://youtu.be/" + resource.ResourceId;

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static int SelectStream(List<VideoData> list)
		{
			if (Log.IsTraceEnabled)
			{
				var dbg = new System.Text.StringBuilder("YT avail codecs: ");
				foreach (var yd in list)
					dbg.Append(yd.Qualitydesciption).Append(" @ ").Append(yd.Codec).Append(", ");
				Log.Trace("{0}", dbg);
			}

			int autoselectIndex = list.FindIndex(t => t.Codec == VideoCodec.M4A);
			if (autoselectIndex == -1)
				autoselectIndex = list.FindIndex(t => t.AudioOnly);
			if (autoselectIndex == -1)
				autoselectIndex = list.FindIndex(t => !t.VideoOnly);

			return autoselectIndex;
		}

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static Task ValidateMedia(VideoData media) => WebWrapper.Request(media.Link).Send();

		[Obsolete("Internal YouTube scraper is deprecated. Use yt-dlp instead.")]
		private static VideoCodec GetCodec(string type)
		{
			string lowtype = type.ToLowerInvariant();
			bool audioOnly = false;
			string codecSubStr;
			if (lowtype.StartsWith("video/", StringComparison.Ordinal))
			{
				codecSubStr = lowtype.Substring("video/".Length);
			}
			else if (lowtype.StartsWith("audio/", StringComparison.Ordinal))
			{
				codecSubStr = lowtype.Substring("audio/".Length);
				audioOnly = true;
			}
			else return VideoCodec.Unknown;

			int codecEnd = codecSubStr.IndexOf(';');
			var extractedCodec = codecEnd >= 0 ? codecSubStr.Substring(0, codecEnd) : codecSubStr;

			switch (extractedCodec)
			{
			case "mp4":
				if (audioOnly)
					return VideoCodec.M4A;
				return VideoCodec.Mp4;
			case "x-flv":
				return VideoCodec.Flv;
			case "3gpp":
				return VideoCodec.ThreeGp;
			case "webm":
				return VideoCodec.Webm;
			default:
				return VideoCodec.Unknown;
			}
		}

		public async Task<Playlist> GetPlaylist(ResolveContext _, string url)
		{
			Match matchYtId = ListMatch.Match(url);
			if (!matchYtId.Success)
				throw Error.LocalStr(strings.error_media_failed_to_parse_id);

			string id = matchYtId.Groups[2].Value;
			if (string.IsNullOrEmpty(YoutubeProjectId))
				return await GetPlaylistYoutubeDl(id, url);
			else
				return await GetPlaylistYoutubeApi(id);
		}

		private async Task<Playlist> GetPlaylistYoutubeApi(string id)
		{
			var plist = new Playlist().SetTitle(id);

			string? nextToken = null;
			do
			{
				var parsed = await WebWrapper.Request("https://www.googleapis.com/youtube/v3/playlistItems"
						+ "?part=contentDetails,snippet"
						+ "&fields=" + Uri.EscapeDataString("items(contentDetails/videoId,snippet/title),nextPageToken")
						+ "&maxResults=50"
						+ "&playlistId=" + id
						+ (nextToken != null ? "&pageToken=" + nextToken : string.Empty)
						+ "&key=" + YoutubeProjectId).AsJson<JsonVideoListResponse>();

				var videoItems = parsed.items;
				if (!plist.AddRange(
					videoItems.Select(item =>
						new PlaylistItem(
							new AudioResource(
								item.contentDetails?.videoId ?? throw new NullReferenceException("item.contentDetails.videoId was null"),
								item.snippet?.title,
								ResolverFor
							)
						)
					)
				)) break;

				nextToken = parsed.nextPageToken;
			} while (nextToken != null);

			return plist;
		}

		private async Task<Playlist> GetPlaylistYoutubeDl(string id, string url)
		{
			var plistData = await YoutubeDlHelper.GetPlaylistAsync(url);
			var plist = new Playlist().SetTitle(plistData.title ?? $"youtube-{id}");
			plist.AddRange(plistData.entries
				.Where(entry => entry.id != null)
				.Select(entry => new PlaylistItem(
					new AudioResource(
						entry.id!,
						entry.title,
						ResolverFor
					)
				)));

			return plist;
		}

		private async Task<PlayResource> YoutubeDlWrapped(AudioResource resource)
		{
			Log.Debug("Using yt-dlp for video: {0}", resource.ResourceId);

			// Read playback mode configuration
			var playbackMode = conf.PlaybackMode?.Value ?? PlaybackMode.HlsStreaming;
			Log.Info("Playback mode for video {0}: {1}", resource.ResourceId, playbackMode);

			// Try direct download if configured
			if (playbackMode == PlaybackMode.DirectDownload || playbackMode == PlaybackMode.Auto)
			{
				Log.Info("Attempting direct download for video {0}", resource.ResourceId);
				var downloadResult = await YoutubeDlHelper.DownloadVideo(resource.ResourceId, tempDownloadDir);

				if (downloadResult.Ok)
				{
					Log.Info("Direct download succeeded for video {0}: {1}", 
						resource.ResourceId, downloadResult.Value);

					// Get metadata for the downloaded file
					var downloadResponse = await YoutubeDlHelper.GetSingleVideo(resource.ResourceId);
					resource.ResourceTitle = downloadResponse.AutoTitle ?? $"Youtube-{resource.ResourceId}";
					var downloadSongInfo = YoutubeDlHelper.MapToSongInfo(downloadResponse);

					// Return PlayResource with local file path
					return new PlayResource(downloadResult.Value, resource, songInfo: downloadSongInfo)
					{
						IsTemporaryFile = true,
						TemporaryFilePath = downloadResult.Value
					};
				}
				else
				{
					Log.Warn("Direct download failed for video {0}: {1}", 
						resource.ResourceId, downloadResult.Error);

					// If DirectDownload mode, fail immediately without fallback
					if (playbackMode == PlaybackMode.DirectDownload)
					{
						Log.Error("DirectDownload mode enabled but download failed. Not falling back to streaming.");
						throw Error.LocalStr("Direct download failed: " + downloadResult.Error);
					}

					// Auto mode: fall back to streaming
					Log.Info("Auto mode: Falling back to HLS streaming for video {0}", resource.ResourceId);
				}
			}

			// Use streaming mode (HLS or direct URL)
			Log.Debug("Using streaming mode for video {0}", resource.ResourceId);
			
			JsonYtdlDump response;
			try
			{
				response = await YoutubeDlHelper.GetSingleVideo(resource.ResourceId);
			}
			catch (Exception ex)
			{
				// If format extraction fails (e.g., signature solving failed), try direct download as fallback
				Log.Warn("Format extraction failed for video {0}: {1}. Attempting direct download fallback.", 
					resource.ResourceId, ex.Message);
				
				var downloadResult = await YoutubeDlHelper.DownloadVideo(resource.ResourceId, tempDownloadDir);
				if (downloadResult.Ok)
				{
					Log.Info("Direct download fallback succeeded for video {0}: {1}", 
						resource.ResourceId, downloadResult.Value);
					
					// Try to get metadata (may fail, but that's okay)
					try
					{
						var downloadResponse = await YoutubeDlHelper.GetSingleVideo(resource.ResourceId);
						resource.ResourceTitle = downloadResponse.AutoTitle ?? $"Youtube-{resource.ResourceId}";
						var downloadSongInfo = YoutubeDlHelper.MapToSongInfo(downloadResponse);
						return new PlayResource(downloadResult.Value, resource, songInfo: downloadSongInfo)
						{
							IsTemporaryFile = true,
							TemporaryFilePath = downloadResult.Value
						};
					}
					catch
					{
						// If metadata extraction also fails, just use the downloaded file
						resource.ResourceTitle = $"Youtube-{resource.ResourceId}";
						return new PlayResource(downloadResult.Value, resource)
						{
							IsTemporaryFile = true,
							TemporaryFilePath = downloadResult.Value
						};
					}
				}
				else
				{
					Log.Error("Both format extraction and direct download failed for video {0}. Format error: {1}, Download error: {2}", 
						resource.ResourceId, ex.Message, downloadResult.Error);
					throw Error.LocalStr($"Failed to extract video formats: {ex.Message}. Direct download also failed: {downloadResult.Error}");
				}
			}
			
			resource.ResourceTitle = response.AutoTitle ?? $"Youtube-{resource.ResourceId}";
			var songInfo = YoutubeDlHelper.MapToSongInfo(response);

			// Use enhanced format selection (prioritizes direct URLs)
			var format = YoutubeDlHelper.FilterBestEnhanced(response.formats);
			var url = format?.url;

			if (string.IsNullOrEmpty(url))
			{
				Log.Warn("No suitable format found in streaming mode for video {0}. Available formats: {@formats}. Attempting direct download fallback.", 
					resource.ResourceId, response.formats);
				
				// Fallback to direct download if no formats available
				var downloadResult = await YoutubeDlHelper.DownloadVideo(resource.ResourceId, tempDownloadDir);
				if (downloadResult.Ok)
				{
					Log.Info("Direct download fallback succeeded for video {0}: {1}", 
						resource.ResourceId, downloadResult.Value);
					return new PlayResource(downloadResult.Value, resource, songInfo: songInfo)
					{
						IsTemporaryFile = true,
						TemporaryFilePath = downloadResult.Value
					};
				}
				else
				{
					Log.Error("No suitable format found for video {0} and direct download also failed. Available formats: {@formats}, Download error: {1}", 
						resource.ResourceId, response.formats, downloadResult.Error);
					throw Error.LocalStr(strings.error_ytdl_empty_response);
				}
			}

			// Check if the URL is an HLS manifest and log accordingly
			bool isHls = YoutubeDlHelper.IsHlsManifest(url);
			var formatType = isHls ? "HLS manifest" : "direct URL";

			Log.Info("Selected {0} for video {1}: format_id={2}, codec={3}, audio-only={4}, resolution={5}x{6}, bitrate={7}",
				formatType,
				resource.ResourceId,
				format.format_id,
				format.acodec,
				format.vcodec == "none",
				format.width ?? 0,
				format.height ?? 0,
				format.abr?.ToString() ?? "null");

			Log.Debug("youtube-dl succeeded!");

			// PlayResource constructor does not modify the URL - it's passed through as-is
			return new PlayResource(url, resource, songInfo: songInfo);
		}

		public static Dictionary<string, List<string>> ParseQueryString(string requestQueryString)
		{
			var rc = new Dictionary<string, List<string>>();
			string[] ar1 = requestQueryString.Split('&', '?');
			foreach (string row in ar1)
			{
				if (string.IsNullOrEmpty(row)) continue;
				int index = row.IndexOf('=');
				if (index < 0) continue;
				var param = Uri.UnescapeDataString(row.Substring(0, index).Replace('+', ' '));

				var list = rc.GetOrNew(param);
				list.Add(Uri.UnescapeDataString(row.Substring(index + 1).Replace('+', ' ')));
			}
			return rc;
		}

		public Task GetThumbnail(ResolveContext _, PlayResource playResource, Func<Stream, Task> action)
		{
			// default  :  120px/ 90px /default.jpg
			// medium   :  320px/180px /mqdefault.jpg
			// high     :  480px/360px /hqdefault.jpg
			// standard :  640px/480px /sddefault.jpg
			// maxres   : 1280px/720px /maxresdefault.jpg
			return WebWrapper
				.Request($"https://i.ytimg.com/vi/{playResource.AudioResource.ResourceId}/mqdefault.jpg")
				.ToStream(action);
		}

		public async Task<IList<AudioResource>> Search(ResolveContext _, string keyword)
		{
			if (string.IsNullOrEmpty(YoutubeProjectId))
				return await SearchYoutubeDlAsync(keyword);
			else
				return await SearchYoutubeApi(keyword);
		}

		public async Task<IList<AudioResource>> SearchYoutubeApi(string keyword)
		{
			const int maxResults = 10;
			var parsed = await WebWrapper.Request(
					"https://www.googleapis.com/youtube/v3/search"
					+ "?part=snippet"
					+ "&fields=" + Uri.EscapeDataString("items(id/videoId,snippet(channelTitle,title))")
					+ "&type=video"
					+ "&safeSearch=none"
					+ "&q=" + Uri.EscapeDataString(keyword)
					+ "&maxResults=" + maxResults
					+ "&key=" + YoutubeProjectId).AsJson<JsonSearchListResponse>();

			return parsed.items.Select(item => new AudioResource(
				item.id?.videoId ?? throw new NullReferenceException("item.id.videoId was null"),
				item.snippet?.title,
				ResolverFor)).ToArray();
		}

		public async Task<IList<AudioResource>> SearchYoutubeDlAsync(string keyword)
		{
			var search = await YoutubeDlHelper.GetSearchAsync(keyword);

			return search.entries
				.Where(entry => entry.id != null)
				.Select(entry => new AudioResource(
					entry.id!,
					entry.title,
					ResolverFor
				)).ToArray();
		}

		public void Dispose()
		{
			// Clean up temp download directory
			try
			{
				if (Directory.Exists(tempDownloadDir))
				{
					Log.Debug("Cleaning up temp download directory: {0}", tempDownloadDir);
					var files = Directory.GetFiles(tempDownloadDir);
					foreach (var file in files)
					{
						try
						{
							File.Delete(file);
							Log.Debug("Cleaned up temp file: {0}", file);
						}
						catch (Exception ex)
						{
							Log.Warn(ex, "Failed to delete temp file: {0}", file);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Failed to clean up temp download directory");
			}
		}
	}
}

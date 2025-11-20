// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.IO;
using NLog;
using TS3AudioBot.Audio;
using TS3AudioBot.CommandSystem.CommandResults;

namespace TS3AudioBot.ResourceFactories
{
	public class PlayResource : IAudioResourceResult, IMetaContainer
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		public AudioResource AudioResource { get; }
		public string PlayUri { get; }
		public PlayInfo? PlayInfo { get; set; }
		public SongInfo? SongInfo { get; set; }

		/// <summary>
		/// Indicates whether this PlayResource uses a temporary file that should be cleaned up after playback.
		/// </summary>
		public bool IsTemporaryFile { get; set; }

		/// <summary>
		/// The path to the temporary file, if IsTemporaryFile is true.
		/// </summary>
		public string? TemporaryFilePath { get; set; }

		/// <summary>
		/// Creates a new PlayResource with the specified URI and metadata.
		/// The URI is stored as-is without any modification, ensuring HLS manifests
		/// and other special URLs are passed through to the player unchanged.
		/// </summary>
		/// <param name="uri">The playback URI (direct media URL or HLS manifest)</param>
		/// <param name="baseData">Base audio resource metadata</param>
		/// <param name="playInfo">Optional playback information (e.g., start offset)</param>
		/// <param name="songInfo">Optional song metadata (title, artist, etc.)</param>
		public PlayResource(string uri, AudioResource baseData, PlayInfo? playInfo = null, SongInfo? songInfo = null)
		{
			AudioResource = baseData;
			// Store URI without modification - HLS manifests and other URLs are passed through as-is
			PlayUri = uri;
			PlayInfo = playInfo;
			SongInfo = songInfo;
		}

		/// <summary>
		/// Cleans up the temporary file associated with this PlayResource, if any.
		/// This method is safe to call multiple times and will handle errors gracefully.
		/// </summary>
		public void CleanupTemporaryFile()
		{
			if (!IsTemporaryFile || string.IsNullOrEmpty(TemporaryFilePath))
			{
				return;
			}

			try
			{
				if (File.Exists(TemporaryFilePath))
				{
					File.Delete(TemporaryFilePath);
					Log.Debug("Deleted temporary file: {0}", TemporaryFilePath);
				}
				else
				{
					Log.Debug("Temporary file already deleted or does not exist: {0}", TemporaryFilePath);
				}
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Failed to delete temporary file: {0}", TemporaryFilePath);
			}
		}

		public override string ToString() => AudioResource.ToString();
	}
}

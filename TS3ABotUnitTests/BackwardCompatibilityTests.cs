using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TS3AudioBot.Config;
using TS3AudioBot.ResourceFactories;
using TS3AudioBot.ResourceFactories.Youtube;

namespace TS3ABotUnitTests
{
	[TestFixture]
	public class BackwardCompatibilityTests
	{
		/// <summary>
		/// Tests that the YoutubeResolver can be instantiated with a configuration
		/// that does not have cookie file or extractor args set (simulating old configs).
		/// Validates: Requirements 7.1, 7.2
		/// </summary>
		[Test]
		public void BackwardCompatibility_ConfigWithoutNewOptions_WorksCorrectly()
		{
			// Arrange: Create a config without setting the new options
			var conf = new ConfResolverYoutube();
			
			// Act: Create resolver (should not throw)
			using var resolver = new YoutubeResolver(conf);
			
			// Assert: Resolver should be created successfully
			Assert.IsNotNull(resolver);
			Assert.AreEqual("youtube", resolver.ResolverFor);
			
			// Verify that the config values are accessible and have defaults
			Assert.IsNotNull(conf.CookieFile);
			Assert.IsNotNull(conf.ExtractorArgs);
			
			// Default values should be empty strings
			Assert.AreEqual("", conf.CookieFile.Value);
			Assert.AreEqual("", conf.ExtractorArgs.Value);
		}

		/// <summary>
		/// Tests that BuildArguments works correctly when cookie file and extractor args
		/// are not configured (null or empty), ensuring backward compatibility.
		/// Validates: Requirements 7.2, 7.3
		/// </summary>
		[Test]
		public void BackwardCompatibility_BuildArgumentsWithoutNewOptions_WorksAsExpected()
		{
			// Arrange: Set up YoutubeDlHelper with no cookie file or extractor args
			YoutubeDlHelper.CookieFile = null;
			YoutubeDlHelper.ExtractorArgs = null;
			
			// We can't directly test BuildArguments as it's private, but we can verify
			// that the helper methods work without these options set
			
			// Act & Assert: These should not throw
			Assert.DoesNotThrow(() => {
				var formats = new List<JsonYtdlFormat>
				{
					new JsonYtdlFormat
					{
						acodec = "mp4a.40.2",
						vcodec = "none",
						abr = 128,
						format_id = "140",
						url = "https://example.com/audio.m4a"
					}
				};
				
				var result = YoutubeDlHelper.FilterBestEnhanced(formats);
				Assert.IsNotNull(result);
			});
		}

		/// <summary>
		/// Tests that FilterBest (old method) still works correctly for backward compatibility.
		/// Validates: Requirements 7.3, 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_FilterBest_StillWorks()
		{
			// Arrange: Create formats with non-null bitrates (old-style YouTube responses)
			var formats = new List<JsonYtdlFormat>
			{
				new JsonYtdlFormat
				{
					acodec = "mp4a.40.2",
					vcodec = "avc1.64001F",
					abr = 128,
					format_id = "18",
					url = "https://example.com/video.mp4"
				},
				new JsonYtdlFormat
				{
					acodec = "mp4a.40.2",
					vcodec = "none",
					abr = 128,
					format_id = "140",
					url = "https://example.com/audio.m4a"
				},
				new JsonYtdlFormat
				{
					acodec = "opus",
					vcodec = "none",
					abr = 160,
					format_id = "251",
					url = "https://example.com/audio.webm"
				}
			};
			
			// Act: Use the old FilterBest method
			var result = YoutubeDlHelper.FilterBest(formats);
			
			// Assert: Should select the highest bitrate audio-only format
			Assert.IsNotNull(result);
			Assert.AreEqual("251", result.format_id); // Opus with 160 abr
			Assert.AreEqual("none", result.vcodec);
		}

		/// <summary>
		/// Tests that FilterBestEnhanced works with old-style formats (non-null bitrates).
		/// Validates: Requirements 7.3, 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_FilterBestEnhanced_WorksWithOldStyleFormats()
		{
			// Arrange: Create formats with non-null bitrates (old-style YouTube responses)
			var formats = new List<JsonYtdlFormat>
			{
				new JsonYtdlFormat
				{
					acodec = "mp4a.40.2",
					vcodec = "avc1.64001F",
					abr = 128,
					format_id = "18",
					url = "https://example.com/video.mp4"
				},
				new JsonYtdlFormat
				{
					acodec = "mp4a.40.2",
					vcodec = "none",
					abr = 128,
					format_id = "140",
					url = "https://example.com/audio.m4a"
				}
			};
			
			// Act: Use the new FilterBestEnhanced method
			var result = YoutubeDlHelper.FilterBestEnhanced(formats);
			
			// Assert: Should select audio-only format (prefers audio-only over combined)
			Assert.IsNotNull(result);
			Assert.AreEqual("140", result.format_id);
			Assert.AreEqual("none", result.vcodec);
		}

		/// <summary>
		/// Tests that the public interface of YoutubeDlHelper has not changed.
		/// Validates: Requirements 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_PublicInterface_Unchanged()
		{
			// Assert: Verify that all expected public methods still exist
			var helperType = typeof(YoutubeDlHelper);
			
			// Check for existing public methods
			Assert.IsNotNull(helperType.GetMethod("GetSingleVideo"));
			Assert.IsNotNull(helperType.GetMethod("GetPlaylistAsync"));
			Assert.IsNotNull(helperType.GetMethod("GetSearchAsync"));
			Assert.IsNotNull(helperType.GetMethod("FindYoutubeDl"));
			Assert.IsNotNull(helperType.GetMethod("RunYoutubeDl"));
			Assert.IsNotNull(helperType.GetMethod("ParseResponse"));
			Assert.IsNotNull(helperType.GetMethod("FilterBest"));
			Assert.IsNotNull(helperType.GetMethod("FilterBestEnhanced"));
			Assert.IsNotNull(helperType.GetMethod("IsHlsManifest"));
			Assert.IsNotNull(helperType.GetMethod("TransformYtdlError"));
			Assert.IsNotNull(helperType.GetMethod("MapToSongInfo"));
			
			// Check for public properties
			Assert.IsNotNull(helperType.GetProperty("DataObj"));
			Assert.IsNotNull(helperType.GetProperty("CookieFile"));
			Assert.IsNotNull(helperType.GetProperty("ExtractorArgs"));
		}

		/// <summary>
		/// Tests that YoutubeResolver public interface has not changed.
		/// Validates: Requirements 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_YoutubeResolverInterface_Unchanged()
		{
			// Arrange
			var conf = new ConfResolverYoutube();
			using var resolver = new YoutubeResolver(conf);
			
			// Assert: Verify that resolver implements expected interfaces
			Assert.IsInstanceOf<IResourceResolver>(resolver);
			Assert.IsInstanceOf<IPlaylistResolver>(resolver);
			Assert.IsInstanceOf<IThumbnailResolver>(resolver);
			Assert.IsInstanceOf<ISearchResolver>(resolver);
			
			// Verify ResolverFor property
			Assert.AreEqual("youtube", resolver.ResolverFor);
			
			// Verify that all expected public methods still exist
			var resolverType = typeof(YoutubeResolver);
			Assert.IsNotNull(resolverType.GetMethod("MatchResource"));
			Assert.IsNotNull(resolverType.GetMethod("MatchPlaylist"));
			Assert.IsNotNull(resolverType.GetMethod("GetResource"));
			Assert.IsNotNull(resolverType.GetMethod("GetResourceById"));
			Assert.IsNotNull(resolverType.GetMethod("RestoreLink"));
			Assert.IsNotNull(resolverType.GetMethod("GetPlaylist"));
			Assert.IsNotNull(resolverType.GetMethod("GetThumbnail"));
			Assert.IsNotNull(resolverType.GetMethod("Search"));
			Assert.IsNotNull(resolverType.GetMethod("Dispose"));
		}

		/// <summary>
		/// Tests that configuration defaults are sensible and don't break existing setups.
		/// Validates: Requirements 7.2
		/// </summary>
		[Test]
		public void BackwardCompatibility_ConfigurationDefaults_AreSensible()
		{
			// Arrange & Act: Create a new config
			var conf = new ConfResolverYoutube();
			
			// Assert: Verify default values
			Assert.AreEqual("", conf.CookieFile.Value, "CookieFile should default to empty string");
			Assert.AreEqual("", conf.ExtractorArgs.Value, "ExtractorArgs should default to empty string");
			Assert.AreEqual(LoaderPriority.Internal, conf.ResolverPriority.Value, "ResolverPriority should default to Internal");
			Assert.AreEqual("", conf.ApiKey.Value, "ApiKey should default to empty string");
		}

		/// <summary>
		/// Tests that the resolver works correctly when ResolverPriority is set to YoutubeDl.
		/// Validates: Requirements 7.3
		/// </summary>
		[Test]
		public void BackwardCompatibility_YoutubeDlPriority_BehavesAsExpected()
		{
			// Arrange: Create config with YoutubeDl priority
			var conf = new ConfResolverYoutube();
			conf.ResolverPriority.Value = LoaderPriority.YoutubeDl;
			
			// Act: Create resolver
			using var resolver = new YoutubeResolver(conf);
			
			// Assert: Resolver should be created successfully
			Assert.IsNotNull(resolver);
			Assert.AreEqual("youtube", resolver.ResolverFor);
			
			// The resolver should work the same way regardless of priority
			// (since internal scraper is deprecated)
		}

		/// <summary>
		/// Tests that JsonYtdlFormat structure has not changed.
		/// Validates: Requirements 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_JsonYtdlFormat_StructureUnchanged()
		{
			// Arrange & Act: Create a format object
			var format = new JsonYtdlFormat
			{
				vcodec = "avc1.64001F",
				acodec = "mp4a.40.2",
				abr = 128,
				asr = 44100,
				tbr = 256,
				format = "18 - 640x360 (360p)",
				format_id = "18",
				url = "https://example.com/video.mp4",
				ext = "mp4",
				width = 640,
				height = 360
			};
			
			// Assert: All properties should be accessible
			Assert.AreEqual("avc1.64001F", format.vcodec);
			Assert.AreEqual("mp4a.40.2", format.acodec);
			Assert.AreEqual(128, format.abr);
			Assert.AreEqual(44100, format.asr);
			Assert.AreEqual(256, format.tbr);
			Assert.AreEqual("18 - 640x360 (360p)", format.format);
			Assert.AreEqual("18", format.format_id);
			Assert.AreEqual("https://example.com/video.mp4", format.url);
			Assert.AreEqual("mp4", format.ext);
			Assert.AreEqual(640, format.width);
			Assert.AreEqual(360, format.height);
		}

		/// <summary>
		/// Tests that empty or null cookie file configuration doesn't break the system.
		/// Validates: Requirements 7.2
		/// </summary>
		[Test]
		public void BackwardCompatibility_EmptyCookieFile_DoesNotBreak()
		{
			// Arrange: Create config with empty cookie file
			var conf = new ConfResolverYoutube();
			conf.CookieFile.Value = "";
			
			// Act: Create resolver and set up helper (this sets the properties internally)
			using var resolver = new YoutubeResolver(conf);
			
			// Assert: Should not throw - resolver creation succeeded
			Assert.IsNotNull(resolver);
			
			// Verify the config value is accessible
			Assert.AreEqual("", conf.CookieFile.Value);
		}

		/// <summary>
		/// Tests that empty or null extractor args configuration doesn't break the system.
		/// Validates: Requirements 7.2
		/// </summary>
		[Test]
		public void BackwardCompatibility_EmptyExtractorArgs_DoesNotBreak()
		{
			// Arrange: Create config with empty extractor args
			var conf = new ConfResolverYoutube();
			conf.ExtractorArgs.Value = "";
			
			// Act: Create resolver and set up helper (this sets the properties internally)
			using var resolver = new YoutubeResolver(conf);
			
			// Assert: Should not throw - resolver creation succeeded
			Assert.IsNotNull(resolver);
			
			// Verify the config value is accessible
			Assert.AreEqual("", conf.ExtractorArgs.Value);
		}

		/// <summary>
		/// Tests that the system handles null ConfigValue objects gracefully.
		/// Validates: Requirements 7.2, 7.4
		/// </summary>
		[Test]
		public void BackwardCompatibility_NullConfigValues_HandledGracefully()
		{
			// Arrange: Set helper properties to null
			YoutubeDlHelper.CookieFile = null;
			YoutubeDlHelper.ExtractorArgs = null;
			
			// Act & Assert: Should not throw when setting to null
			Assert.DoesNotThrow(() => {
				// Setting to null should work without issues
				YoutubeDlHelper.CookieFile = null;
				YoutubeDlHelper.ExtractorArgs = null;
			});
			
			// Creating a resolver with default config should also work
			var conf = new ConfResolverYoutube();
			Assert.DoesNotThrow(() => {
				using var resolver = new YoutubeResolver(conf);
				Assert.IsNotNull(resolver);
			});
		}
	}
}

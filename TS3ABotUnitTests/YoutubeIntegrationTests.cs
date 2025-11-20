// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TS3AudioBot.ResourceFactories;
using TS3AudioBot.ResourceFactories.Youtube;

namespace TS3ABotUnitTests
{
	/// <summary>
	/// Integration tests for YouTube playback with real videos.
	/// These tests verify that the enhanced format selection works with actual yt-dlp responses.
	/// 
	/// NOTE: These tests require yt-dlp to be installed and accessible.
	/// They may fail if YouTube changes their API or if videos become unavailable.
	/// </summary>
	[TestFixture]
	public class YoutubeIntegrationTests
	{
		// Test video IDs
		private const string WorkingVideoId = "ju6KpFOP_FE"; // Chicoreli - Stressin'
		private const string PreviouslyNonWorkingVideoId = "AqXlIIfi_IU"; // Under The Sun (Original Mix)

		[Test]
		[Category("Integration")]
		[Category("RequiresYtDlp")]
		public async Task TestWorkingVideo_ShouldSelectValidFormat()
		{
			// Arrange
			Console.WriteLine($"Testing working video: {WorkingVideoId}");

			// Act
			var result = await YoutubeDlHelper.GetSingleVideo(WorkingVideoId);

			// Assert
			Assert.IsNotNull(result, "yt-dlp should return video data");
			Assert.IsNotNull(result.formats, "Video should have formats");
			Assert.IsTrue(result.formats.Length > 0, "Video should have at least one format");

			// Test enhanced format selection
			var selectedFormat = YoutubeDlHelper.FilterBestEnhanced(result.formats);

			Assert.IsNotNull(selectedFormat, "FilterBestEnhanced should select a format");
			Assert.IsNotNull(selectedFormat.url, "Selected format should have a URL");
			Assert.AreNotEqual("none", selectedFormat.acodec, "Selected format should have audio");

			// Log the selected format details
			Console.WriteLine($"Selected format: {selectedFormat.format_id}");
			Console.WriteLine($"  Codec: {selectedFormat.acodec}");
			Console.WriteLine($"  Audio-only: {selectedFormat.vcodec == "none"}");
			Console.WriteLine($"  Resolution: {selectedFormat.width}x{selectedFormat.height}");
			Console.WriteLine($"  Bitrate: {selectedFormat.abr?.ToString() ?? "null"}");
			Console.WriteLine($"  URL type: {(YoutubeDlHelper.IsHlsManifest(selectedFormat.url) ? "HLS manifest" : "Direct")}");
		}

		[Test]
		[Category("Integration")]
		[Category("RequiresYtDlp")]
		public async Task TestPreviouslyNonWorkingVideo_ShouldNowWork()
		{
			// Arrange
			Console.WriteLine($"Testing previously non-working video: {PreviouslyNonWorkingVideoId}");

			// Act
			var result = await YoutubeDlHelper.GetSingleVideo(PreviouslyNonWorkingVideoId);

			// Assert
			Assert.IsNotNull(result, "yt-dlp should return video data");
			Assert.IsNotNull(result.formats, "Video should have formats");
			Assert.IsTrue(result.formats.Length > 0, "Video should have at least one format");

			// Test enhanced format selection
			var selectedFormat = YoutubeDlHelper.FilterBestEnhanced(result.formats);

			Assert.IsNotNull(selectedFormat, "FilterBestEnhanced should select a format even with null bitrates");
			Assert.IsNotNull(selectedFormat.url, "Selected format should have a URL");
			Assert.AreNotEqual("none", selectedFormat.acodec, "Selected format should have audio");

			// Log the selected format details
			Console.WriteLine($"Selected format: {selectedFormat.format_id}");
			Console.WriteLine($"  Codec: {selectedFormat.acodec}");
			Console.WriteLine($"  Audio-only: {selectedFormat.vcodec == "none"}");
			Console.WriteLine($"  Resolution: {selectedFormat.width}x{selectedFormat.height}");
			Console.WriteLine($"  Bitrate: {selectedFormat.abr?.ToString() ?? "null"}");
			Console.WriteLine($"  URL type: {(YoutubeDlHelper.IsHlsManifest(selectedFormat.url) ? "HLS manifest" : "Direct")}");
		}

		[Test]
		[Category("Integration")]
		public void TestFilterBestEnhanced_WithNullBitrates_ShouldSelectBasedOnCodec()
		{
			// Arrange - Create formats similar to what modern YouTube returns
			var formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5", // HE-AAC
					vcodec = "avc1.4D400C",
					abr = null, // Null bitrate
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/test1.m3u8"
				},
				new JsonYtdlFormat
				{
					format_id = "93",
					acodec = "mp4a.40.2", // AAC-LC (better)
					vcodec = "avc1.4D401E",
					abr = null, // Null bitrate
					width = 640,
					height = 360,
					url = "https://manifest.googlevideo.com/test2.m3u8"
				},
				new JsonYtdlFormat
				{
					format_id = "95",
					acodec = "mp4a.40.2", // AAC-LC
					vcodec = "avc1.64001F",
					abr = null, // Null bitrate
					width = 1280,
					height = 720,
					url = "https://manifest.googlevideo.com/test3.m3u8"
				}
			};

			// Act
			var selected = YoutubeDlHelper.FilterBestEnhanced(formats);

			// Assert
			Assert.IsNotNull(selected, "Should select a format");
			Assert.AreEqual("93", selected.format_id, "Should select format 93 (AAC-LC with lowest resolution among AAC-LC formats)");
			Assert.IsTrue(selected.acodec.Contains("mp4a.40.2"), "Should prefer AAC-LC codec");
		}

		[Test]
		[Category("Integration")]
		public void TestHlsManifestDetection()
		{
			// Arrange & Act & Assert
			Assert.IsTrue(YoutubeDlHelper.IsHlsManifest("https://manifest.googlevideo.com/api/manifest/hls_playlist/test"),
				"Should detect manifest.googlevideo.com URLs");

			Assert.IsTrue(YoutubeDlHelper.IsHlsManifest("https://example.com/video.m3u8"),
				"Should detect .m3u8 URLs");

			Assert.IsTrue(YoutubeDlHelper.IsHlsManifest("https://example.com/hls_playlist/video"),
				"Should detect hls_playlist in URL");

			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest("https://example.com/video.mp4"),
				"Should not detect regular video URLs");

			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(null),
				"Should handle null URLs");

			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(""),
				"Should handle empty URLs");
		}

		[Test]
		[Category("Integration")]
		public void TestErrorTransformation_Timeout()
		{
			// Arrange
			var errorOutput = "ERROR: Request timed out while fetching video information";

			// Act
			var result = YoutubeDlHelper.TransformYtdlError(errorOutput);

			// Assert
			Assert.IsTrue(result.Contains("timed out"), "Should detect timeout errors");
			Assert.IsTrue(result.Contains("try again"), "Should suggest retrying");
		}

		[Test]
		[Category("Integration")]
		public void TestErrorTransformation_VideoNotFound()
		{
			// Arrange
			var errorOutput = "ERROR: This video is unavailable";

			// Act
			var result = YoutubeDlHelper.TransformYtdlError(errorOutput);

			// Assert
			Assert.IsTrue(result.Contains("not found") || result.Contains("unavailable"), 
				"Should detect video unavailable errors");
		}

		[Test]
		[Category("Integration")]
		public void TestErrorTransformation_AuthenticationRequired()
		{
			// Arrange
			var errorOutput = "ERROR: Sign in to confirm your age";

			// Act
			var result = YoutubeDlHelper.TransformYtdlError(errorOutput);

			// Assert
			Assert.IsTrue(result.Contains("authentication") || result.Contains("cookie"), 
				"Should detect authentication errors and suggest cookie configuration");
		}

		[Test]
		[Category("Integration")]
		public void TestCodecQualityRanking()
		{
			// This test verifies the codec quality ranking by testing format selection
			// with different codec combinations

			// Arrange - AAC-LC vs HE-AAC
			var formatsAacComparison = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "1",
					acodec = "mp4a.40.5", // HE-AAC
					vcodec = "none",
					abr = null,
					url = "https://test.com/1"
				},
				new JsonYtdlFormat
				{
					format_id = "2",
					acodec = "mp4a.40.2", // AAC-LC (better)
					vcodec = "none",
					abr = null,
					url = "https://test.com/2"
				}
			};

			// Act
			var selectedAac = YoutubeDlHelper.FilterBestEnhanced(formatsAacComparison);

			// Assert
			Assert.AreEqual("2", selectedAac.format_id, "Should prefer AAC-LC over HE-AAC");

			// Arrange - Opus vs AAC-LC (both high quality)
			var formatsOpusComparison = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "3",
					acodec = "opus",
					vcodec = "none",
					abr = null,
					url = "https://test.com/3"
				},
				new JsonYtdlFormat
				{
					format_id = "4",
					acodec = "mp4a.40.2", // AAC-LC
					vcodec = "none",
					abr = null,
					url = "https://test.com/4"
				}
			};

			// Act
			var selectedOpus = YoutubeDlHelper.FilterBestEnhanced(formatsOpusComparison);

			// Assert
			// Both Opus and AAC-LC have quality 3, so either could be selected
			// The important thing is that a format is selected
			Assert.IsNotNull(selectedOpus, "Should select a format when both codecs are high quality");
		}

		[Test]
		[Category("Integration")]
		public void TestAudioOnlyPreference()
		{
			// Arrange - Audio-only vs combined stream with same codec
			var formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "audio",
					acodec = "mp4a.40.2",
					vcodec = "none", // Audio-only
					abr = null,
					url = "https://test.com/audio"
				},
				new JsonYtdlFormat
				{
					format_id = "combined",
					acodec = "mp4a.40.2",
					vcodec = "avc1.64001F", // Has video
					abr = null,
					width = 1280,
					height = 720,
					url = "https://test.com/combined"
				}
			};

			// Act
			var selected = YoutubeDlHelper.FilterBestEnhanced(formats);

			// Assert
			Assert.AreEqual("audio", selected.format_id, "Should prefer audio-only format over combined when codec is the same");
		}

		[Test]
		[Category("Integration")]
		public void TestResolutionPreference_ForCombinedStreams()
		{
			// Arrange - Multiple combined streams with same codec
			var formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "720p",
					acodec = "mp4a.40.2",
					vcodec = "avc1.64001F",
					abr = null,
					width = 1280,
					height = 720,
					url = "https://test.com/720p"
				},
				new JsonYtdlFormat
				{
					format_id = "360p",
					acodec = "mp4a.40.2",
					vcodec = "avc1.4D401E",
					abr = null,
					width = 640,
					height = 360,
					url = "https://test.com/360p"
				},
				new JsonYtdlFormat
				{
					format_id = "1080p",
					acodec = "mp4a.40.2",
					vcodec = "avc1.640028",
					abr = null,
					width = 1920,
					height = 1080,
					url = "https://test.com/1080p"
				}
			};

			// Act
			var selected = YoutubeDlHelper.FilterBestEnhanced(formats);

			// Assert
			Assert.AreEqual("360p", selected.format_id, 
				"Should prefer lowest resolution combined stream to minimize bandwidth for audio playback");
		}

		/// <summary>
		/// Tests that the same format selection logic is applied consistently across multiple videos.
		/// Validates Requirements 7.1, 7.3
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentFormatSelection_AcrossMultipleVideos()
		{
			// Arrange - Create format sets that simulate different videos
			// All have the same structure but different URLs to simulate different videos
			var video1Formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "140",
					acodec = "mp4a.40.2",
					vcodec = "none",
					abr = 128,
					url = "https://direct.example.com/video1.m4a"
				},
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5",
					vcodec = "avc1.4D400C",
					abr = null,
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/video1.m3u8"
				}
			};

			var video2Formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "140",
					acodec = "mp4a.40.2",
					vcodec = "none",
					abr = 128,
					url = "https://direct.example.com/video2.m4a"
				},
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5",
					vcodec = "avc1.4D400C",
					abr = null,
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/video2.m3u8"
				}
			};

			var video3Formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "140",
					acodec = "mp4a.40.2",
					vcodec = "none",
					abr = 128,
					url = "https://direct.example.com/video3.m4a"
				},
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5",
					vcodec = "avc1.4D400C",
					abr = null,
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/video3.m3u8"
				}
			};

			// Act - Apply format selection to all videos
			var selected1 = YoutubeDlHelper.FilterBestEnhanced(video1Formats);
			var selected2 = YoutubeDlHelper.FilterBestEnhanced(video2Formats);
			var selected3 = YoutubeDlHelper.FilterBestEnhanced(video3Formats);

			// Assert - All videos should select the same format_id (consistent logic)
			Assert.IsNotNull(selected1, "Video 1 should have a selected format");
			Assert.IsNotNull(selected2, "Video 2 should have a selected format");
			Assert.IsNotNull(selected3, "Video 3 should have a selected format");

			Assert.AreEqual(selected1.format_id, selected2.format_id, 
				"Videos 1 and 2 should select the same format_id");
			Assert.AreEqual(selected2.format_id, selected3.format_id, 
				"Videos 2 and 3 should select the same format_id");

			// All should prefer direct URL over HLS
			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(selected1.url), 
				"Video 1 should select direct URL");
			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(selected2.url), 
				"Video 2 should select direct URL");
			Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(selected3.url), 
				"Video 3 should select direct URL");

			Console.WriteLine($"Consistent format selection verified: All videos selected format {selected1.format_id}");
		}

		/// <summary>
		/// Tests that direct URLs are consistently prioritized over HLS manifests across multiple videos.
		/// Validates Requirements 6.3, 6.4, 7.3
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentDirectUrlPrioritization_AcrossMultipleVideos()
		{
			// Arrange - Create multiple format sets with both direct and HLS options
			var testCases = new[]
			{
				new
				{
					Name = "Video A",
					Formats = new[]
					{
						new JsonYtdlFormat
						{
							format_id = "direct",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://direct.example.com/videoA.m4a"
						},
						new JsonYtdlFormat
						{
							format_id = "hls",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://manifest.googlevideo.com/videoA.m3u8"
						}
					}
				},
				new
				{
					Name = "Video B",
					Formats = new[]
					{
						new JsonYtdlFormat
						{
							format_id = "direct",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://direct.example.com/videoB.m4a"
						},
						new JsonYtdlFormat
						{
							format_id = "hls",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://manifest.googlevideo.com/videoB.m3u8"
						}
					}
				},
				new
				{
					Name = "Video C",
					Formats = new[]
					{
						new JsonYtdlFormat
						{
							format_id = "direct",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://direct.example.com/videoC.m4a"
						},
						new JsonYtdlFormat
						{
							format_id = "hls",
							acodec = "mp4a.40.2",
							vcodec = "none",
							abr = 128,
							url = "https://manifest.googlevideo.com/videoC.m3u8"
						}
					}
				}
			};

			// Act & Assert - All videos should consistently prefer direct URLs
			foreach (var testCase in testCases)
			{
				var selected = YoutubeDlHelper.FilterBestEnhanced(testCase.Formats);

				Assert.IsNotNull(selected, $"{testCase.Name} should have a selected format");
				Assert.AreEqual("direct", selected.format_id, 
					$"{testCase.Name} should select direct URL format");
				Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(selected.url), 
					$"{testCase.Name} should not select HLS manifest");

				Console.WriteLine($"{testCase.Name}: Correctly prioritized direct URL");
			}
		}

		/// <summary>
		/// Tests that HLS URL detection is consistent across different URL patterns.
		/// Validates Requirements 3.1, 6.1, 7.1
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentHlsDetection_AcrossUrlPatterns()
		{
			// Arrange - Various HLS URL patterns
			var hlsUrls = new[]
			{
				"https://manifest.googlevideo.com/api/manifest/hls_playlist/video1",
				"https://manifest.googlevideo.com/api/manifest/hls_variant/video2",
				"https://example.com/stream.m3u8",
				"https://example.com/playlist.m3u8?token=abc",
				"https://cdn.example.com/hls_playlist/stream",
				"https://cdn.example.com/path/to/hls_playlist/video"
			};

			var nonHlsUrls = new[]
			{
				"https://direct.example.com/video.m4a",
				"https://direct.example.com/audio.mp3",
				"https://cdn.example.com/video.mp4",
				"https://example.com/stream?format=mp4"
			};

			// Act & Assert - All HLS URLs should be detected consistently
			foreach (var url in hlsUrls)
			{
				Assert.IsTrue(YoutubeDlHelper.IsHlsManifest(url), 
					$"Should consistently detect HLS URL: {url}");
			}

			// All non-HLS URLs should be rejected consistently
			foreach (var url in nonHlsUrls)
			{
				Assert.IsFalse(YoutubeDlHelper.IsHlsManifest(url), 
					$"Should consistently reject non-HLS URL: {url}");
			}

			Console.WriteLine($"HLS detection consistent across {hlsUrls.Length} HLS URLs and {nonHlsUrls.Length} non-HLS URLs");
		}

		/// <summary>
		/// Tests that codec quality ranking is applied consistently across different format combinations.
		/// Validates Requirements 7.1, 7.3
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentCodecRanking_AcrossFormatCombinations()
		{
			// Arrange - Multiple test cases with different codec combinations
			var testCases = new[]
			{
				new
				{
					Name = "AAC-LC vs HE-AAC (Set 1)",
					Formats = new[]
					{
						new JsonYtdlFormat { format_id = "he-aac", acodec = "mp4a.40.5", vcodec = "none", url = "https://test.com/1" },
						new JsonYtdlFormat { format_id = "aac-lc", acodec = "mp4a.40.2", vcodec = "none", url = "https://test.com/2" }
					},
					ExpectedCodec = "mp4a.40.2"
				},
				new
				{
					Name = "AAC-LC vs HE-AAC (Set 2)",
					Formats = new[]
					{
						new JsonYtdlFormat { format_id = "aac-lc", acodec = "mp4a.40.2", vcodec = "none", url = "https://test.com/3" },
						new JsonYtdlFormat { format_id = "he-aac", acodec = "mp4a.40.5", vcodec = "none", url = "https://test.com/4" }
					},
					ExpectedCodec = "mp4a.40.2"
				},
				new
				{
					Name = "Opus vs HE-AAC",
					Formats = new[]
					{
						new JsonYtdlFormat { format_id = "he-aac", acodec = "mp4a.40.5", vcodec = "none", url = "https://test.com/5" },
						new JsonYtdlFormat { format_id = "opus", acodec = "opus", vcodec = "none", url = "https://test.com/6" }
					},
					ExpectedCodec = "opus"
				}
			};

			// Act & Assert - Codec ranking should be consistent
			foreach (var testCase in testCases)
			{
				var selected = YoutubeDlHelper.FilterBestEnhanced(testCase.Formats);

				Assert.IsNotNull(selected, $"{testCase.Name} should select a format");
				Assert.IsTrue(selected.acodec.Contains(testCase.ExpectedCodec), 
					$"{testCase.Name} should consistently prefer {testCase.ExpectedCodec} codec. Got: {selected.acodec}");

				Console.WriteLine($"{testCase.Name}: Correctly selected {selected.acodec}");
			}
		}

		/// <summary>
		/// Tests that error transformation is consistent across similar error patterns.
		/// Validates Requirements 7.4
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentErrorTransformation_AcrossSimilarErrors()
		{
			// Arrange - Similar error patterns that should be handled consistently
			var timeoutErrors = new[]
			{
				"ERROR: Request timed out",
				"ERROR: Connection timed out while fetching video",
				"ERROR: timeout occurred during download"
			};

			var unavailableErrors = new[]
			{
				"ERROR: Video unavailable",
				"ERROR: This video is not available",
				"ERROR: Video has been removed"
			};

			var authErrors = new[]
			{
				"ERROR: Sign in to confirm your age",
				"ERROR: This video requires login",
				"ERROR: Members-only content"
			};

			// Act & Assert - Timeout errors should be handled consistently
			foreach (var error in timeoutErrors)
			{
				var result = YoutubeDlHelper.TransformYtdlError(error);
				Assert.IsTrue(result.ToLowerInvariant().Contains("timeout") || 
				              result.ToLowerInvariant().Contains("timed out"), 
					$"Should consistently handle timeout error: {error}");
			}

			// Unavailable errors should be handled consistently
			foreach (var error in unavailableErrors)
			{
				var result = YoutubeDlHelper.TransformYtdlError(error);
				Assert.IsTrue(result.ToLowerInvariant().Contains("not found") || 
				              result.ToLowerInvariant().Contains("unavailable") ||
				              result.ToLowerInvariant().Contains("removed"), 
					$"Should consistently handle unavailable error: {error}");
			}

			// Auth errors should be handled consistently
			foreach (var error in authErrors)
			{
				var result = YoutubeDlHelper.TransformYtdlError(error);
				Assert.IsTrue(result.ToLowerInvariant().Contains("authentication") || 
				              result.ToLowerInvariant().Contains("cookie") ||
				              result.ToLowerInvariant().Contains("login") ||
				              result.ToLowerInvariant().Contains("sign in"), 
					$"Should consistently handle auth error: {error}");
			}

			Console.WriteLine($"Error transformation consistent across {timeoutErrors.Length + unavailableErrors.Length + authErrors.Length} error patterns");
		}

		/// <summary>
		/// Tests that format selection with null bitrates is handled consistently.
		/// This simulates the real-world scenario where modern YouTube formats often have null bitrates.
		/// Validates Requirements 7.1, 7.3
		/// </summary>
		[Test]
		[Category("Integration")]
		[Category("Consistency")]
		public void TestConsistentNullBitrateHandling_AcrossMultipleVideos()
		{
			// Arrange - Multiple videos with null bitrates (common in modern YouTube)
			var video1Formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5",
					vcodec = "avc1.4D400C",
					abr = null,
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/video1.m3u8"
				},
				new JsonYtdlFormat
				{
					format_id = "93",
					acodec = "mp4a.40.2",
					vcodec = "avc1.4D401E",
					abr = null,
					width = 640,
					height = 360,
					url = "https://manifest.googlevideo.com/video1_hd.m3u8"
				}
			};

			var video2Formats = new[]
			{
				new JsonYtdlFormat
				{
					format_id = "91",
					acodec = "mp4a.40.5",
					vcodec = "avc1.4D400C",
					abr = null,
					width = 256,
					height = 144,
					url = "https://manifest.googlevideo.com/video2.m3u8"
				},
				new JsonYtdlFormat
				{
					format_id = "93",
					acodec = "mp4a.40.2",
					vcodec = "avc1.4D401E",
					abr = null,
					width = 640,
					height = 360,
					url = "https://manifest.googlevideo.com/video2_hd.m3u8"
				}
			};

			// Act
			var selected1 = YoutubeDlHelper.FilterBestEnhanced(video1Formats);
			var selected2 = YoutubeDlHelper.FilterBestEnhanced(video2Formats);

			// Assert - Both should select the same format_id consistently
			Assert.IsNotNull(selected1, "Video 1 should select a format despite null bitrates");
			Assert.IsNotNull(selected2, "Video 2 should select a format despite null bitrates");

			Assert.AreEqual(selected1.format_id, selected2.format_id, 
				"Both videos should select the same format_id when handling null bitrates");

			// Both should prefer AAC-LC (format 93) over HE-AAC (format 91)
			Assert.AreEqual("93", selected1.format_id, 
				"Should consistently select AAC-LC format when bitrates are null");
			Assert.AreEqual("93", selected2.format_id, 
				"Should consistently select AAC-LC format when bitrates are null");

			Console.WriteLine($"Null bitrate handling consistent: Both videos selected format {selected1.format_id}");
		}
	}
}

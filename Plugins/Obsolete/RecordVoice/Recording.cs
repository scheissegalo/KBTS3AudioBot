using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib.Full;
using TS3AudioBot.Audio;
using TSLib.Audio;  // Adjust based on the actual location of IPipe
using NAudio.Wave;

namespace DieterBot
{
	public class Dieter : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Player player;
		//private OpusEncoder encoder;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private bool looping = true;
		private readonly DecoderPipe decoderPipe;
		//private readonly FilePipe filePipe;

		public Dieter(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, PlayManager playManager, Player player)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
			this.playManager = playManager;
			this.player = player;

			this.decoderPipe = new DecoderPipe();
			//filePipe = new FilePipe("testing.wav");
		}

		public void Initialize()
		{
			Log.Info("DieterBot plugin started.");
			string folderPath = Path.GetFullPath("dieter");

			// Check if the folder exists
			if (!Directory.Exists(folderPath))
			{
				Log.Error($"Folder not found: {folderPath}");
				return;
			}
			// Get all MP3 files in the folder
			string[] mp3Files = Directory.GetFiles(folderPath, "*.mp3");

			// Check if there are any MP3 files
			if (mp3Files.Length == 0)
			{
				Console.WriteLine("No MP3 files found in the folder.");
				return;
			}
			//var filePipe = new FilePipe();
			tsFullClient.Chain<AudioPacketReader>()
				.Chain<DecoderPipe>()
				.Chain<FilePipe>();

			Log.Info($"Chains loadet");
			//Start Dieter Loop
			//StartLoop(mp3Files);
		}

		public async Task DoTheDieter(string[] mp3Files)
		{
			if (playManager.IsPlaying)
			{
				Log.Info("Unable to dieter, song is playing");
			}
			else
			{
				// Pick a random MP3 file
				Random random = new Random();
				string randomMp3 = mp3Files[random.Next(mp3Files.Length)];

				float saveTempVolume = player.Volume;
				player.Volume = 100;
				var ownClient = await tsFullClient.GetClientUidFromClientId(serverView.OwnClient);
				InvokerData id = new InvokerData(ownClient.Value.ClientUid);
				//var fullPath = Path.GetFullPath("dieta.wav");
				
				await playManager.Play(id, randomMp3);
				player.Volume = saveTempVolume;
			}
		}

		private async void StartLoop(string[] mp3Files)
		{
			while (looping)
			{
				await DoTheDieter(mp3Files);
				await Task.Delay(10000);
			}
		}


		public void Dispose()
		{
			looping = false;
			//StopRecording();
			Log.Info("DieterBot plugin disposed.");

		}

	}

	public class FilePipe : IAudioPipe, IAudioPassiveConsumer, IAudioActiveProducer, IDisposable
	{
		private WaveFileWriter? waveFileWriter;
		private readonly int sampleRate = 48000; // Your decoder's sample rate
		private readonly int channels = 2; // Stereo
		private bool headerWritten = false;
		string directoryPath = "dieter";
		string filePath = "test.wav";
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private readonly System.Timers.Timer silenceTimer;
		private readonly TimeSpan silenceThreshold = TimeSpan.FromMilliseconds(300); // 0.3 seconds
		private List<byte> audioBuffer = new List<byte>(); // Temporary buffer for audio data

		public bool Active => waveFileWriter != null;
		public IAudioPassiveConsumer? OutStream { get; set; }

		public FilePipe()
		{
			//Log.Info($"File Pipe Called");			
			// Create the wave file writer with a memory stream or file stream
			//var waveFormat = new WaveFormat(sampleRate, 16, channels); // Assuming 16-bit samples
			//waveFileWriter = new WaveFileWriter(filePath, waveFormat);			

			// Set up the timer
			silenceTimer = new System.Timers.Timer(silenceThreshold.TotalMilliseconds);
			silenceTimer.AutoReset = false; // One-shot timer
			silenceTimer.Elapsed += OnSilenceThresholdReached;
			Log.Info("File Pipe Initialized");
			filePath = System.IO.Path.Combine(directoryPath, "test.wav");
			try
			{
				System.IO.Directory.CreateDirectory(directoryPath);
				Log.Info($"Ensured directory exists: {directoryPath}");
			}
			catch (Exception ex)
			{
				Log.Error($"Error ensuring directory exists: {ex.Message}");
				return;
			}
		}

		private void OnSilenceThresholdReached(object? sender, System.Timers.ElapsedEventArgs e)
		{
			Log.Info("Silence threshold reached. Writing buffer to file.");
			WriteBufferToFile();
		}

		//private void StartNewFile()
		//{
		//	// Dispose the previous writer if it exists
		//	waveFileWriter?.Dispose();

		//	// Create a new wave file writer
		//	var waveFormat = new WaveFormat(sampleRate, 16, channels); // Assuming 16-bit samples
		//	waveFileWriter = new WaveFileWriter(filePath, waveFormat);
		//	Log.Info("Started a new audio file.");
		//}

		public void Write(Span<byte> data, Meta? meta)
		{
			// Reset and restart the silence timer on data reception
			silenceTimer.Stop();
			silenceTimer.Start();

			// Append the incoming audio data to the buffer
			audioBuffer.AddRange(data.ToArray());
			Log.Info($"Received data. Buffer size: {audioBuffer.Count}");

			//Log.Info($"Write Called");
			//if (data.Length == 0)
			//{
			//	// If no data is received, start the silence timer if not already running
			//	if (!silenceTimer.IsRunning)
			//	{
			//		Log.Info($"Starting Timer");
			//		silenceTimer.Start();
			//	}


			//	// Check if silence threshold is exceeded
			//	if (silenceTimer.Elapsed >= silenceThreshold)
			//	{
			//		Log.Info("Silence detected. Writing audio buffer to file.");
			//		WriteBufferToFile();
			//		silenceTimer.Reset(); // Reset the timer
			//	}
			//	else
			//	{
			//		Log.Info($"Waiting for timer {silenceTimer.Elapsed}");
			//	}
			//	return;
			//}
			//else
			//{
			//	Log.Info("Not recieving data");
			//}

			// Reset the silence timer since data is received
			//silenceTimer.Reset();

			//// Append incoming data to the buffer
			//audioBuffer.AddRange(data.ToArray());
			//Log.Info($"Adding Range Count: {audioBuffer.Count}");
			//Log.Info($"Writing {data.Length} bytes of audio data to the file.");
			//if (!headerWritten)
			//{
			//	// Write a WAV header (this is automatically handled by WaveFileWriter)
			//	headerWritten = true;
			//}
			//// Log the size of the data being written to the file
			//Log.Info($"Writing {data.Length} bytes to the file.");

			//if (data.Length == 0)
			//{
			//	Log.Error("Received zero-length audio data.");
			//}
			//StartNewFile();
			//// Assuming 'data' is already decoded PCM data (16-bit stereo at sampleRate)
			//byte[] pcmData = data.ToArray();
			//if (waveFileWriter != null)
			//{
			//	return;
			//}
			//waveFileWriter?.Write(pcmData, 0, pcmData.Length);
			//waveFileWriter?.Flush();
		}

		private void WriteBufferToFile()
		{
			if (audioBuffer.Count == 0)
			{
				Log.Warn("No audio data in buffer to write.");
				return;
			}

			// Dispose the current writer if it exists
			waveFileWriter?.Dispose();
			waveFileWriter = null;

			// Delete the file if it already exists
			//if (System.IO.File.Exists(filePath))
			//{
			//	try
			//	{
			//		System.IO.File.Delete(filePath);
			//		Log.Info($"Deleted existing file: {filePath}");
			//	}
			//	catch (Exception ex)
			//	{
			//		Log.Error($"Error deleting file: {ex.Message}");
			//		return;
			//	}
			//}

			if (System.IO.File.Exists(filePath))
			{
				try
				{
					System.IO.File.Delete(filePath);
					Log.Info($"Deleted existing file: {filePath}");
				}
				catch (Exception ex)
				{
					Log.Error($"Error deleting file: {ex.Message}");
					return;
				}
			}


			// Initialize a new WaveFileWriter
			try
			{
				var waveFormat = new WaveFormat(48000, 16, 2); // Example: 48kHz, 16-bit, stereo
				waveFileWriter = new WaveFileWriter(filePath, waveFormat);

				// Write the buffered data to the new file
				byte[] bufferArray = audioBuffer.ToArray();
				waveFileWriter.Write(bufferArray, 0, bufferArray.Length);
				waveFileWriter.Flush();
				audioBuffer.Clear(); // Clear the buffer after writing

				Log.Info($"Wrote {bufferArray.Length} bytes to the file: {filePath}");
			}
			catch (Exception ex)
			{
				Log.Error($"Error creating or writing to file: {ex.Message}");
			}
		}


		public void Dispose()
		{
			silenceTimer.Dispose();// Ensure any remaining data is written
			waveFileWriter?.Dispose();
			Log.Info("File Pipe Disposed.");
		}
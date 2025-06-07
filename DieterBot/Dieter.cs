using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib.Full;
using TS3AudioBot.Audio;
using TSLib.Audio;
using NAudio.Wave;
using System.Diagnostics;
using System;
using Org.BouncyCastle.Crypto.Generators;
using TSLib;
using TSLib.Scheduler;
using System.Threading.Tasks;
using TS3AudioBot.CommandSystem;
using System.Numerics;
using System.Text.RegularExpressions;
using TSLib.Helper;

namespace DieterBot
{

	public class Dieter : IBotPlugin
	{
		//public static SynchronizationContext TsFullClientSyncContext { get; private set; }

		public static TsFullClient tsStatic;
		public static PlayManager PlayManagerStatic;
		public static Connection ServerViewStatic;
		public static Player PlayerStatic;
		public static bool RecordingMode = false;
		public static bool PredictionMode = false;

		private PlayManager playManager;
		public TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		public static DedicatedTaskScheduler taskSchedule;
		private Player player;

		//public Dieter _instance;

		//private TSHolder tsHolder = new TSHolder();
		private readonly DecoderPipe decoderPipe;
		//private OpusEncoder encoder;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private bool looping = true;

		public Dieter(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, PlayManager playManager, Player player, DedicatedTaskScheduler ts)
		{
			//this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			tsStatic = tsFull;
			this.serverView = serverView;
			ServerViewStatic = serverView;
			this.playManager = playManager;
			PlayManagerStatic = playManager;
			this.player = player;
			PlayerStatic = player;
			taskSchedule = ts;

			//TSHolder.Initialize(tsFullClient);
			// Capture the synchronization context
			//TsFullClientSyncContext = SynchronizationContext.Current;

			this.decoderPipe = new DecoderPipe();

			//if (_instance == null)
			//{
			//	_instance = this;
			//}

			//var filePipe = AudioPipeFactory.CreateFilePipe(tsFullClient);

			// Create the FilePipe instance
			//var filePipe = new FilePipe();

			// Inject the TsFullClient into FilePipe after it's created

			tsFullClient.Chain<AudioPacketReader>()
					.Chain<DecoderPipe>()
					//.Chain<StaticMetaPipe>()
					//.Chain<filePipe>();
					.Chain<FilePipe>();

			//filePipe.tsFullClient = this.tsFullClient;
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

			//var filePipe = AudioPipeFactory.CreateFilePipe(this);

			//tsFullClient.Chain<AudioPacketReader>()
			//.Chain<DecoderPipe>()
			////.Chain<filePipe>();
			//.Chain<FilePipe>();

			//tsFullClient.OutStream = 

			Log.Info($"Chains loadet");

			//Start Dieter Loop
			StartLoop(mp3Files);

			//var client = new TsFullClient(TSLib.Scheduler.DedicatedTaskScheduler);
		}

		[Command("record")]
		public static async Task<string> CommandRecord(ClientCall invoker)
		{
			if (RecordingMode)
			{
				RecordingMode = false;
				return "Recording Stopped!";
			}
			else
			{
				RecordingMode = true;
				return "Recording";
			}
			
		}

		[Command("predict")]
		public static async Task<string> CommandPredict(ClientCall invoker)
		{
			if (PredictionMode)
			{
				PredictionMode = false;
				return "Recording Stopped!";
			}
			else
			{
				PredictionMode = true;
				return "Recording";
			}

		}

		public void HandleCorrectPrediction(string audioFilePath)
		{
			Console.WriteLine($"Correct word detected for file: {audioFilePath}");
			//ExecuteSomeLogic();
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
			Random random = new Random();

			while (looping)
			{
				await DoTheDieter(mp3Files);
				int randomDelay = random.Next(3600000, 18000001); // Random delay in milliseconds
				await Task.Delay(randomDelay);
			}
		}


		public void Dispose()
		{
			looping = false;
			//StopRecording();
			Log.Info("DieterBot plugin disposed.");

		}
	}

	public class TSWoker
	{
		//public void SendBotToAsk()
		//{
		//	var client = new TsFullClient(EventDispatchType.AutoThreadPooled);
		//	var data = Ts3Crypt.LoadIdentity("MCkDAgbAAgEgAiBPKKMIrHtAH/FBKchbm4iRWZybdRTk/ZiehtH0gQRg+A==", 64, 0).Unwrap();
		//	con = new ConnectionDataFull() { Address = "pow.splamy.de", Username = "TestClient", Identity = data };

		//	// Setup audio
		//	new StreamAudioProducer(File.OpenRead("bass.mp3"))
		//			// Reads from a passive output buffer with a fixed timing
		//			.Into<PreciseTimedPipe>(x => x.Initialize(new SampleInfo(48_000, 2, 16)))
		//			// Encode to the codec of our choice (codec should match the timed pipe)
		//			.Chain(new EncoderPipe(Codec.OpusMusic))
		//			// Define where to send to.
		//			.Chain<StaticMetaPipe>(x => x.SetVoice())
		//			// Send it with our client.
		//			.Chain(client);

		//	// Connect
		//	client.Connect(con);
		//}
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
		private int fileNumber = 1;
		private bool isPredicting = false;
		private bool listeningMode = false;

		private static FilePipe _instance;

		private readonly System.Timers.Timer silenceTimer;
		private readonly TimeSpan silenceThreshold = TimeSpan.FromMilliseconds(300); // 0.3 seconds
		private List<byte> audioBuffer = new List<byte>(); // Temporary buffer for audio data

		public bool Active => waveFileWriter != null;
		public IAudioPassiveConsumer? OutStream { get; set; }

		// Define a list of supported commands
		private static readonly Dictionary<string, Action> CommandActions = new Dictionary<string, Action>
		{
			{ "music", PlayMusic },
			{ "youtube", OpenYouTube },
			{ "stop", StopMusic },
			{ "play", PlayMusic }
		};


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
			filePath = System.IO.Path.Combine(directoryPath, $"rec_{fileNumber}.wav");
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

			_instance = this;
		}

		public static void ProcessCommand(string commandText)
		{
			// Convert command text to lowercase to handle case-insensitivity
			commandText = commandText.ToLower();

			// Check if the command exists in the dictionary
			foreach (var command in CommandActions)
			{
				if (commandText.Contains(command.Key)) // Check if command is in the text
				{
					// Execute the corresponding action
					command.Value.Invoke();
					return; // Exit once a command is matched and action is executed
				}
			}

			Console.WriteLine("No valid command found.");
		}

		// Example actions for commands
		private async static void PlayMusic()
		{
			Console.WriteLine("Playing music...");
			var myClinet = Dieter.ServerViewStatic.OwnClient;
			var ownClient = await Dieter.tsStatic.GetClientUidFromClientId(myClinet);
			//InvokerData id = new InvokerData(ownClient.Value.ClientUid);
			InvokerData id = new InvokerData(ownClient.Value.ClientUid);
			//var fullPath = Path.GetFullPath("dieta.wav");

			await Dieter.PlayManagerStatic.Play(id, "https://youtu.be/wx-_ObZVqgE");
			// Add logic to start playing music
		}

		private static void OpenYouTube()
		{
			Console.WriteLine("Opening YouTube...");
			// Add logic to open YouTube (e.g., launching a browser with a link)
		}

		private async static void StopMusic()
		{
			Console.WriteLine("Stopping music...");
			await Dieter.PlayManagerStatic.Stop();
			_instance.listeningMode = false;
			_instance.SendMp3("jetztReichts.mp3");
			// Add logic to stop the music playback
		}

		private void OnSilenceThresholdReached(object? sender, System.Timers.ElapsedEventArgs e)
		{
			Log.Info("Silence threshold reached. Writing buffer to file.");
			WriteBufferToFile();
		}

		public void Write(Span<byte> data, Meta? meta)
		{
			// Reset and restart the silence timer on data reception
			silenceTimer.Stop();
			silenceTimer.Start();

			// Append the incoming audio data to the buffer
			audioBuffer.AddRange(data.ToArray());
			Log.Info($"Received data. Buffer size: {audioBuffer.Count}");

			//Log.Info($"Write Called");

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


			filePath = System.IO.Path.Combine(directoryPath, $"rec_{fileNumber}.wav");
			string oldfilePath = System.IO.Path.Combine(directoryPath, $"rec_{fileNumber-1}.wav");

			if (!Dieter.RecordingMode)
			{
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

				if (System.IO.File.Exists(oldfilePath))
				{
					try
					{
						System.IO.File.Delete(oldfilePath);
						Log.Info($"Deleted old existing file: {oldfilePath}");
					}
					catch (Exception ex)
					{
						Log.Error($"Error deleting file: {ex.Message}");
						return;
					}
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

				fileNumber++;

				if (listeningMode)
				{
					// Listen Mode
					STT();
				}
				else
				{
					//Predict Keyword
					if (Dieter.PredictionMode)
					{
						PredictKeyword();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Error creating or writing to file: {ex.Message}");
			}
		}

		private async Task SendMp3(string mp3)
		{
			if (Dieter.taskSchedule != null && Dieter.tsStatic != null)
			{
				await Dieter.taskSchedule.Invoke(async () =>
				{
					try
					{
						if (Dieter.PlayManagerStatic.IsPlaying)
						{
							Log.Info("Unable to dieter, song is playing");
						}
						else
						{
							// Pick a random MP3 file
							//Random random = new Random();
							//string randomMp3 = "ichbindieter.mp3";
							string folderPath = Path.GetFullPath("dieter");
							string fullPath = Path.Combine(folderPath, mp3);

							float saveTempVolume = Dieter.PlayerStatic.Volume;
							Dieter.PlayerStatic.Volume = 100;
							var myClinet = Dieter.ServerViewStatic.OwnClient;
							var ownClient = await Dieter.tsStatic.GetClientUidFromClientId(myClinet);
							//InvokerData id = new InvokerData(ownClient.Value.ClientUid);
							InvokerData id = new InvokerData(ownClient.Value.ClientUid);
							//var fullPath = Path.GetFullPath("dieta.wav");

							await Dieter.PlayManagerStatic.Play(id, fullPath);
							Dieter.PlayerStatic.Volume = saveTempVolume;
						}

					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error sending MP3: {ex.Message}");
					}
				});
			}
			else
			{
				Console.WriteLine("Task scheduler or TsFullClient is not initialized.");
			}

		}

		private void STT()
		{
			// Path to your Python interpreter
			string pythonPath = @"C:\Users\user\.conda\envs\librosa\python.exe"; // Replace with your actual path on Windows
																				 // Path to the Python script
			string scriptPath = @"F:\clones\KBTS3AudioBot\TS3AudioBot\bin\Debug\net6.0\dieter\stt.py"; // Replace with the actual path to your Python script

			// Argument: audio file path
			string audioFilePath = @$"{Path.GetFullPath(filePath)}"; // Replace with the actual path to the audio file

			string scriptDirectory = @"F:\clones\KBTS3AudioBot\TS3AudioBot\bin\Debug\net6.0\dieter";

			// Set up the process start information
			ProcessStartInfo start = new ProcessStartInfo
			{
				FileName = pythonPath,
				Arguments = $"\"{scriptPath}\" \"{audioFilePath}\"",
				RedirectStandardOutput = true,  // Capture the output
				RedirectStandardError = true,  // Capture errors
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = scriptDirectory
			};

			using (Process process = Process.Start(start))
			{
				// Read the output
				string output = process.StandardOutput.ReadToEnd();
				//string error = process.StandardError.ReadToEnd();
				//string result = process.StandardOutput.ReadToEnd().Trim();

				process.WaitForExit();

				//Console.WriteLine("Raw Output: " + output);
				//Console.WriteLine("Raw Error: " + error);

				// Clean the output by extracting the prediction result
				string[] outputLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string prediction = outputLines[^1]; // Get the last line (assumes prediction is the last output)

				//Console.WriteLine("Prediction Result: " + prediction);

				// Check the prediction result
				// Check if the prediction is correct or incorrect
				if (Dieter.taskSchedule != null && Dieter.tsStatic != null)
				{
					Dieter.taskSchedule.Invoke(() =>
					{
						try
						{
							listeningMode = true;
							Dieter.tsStatic.SendChannelMessage($"You said: " + RemoveTimestamps(prediction));
							// Process the cleaned text for commands
							ProcessCommand(prediction);
							//SendMp3("ichbindieter.mp3");


						}
						catch (Exception ex)
						{
							Console.WriteLine($"Error sending message: {ex.Message}");
							//SendMp3();
						}
					});
				}
				else
				{
					Console.WriteLine("Task scheduler or TsFullClient is not initialized.");
				}

				//if (prediction == "1")
				//{
				//}
			}
		}

		// Regex method to remove timestamps (e.g., [0.00s - 2.00s])
		private static string RemoveTimestamps(string text)
		{
			// This regex matches time stamps in the format [0.00s - 2.00s]
			string pattern = @"\[\d+\.\d+s - \d+\.\d+s\]\s*";
			return Regex.Replace(text, pattern, string.Empty).Trim();
		}


		private void PredictKeyword()
		{
			if (isPredicting)
			{
				Console.WriteLine("Already predicting!");
				return;
			}
			isPredicting = true;
			// Path to your Python interpreter
			string pythonPath = @"C:\Users\user\.conda\envs\librosa\python.exe"; // Replace with your actual path on Windows
																				 // Path to the Python script
			string scriptPath = @"F:\clones\KBTS3AudioBot\TS3AudioBot\bin\Debug\net6.0\dieter\4predict.py"; // Replace with the actual path to your Python script

			// Argument: audio file path
			string audioFilePath = @$"{Path.GetFullPath(filePath)}"; // Replace with the actual path to the audio file

			string scriptDirectory = @"F:\clones\KBTS3AudioBot\TS3AudioBot\bin\Debug\net6.0\dieter";

			// Set up the process start information
			ProcessStartInfo start = new ProcessStartInfo
			{
				FileName = pythonPath,
				Arguments = $"\"{scriptPath}\" \"{audioFilePath}\"",
				RedirectStandardOutput = true,  // Capture the output
				RedirectStandardError = true,  // Capture errors
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = scriptDirectory
			};

			//Process process = new Process();
			//process.StartInfo.FileName = pythonPath;
			//process.StartInfo.Arguments = $"\"{scriptPath}\" \"{audioFilePath}\"",
			//process.StartInfo.RedirectStandardOutput = true;
			//process.StartInfo.RedirectStandardError = true;
			//process.StartInfo.UseShellExecute = false;
			//process.StartInfo.CreateNoWindow = true;

			//process.Start();

			// Start the process
				using (Process process = Process.Start(start))
				{
					// Read the output
					string output = process.StandardOutput.ReadToEnd();
					string error = process.StandardError.ReadToEnd();
					string result = process.StandardOutput.ReadToEnd().Trim();

					process.WaitForExit();

					//Console.WriteLine("Raw Output: " + output);
					//Console.WriteLine("Raw Error: " + error);

					// Clean the output by extracting the prediction result
					string[] outputLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					string prediction = outputLines[^1].Trim(); // Get the last line (assumes prediction is the last output)

					//Console.WriteLine("Prediction Result: " + prediction);

					// Check the prediction result
					// Check if the prediction is correct or incorrect
					if (prediction == "1")
					{
						Console.WriteLine("Prediction: Correct word");
						if (Dieter.taskSchedule != null && Dieter.tsStatic != null)
						{
							Dieter.taskSchedule.Invoke(() =>
							{
								try
								{
									listeningMode = true;
									Dieter.tsStatic.SendChannelMessage("Listening mode activated");
									SendMp3("mussteSein.wav");

								}
								catch (Exception ex)
								{
									Console.WriteLine($"Error sending message: {ex.Message}");
									//SendMp3();
								}
							});
						}
						else
						{
							Console.WriteLine("Task scheduler or TsFullClient is not initialized.");
						}
						//StartStreaming();

						//tsFullClient.SendChannelMessage("Prediction: Correct word");
						//if (Dieter.TsFullClientSyncContext != null)
						//{
						//	Dieter.TsFullClientSyncContext.Post(_ =>
						//	{
						//		TSHolder.TsFullClientInstance.SendChannelMessage("Predicted keyword detected!");
						//	}, null);
						//}
						//Dieter.HandleCorrectPrediction(audioFilePath);

					}
					else if (prediction == "0")
					{
						Console.WriteLine("Prediction: Incorrect word");
						if (Dieter.taskSchedule != null && Dieter.tsStatic != null)
						{
							Dieter.taskSchedule.Invoke(() =>
							{
								try
								{
									Dieter.tsStatic.SendChannelMessage("Prediction: Incorrect word");
								}
								catch (Exception ex)
								{
									Console.WriteLine($"Error sending message: {ex.Message}");
								}
							});
						}
						else
						{
							Console.WriteLine("Task scheduler or TsFullClient is not initialized.");
						}
						//StartStreaming();
						//if (Dieter.TsFullClientSyncContext != null)
						//{
						//	Dieter.TsFullClientSyncContext.Post(_ =>
						//	{
						//		TSHolder.TsFullClientInstance.SendChannelMessage("Prediction: Incorrect word");
						//	}, null);
						//}
						//tsFullClient.SendChannelMessage("Prediction: Incorrect word");
					}
					else
					{
						Console.WriteLine("Unexpected prediction output.");
					}


					// Wait for the process to complete
					//// Print the output
					//Console.WriteLine("Output:");
					//Console.WriteLine(output);

					//// Print any errors
					//if (!string.IsNullOrEmpty(error))
					//{
					//	Console.WriteLine("Error:");
					//	Console.WriteLine(error);
					//}
				}
				isPredicting = false;
			
		}
			

		public void Dispose()
		{
			silenceTimer.Dispose();// Ensure any remaining data is written
			waveFileWriter?.Dispose();
			Log.Info("File Pipe Disposed.");

			

			//for (int i = 1; i < fileNumber; i++)
			//{
			//	string oldFilePath = System.IO.Path.Combine(directoryPath, $"rec_{i}.wav");
			//	if (System.IO.File.Exists(oldFilePath))
			//	{
			//		try
			//		{
			//			System.IO.File.Delete(oldFilePath);
			//			Log.Info($"Deleted existing file: {oldFilePath}");
			//		}
			//		catch (Exception ex)
			//		{
			//			Log.Error($"Error deleting file: {ex.Message}");
			//			return;
			//		}
			//	}
			//}
		}
	}

}
	

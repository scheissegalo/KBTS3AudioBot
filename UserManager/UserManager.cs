using TS3AudioBot.Audio;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TSLib;
using TSLib.Full.Book;
using TSLib.Full;
using LiteDB;
using TSLib.Messages;
using System.Linq;
using System.Text.RegularExpressions;
using System;
//
//using Microsoft.VisualBasic; 68

namespace UserManager
{
	public class UserManager : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private PlayManager playManager;
		private Ts3Client ts3Client;
		private Connection serverView;

		private LiteDatabase _db;
		private ILiteCollection<TSUser> DBusers;
		private LocalizationManager localizationManager = new LocalizationManager();
		private Dictionary<string, Func<TextMessage, Task>> commandDictionary;
		private ChannelId parentChannel = (ChannelId)68;
		private ServerGroupId setupDoneGroup = (ServerGroupId)7;


		public readonly List<ServerGroupId> BotGroups = new List<ServerGroupId> { (ServerGroupId)11, (ServerGroupId)47, (ServerGroupId)115 };


		private bool isInitialized = false;

		public UserManager(PlayManager playManager, Ts3Client ts3Client, Connection serverView, TsFullClient tsFull, Player playerConnection)
		{
			this.playManager = playManager;
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			// Initialize the command dictionary
			commandDictionary = new Dictionary<string, Func<TextMessage, Task>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "skipsetup", HandleSkipSetup },
				{ "hello", HandleHello },
				{ "hi", HandleHello },
				{ "sup", HandleHello },
				{ "restart", RestartSetup }
				//{ "hello*", HandleHelloWildcard },
				//{ "!setlanguage", HandleSetLanguage },
				//{ "!setname", HandleSetName },
				//{ "!enableRanking", HandleEnableRanking },
				//{ "!disableRanking", HandleDisableRanking },
				//{ "!createChannel", HandleCreateChannel }
				// Add more commands here
			};
		}

		private async Task RestartSetup(TextMessage message)
		{
			TSUser? user = DBusers.FindOne(u => u.UserID == message.InvokerUid);

			if (user.WantsOwnChannel && user.ChannelID == (ChannelId)0)
			{
				user.SetupDate = DateTime.UtcNow;
				user.SetupStep = 0;
				DBusers.Update(user);
				await tsFullClient.SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "restartSetup")}", user.ClientID);
			}
			else
			{
				await tsFullClient.SendPrivateMessage("you still have a channel!", user.ClientID);
			}

		}

		public async void Initialize()
		{
			InitializeDB();
			await CheckUser();
			//tsFullClient.OnClientChatClosed += OnClientChatClosed;
			//tsFullClient.OnClientChatComposing += OnClientChatComposing;
			//tsFullClient.OnEachClientChatClosed += OnEachClientChatClosed;
			//tsFullClient.OnEachClientChatComposing += OnEachClientChatComposing;
			tsFullClient.OnEachTextMessage += OnEachTextMessage;
		}

		private async void OnEachTextMessage(object? sender, TextMessage e)
		{
			if (commandDictionary.TryGetValue(e.Message, out var commandHandler))
			{
				// If command exists, invoke the handler
				await commandHandler(e);
			}
			else
			{
				// Usersetup
				TSUser? user = DBusers.FindOne(u => u.UserID == e.InvokerUid);

				if (user != null)
				{
					string TSCountryCode = await GetUserCountryCodeFromTS(user);

					string userCountryCode = user.CountryCode;
					string localizedYes = localizationManager.GetTranslation(userCountryCode, "yes").ToLower();
					string localizedNo = localizationManager.GetTranslation(userCountryCode, "no").ToLower();
					string TranslateBool(bool condition) => localizationManager.GetTranslation(userCountryCode, condition ? "yes" : "no");
					//Console.WriteLine($"Processing setup step: {((SetupStep)user.SetupStep)}");
					// Depending on the setup step, process the user's response
					switch ((SetupStep)user.SetupStep)
					{
					case SetupStep.Welcome:
						string message;
						string answer;

						//Send english Backup incase user is on VPN
						if (TSCountryCode != "en")
						{
							string messageEN = localizationManager.GetTranslation("en", "welcomeMessage");
							await tsFullClient.SendPrivateMessage($"Backup English: {localizationManager.GetTranslation("en", "hello")} {user.Nickname}!\n {messageEN}", user.ClientID);
						}
						message = localizationManager.GetTranslation(TSCountryCode, "welcomeMessage");
						await tsFullClient.SendPrivateMessage($"{localizationManager.GetTranslation(TSCountryCode, "hello")} {user.Nickname}!\n {message}", user.ClientID);

						if (TSCountryCode != "en")
						{
							string messageEN = localizationManager.GetTranslation("en", "whatIsYourLanguage");
							await tsFullClient.SendPrivateMessage($"Backup English:{localizationManager.GetTranslation("en", "skipSetup")}\n{messageEN} [b]{TSCountryCode}[/b]!", user.ClientID);
						}
						message = localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
						await tsFullClient.SendPrivateMessage($"{localizationManager.GetTranslation(TSCountryCode, "skipSetup")}\n{message} [b]{TSCountryCode}[/b]!", user.ClientID);

						//Send user to Select Language step
						user.SetupStep = (int)SetupStep.AskPreferredLanguage;
						break;

					case SetupStep.AskPreferredLanguage:
						if (IsValidCountryCode(e.Message))
						{
							user.CountryCode = e.Message;  // Save the language notValidCountryCode
							DBusers.Update(user);
							user = DBusers.FindById(user.Id);

							userCountryCode = user.CountryCode;

							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "acceptRules"), user.ClientID);
							user.SetupStep = (int)SetupStep.AcceptRules;
						}
						else
						{
							if (TSCountryCode != "en")
							{
								string messageEN = localizationManager.GetTranslation("en", "whatIsYourLanguage");
								await tsFullClient.SendPrivateMessage($"Backup English: {messageEN} !\n {localizationManager.GetTranslation("en", "skipSetup")}", user.ClientID);
							}
							message = localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
							await tsFullClient.SendPrivateMessage($"{message} {TSCountryCode}!\n {localizationManager.GetTranslation(TSCountryCode, "skipSetup")}", user.ClientID);

							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(TSCountryCode, "notValidCountryCode"), user.ClientID);
						}
						break;

					case SetupStep.AcceptRules:
						
						Console.WriteLine($"Translated yes: {localizedYes}, No: {localizedNo}");
						answer = e.Message.ToLower();

						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.AcceptedRules = answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);
							//user.AcceptedRules = answer.Equals(localizedYes);
							DBusers.Update(user);
							user = DBusers.FindById(user.Id);

							user.SetupStep = (int)SetupStep.AskRankingPreference;
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "rankingDisabled"), user.ClientID);
						}
						else
						{
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "acceptRules"), user.ClientID);
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo"), user.ClientID);
						}

						//SendMessage(user, "Do you want ranking disabled? (yes/no)"); yourOwnChannel
						break;

					case SetupStep.AskRankingPreference:
						answer = e.Message.ToLower();
						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.RankingEnabled = answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);  // Save the ranking preference
							user.SetupStep = (int)SetupStep.AskChannelPreference;
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "yourOwnChannel"), user.ClientID);
						}
						else
						{
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "rankingDisabled"), user.ClientID);
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo"), user.ClientID);
						}
						//SendMessage(user, "Do you want your own channel? (yes/no)");setupComplete
						break;

					case SetupStep.AskChannelPreference:
						answer = e.Message.ToLower();
						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.WantsOwnChannel = e.Message.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);  // Save channel preference
							user.SetupDone = true;
							user.SetupStep = (int)SetupStep.Completed;
							DBusers.Update(user);
							user = DBusers.FindById(user.Id);

							var dbuser = await tsFullClient.GetClientDbIdFromUid(user.UserID);
							await tsFullClient.ServerGroupAddClient(setupDoneGroup, dbuser.Value.ClientDbId);
							if (user.WantsOwnChannel)
							{
								var newChannelId = await tsFullClient.ChannelCreate(
									user.Name ,
									parent: parentChannel,
									type: ChannelType.Permanent,
									description:$"Created with [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] N-SYS for {user.Name} at {DateTime.Now.ToString()}");
								await tsFullClient.ClientMove(user.ClientID, newChannelId.Value.ChannelId);
								await tsFullClient.ChannelGroupAddClient((ChannelGroupId)5, newChannelId.Value.ChannelId, dbuser.Value.ClientDbId);
								await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "addPassword"), user.ClientID);
								user.ChannelID = newChannelId.Value.ChannelId;
								DBusers.Update(user);
							}
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "setupComplete"), user.ClientID);
						}
						else
						{
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "yourOwnChannel"), user.ClientID);
							await tsFullClient.SendPrivateMessage(localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo"), user.ClientID);
						}
						//SendMessage(user, "Setup is complete! Enjoy your time on the server.");addPassword
						break;

					case SetupStep.Completed:
						string acceptedRules = TranslateBool(user.AcceptedRules);
						string rankingEnabled = TranslateBool(user.RankingEnabled);
						string setupDone = TranslateBool(user.SetupDone);
						string skippedSetup = TranslateBool(user.SkipSetup);
						string hasOwnChannel = TranslateBool(user.WantsOwnChannel);

						await tsFullClient.SendPrivateMessage($"{localizationManager.GetTranslation(userCountryCode, "welcomeBack")} {user.Nickname}!", user.ClientID);
						await tsFullClient.SendPrivateMessage($@"
[b]{localizationManager.GetTranslation(userCountryCode, "userStats")}:[/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "countryCode")}[/color][b]: [color=red]{user.CountryCode}[/color][/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "rulesAccepted")}[/color][b]: [color=red]{acceptedRules}[/color][/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "rankingEnabled")}[/color][b]: [color=red]{rankingEnabled}[/color][/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "score")}[/color][b]: [color=red]{user.Score}[/color][/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "setupDone")}[/color][b]: [color=red]{setupDone}[/color][/b]
[color=green]{localizationManager.GetTranslation(userCountryCode, "skippedSetup")}[/color][b]: [color=red]{skippedSetup}[/color][/b]
{user.ChannelID.Value.ToString()}
[color=green]{localizationManager.GetTranslation(userCountryCode, "ownChannel")}[/color][b]: [color=red]{hasOwnChannel}[/color][/b]", user.ClientID);
						//await tsFullClient.SendPrivateMessage($"{localizationManager.GetTranslation(userCountryCode, "howToHelpToday")}!", user.ClientID);
						//SendMessage(user, "Welcome back!");
						break;

					default:
						// Handle unexpected cases
						await tsFullClient.SendPrivateMessage("Something went wrong with your setup.", user.ClientID);
						//SendMessage(user, "Something went wrong with your setup.");
						break;
					}

					// Update the user in the database after processing the message
					DBusers.Update(user);
				}
			}

		}

		// Method to check if the country code is valid
		public bool IsValidCountryCode(string countryCode)
		{
			// Example regex for validating country code, assuming it's a 2-letter ISO country code (e.g., 'US', 'DE', 'IN', etc.)
			string pattern = @"^[A-Za-z]{2}$";

			// Use Regex to check if the country code matches the pattern
			return Regex.IsMatch(countryCode, pattern);
		}

		public async Task<string> GetUserCountryCodeFromTS(TSUser tsuser)
		{
			var fulluser = await tsFullClient.ClientInfo(tsuser.ClientID);
			if (fulluser.Value.CountryCode == null)
			{
				return "en";
			}
			return fulluser.Value.CountryCode;
		}

		// Command handler methods
		private async Task HandleSkipSetup(TextMessage e)
		{
			// Handle !skipsetup command
			TSUser? user = DBusers.FindOne(u => u.UserID == e.InvokerUid);
			if (user != null)
			{
				string TSCountryCode = await GetUserCountryCodeFromTS(user);
				//Send english Backup incase user is on VPN setupSkipped
				if (TSCountryCode != "en")
				{
					string messageEN = localizationManager.GetTranslation("en", "setupSkipped");
					await tsFullClient.SendPrivateMessage($"Backup English: {messageEN}", user.ClientID);
				}

				string message = localizationManager.GetTranslation(TSCountryCode, "setupSkipped");
				await tsFullClient.SendPrivateMessage(message, user.ClientID);
			}
		}

		private async Task HandleHello(TextMessage e)
		{
			// Respond to a simple hello command
			TSUser? user = DBusers.FindOne(u => u.UserID == e.InvokerUid);
			if (user != null)
			{
				string TSCountryCode = await GetUserCountryCodeFromTS(user);
				string message = localizationManager.GetTranslation(TSCountryCode, "helloMessage");
				await tsFullClient.SendPrivateMessage(message, user.ClientID);
			}
		}


		private void OnEachClientChatComposing(object? sender, ClientChatComposing e) => throw new NotImplementedException();
		private void OnEachClientChatClosed(object? sender, ClientChatClosed e) => throw new NotImplementedException();
		private void OnClientChatComposing(object sender, IEnumerable<ClientChatComposing> e) => throw new NotImplementedException();
		private void OnClientChatClosed(object sender, IEnumerable<ClientChatClosed> e) => throw new NotImplementedException();

		private bool MatchesCommandPattern(string message, string pattern)
		{
			// Check if the pattern has a wildcard and match accordingly
			if (pattern.EndsWith("*"))
			{
				// Match any message starting with the pattern (e.g., "hello*")
				string prefix = pattern.Substring(0, pattern.Length - 1);  // Remove the "*"
				return message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
			}

			// Exact match for other commands
			return string.Equals(message, pattern, StringComparison.OrdinalIgnoreCase);
		}

		private void InitializeDB()
		{
			try
			{
				// Initialize the LiteDB database and assign it to the class-level variable
				_db = new LiteDatabase(@"Filename=ts_users.db;Upgrade=true;");

				// Get or create the collection and assign it to the class-level variable
				DBusers = _db.GetCollection<TSUser>("tsusers");

				if (DBusers == null)
				{
					Console.WriteLine("Database collection 'tsusers' is null!");
					return;
				}

				Console.WriteLine("Database initialized successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private async Task CheckUser()
		{
			try
			{
				var allUsers = await tsFullClient.ClientList();

				foreach (var user in allUsers.Value)
				{

					//Check if user is not a query user
					if (user.ClientType.Equals(ClientType.Full))
					{
						var fulluser = await tsFullClient.ClientInfo(user.ClientId);
						// Check if is a bot
						ServerGroupId[] userServerGroups = fulluser.Value.ServerGroups;
						if (!userServerGroups.Any(sg => BotGroups.Contains(sg)))
						{
							//Console.WriteLine($"User {user.Name} {fulluser.Value.Uid} has ServerGroups: {String.Join(",", fulluser.Value.ServerGroups)}");

							//If user exists in database
							if(DBusers.FindOne(u => u.UserID == fulluser.Value.Uid) == null)
							{
								// User does not exist, so add them to the database
								//DBusers.Insert(user);
								TSUser tSUser = new TSUser
								{
									UserID = fulluser.Value.Uid,
									Name = fulluser.Value.Name,
									ClientID = user.ClientId,
									LastUpdate = DateTime.Now,
								};
								DBusers.Insert(tSUser);
								Console.WriteLine($"User {tSUser.Name} added to the database.");
								//Start User Interaction
							}
							else
							{
								TSUser? tsuser = LoadUserFromDB(fulluser.Value.Uid);
								if (tsuser != null)
								{
									Console.WriteLine($"User {tsuser.Name} already exists in the database. Last Checked: {tsuser.LastUpdate.ToString("HH:mm:ss")}");
									tsuser.LastUpdate = DateTime.Now;
									tsuser.ClientID = user.ClientId;
									// Save the updated timestamp back to the database
									DBusers.Update(tsuser);
								}
								
							}
						}						
					}
				}

				if (DBusers != null)
				{
					//DBusers.Insert(user);
					//Console.WriteLine("Access to database!");
				}


			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		public TSUser? LoadUserFromDB(Uid uid)
		{
			TSUser user = DBusers.FindOne(u => u.UserID == uid);

			if (user != null)
			{
				Console.WriteLine($"User found: {user.Name}");
				return user;
			}
			else
			{
				Console.WriteLine("User not found in the database.");
			}

			return null;
		}

		private async Task PokeUser(ClientId clientId, string message)
		{
			const int maxChunkSize = 95; // Poke Message Legth

			// Split the message into chunks
			var chunks = SplitIntoChunks(message, maxChunkSize);

			// Send each chunk through PokeClient
			foreach (var chunk in chunks)
			{
				await tsFullClient.PokeClient(chunk, clientId);
			}
		}

		private List<string> SplitIntoChunks(string message, int chunkSize)
		{
			List<string> chunks = new List<string>();
			for (int i = 0; i < message.Length; i += chunkSize)
			{
				int endIndex = Math.Min(i + chunkSize, message.Length);
				chunks.Add(message.Substring(i, endIndex - i));
			}
			return chunks;
		}

		public void Dispose()
		{
			_db?.Dispose();
		}

	}

	public enum SetupStep
	{
		Welcome = 0,
		AskPreferredLanguage = 1,
		AcceptRules = 2,
		AskRankingPreference = 3,
		AskChannelPreference = 4,
		Completed = 5
	}

	public class TSUser
	{
		public ObjectId? Id { get; set; }
		public Uid UserID { get; set; }
		public ClientId ClientID { get; set; }
		public string? Name { get; set; }
		public float Score { get; set; } = 0f;
		public string? Nickname { get; set; }
		public bool SetupDone { get; set; } = false;
		public bool SkipSetup { get; set; } = false;
		public bool AcceptedRules { get; set; } = false;
		public int SetupStep { get; set; } = 0;
		public bool WantsOwnChannel { get; set; } = false;
		public DateTime SetupDate { get; set; }
		public DateTime LastUpdate { get; set; }
		public string CountryCode { get; set; } = "en";
		public bool RankingEnabled { get; set; } = false;
		public ChannelId ChannelID { get; set; }
		public TimeSpan OnlineTime { get; set; }

	}
}

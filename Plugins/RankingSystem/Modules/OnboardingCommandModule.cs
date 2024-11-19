using System;
using System.Threading.Tasks;
using LiteDB;
using RankingSystem.Interfaces;
using TSLib.Full;
using TSLib;
using TSLib.Messages;
using RankingSystem.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace RankingSystem.Modules
{
	public class OnboardingCommandModule : ICommandModule
	{
		private readonly IUserRepository _userRepository;
		private readonly IServerGroupManager _serverGroupManager;
		private readonly IChannelManager _channelManager;
		private Constants constants = new Constants();
		private LocalizationManager localizationManager = new LocalizationManager();
		private TsFullClient _tsFullClient;

		public OnboardingCommandModule(IUserRepository userRepository, IServerGroupManager serverGroupManager, IChannelManager channelManager, TsFullClient tsFullClient)
		{
			_userRepository = userRepository;
			_serverGroupManager = serverGroupManager;
			_channelManager = channelManager;
			_tsFullClient = tsFullClient;
		}

		public void RegisterCommands(Services.CommandManager commandManager)
		{
			//commandManager.RegisterCommand("skipsetup", HandleSkipSetup);
			//commandManager.RegisterCommand("hello", HandleHello);
			// Register Commands
			commandManager.RegisterCommand("skipsetup", HandleSkipSetup);
			commandManager.RegisterCommand("hello", HandleHello);
			commandManager.RegisterCommand("hi", HandleHello);
			commandManager.RegisterCommand("sup", HandleHello);
			commandManager.RegisterCommand("restart", HandleRestartSetup);
			commandManager.RegisterCommand("createmychannel", HandleCreateMyChannel);
			commandManager.RegisterCommand("deletemychannel", HandleDeleteMyChannel);
			commandManager.RegisterCommand("help", HandleHelp);
			commandManager.RegisterCommand("help me", HandleHelp);
			commandManager.RegisterCommand("setlanguage", HandleSetLanguage);
			commandManager.RegisterCommand("mychannel", HandleMoveToMyChannel);
			commandManager.RegisterCommand("buy", HandleBuyFeatures);
			commandManager.RegisterCommand("shop", HandleBuyFeatures);
			commandManager.RegisterCommand("sendcredit", HandleSendCredit);
			commandManager.RegisterCommand("sendcredits", HandleSendCredit);
			commandManager.RegisterCommand("disablestatus", HandleDisableStatus);
			//commandManager.RegisterCommand("importdb", HandleDBImport);
			//commandManager.RegisterCommand("addsteam", HandleAddSteam);
			// Register Admin Commands
			commandManager.RegisterCommand("addcredit", HandleAddCredit);
			commandManager.RegisterCommand("addusercredit", HandleAddUserCredit);
			commandManager.RegisterCommand("setstep", HandleSetStep);
		}

		private async Task HandleAddSteam(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				Console.WriteLine("User not found");
				return;
			}

			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				//string errorMessage = "Too many arguments";
				await SendPrivateMessage("Not enought arguments", user.ClientID);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();

			user.SteamID = argument;
			_userRepository.Update(user);
			await SendPrivateMessage("Steam ID Added", user.ClientID);
		}

		private async Task HandleSetStep(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				Console.WriteLine("User not found");
				return;
			}

			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				//string errorMessage = "Too many arguments";
				await _tsFullClient.SendPrivateMessage("Not enought arguments", user.ClientID);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();

			if (int.TryParse(argument, out int floatValue))
			{
				user.SetupStep = floatValue;
				_userRepository.Update(user);
				await _tsFullClient.SendPrivateMessage($"Updated, your SetupStep: {user.SetupStep}", user.ClientID);
			}
			else
			{
				await _tsFullClient.SendPrivateMessage("Wrong format, Example: setstep 0-?", user.ClientID);
			}

		}

		private async Task HandleDisableStatus(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				//Console.WriteLine("User not found");
				return;
			}
			if (user.DailyStatusEnabled)
			{
				user.DailyStatusEnabled = false;
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "dailyPersonalStatus") + " " +
					localizationManager.GetTranslation(user.CountryCode, "disabled"), user.ClientID, true);
			}
			else
			{
				user.DailyStatusEnabled = true;
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "dailyPersonalStatus") + " " +
					localizationManager.GetTranslation(user.CountryCode, "enabled"), user.ClientID, true);
			}
			_userRepository.Update(user);

		}

		//private async Task HandleDBImport(TextMessage message)
		//{
		//	if (ImportDatabase())
		//	{
		//		await _tsFullClient.SendPrivateMessage("Import Successfull", message.InvokerId);
		//	}
		//	else
		//	{
		//		await _tsFullClient.SendPrivateMessage("Import Error", message.InvokerId);
		//	}
		//}

		//public bool ImportDatabase()
		//{
		//	try
		//	{
		//		// Initialize the LiteDB database using the recommended approach
		//		using (var db = new LiteDatabase(@"Filename=rank_users.db;Upgrade=true;"))
		//		{
		//			// Get a collection (or create it if it doesn't exist)
		//			var DBusers = db.GetCollection<User>("users");

		//			if (DBusers == null)
		//			{
		//				Console.WriteLine("Database collection 'users' is null!");
		//				return false;
		//			}

		//			// Fetch all users in the collection
		//			var users = DBusers.FindAll().ToList();

		//			foreach (var user in users)
		//			{
		//				// Calculate credits based on online time
		//				double totalMinutes = user.OnlineTime.TotalMinutes;
		//				float credits = (float)totalMinutes * constants.ScorePerTick;

		//				// Feed the data to your new database
		//				Console.WriteLine($"Importing user: {user.Name} - {user.Nickname}, ID: {user.UserID}");
		//				var newUser = new TSUser
		//				{
		//					UserID = (Uid)user.UserID,
		//					Name = user.Name,
		//					OnlineTime = user.OnlineTime,
		//					LastUpdate = DateTime.Now,
		//					SkipSetup = true,
		//					SetupStep = 0,
		//					Score = credits
		//				};

		//				// Add the user to your new database
		//				_userRepository.Insert(newUser);
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine(ex.Message);
		//		return false;
		//	}

		//	return true;
		//}

		private async Task HandleSendCredit(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				//Console.WriteLine("User not found");
				return;
			}
			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument notEnoughtArguments
				//string errorMessage = "Too many arguments";
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtArguments"), user.ClientID, true);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();
			string username = parts[2].Trim();

			if (float.TryParse(argument, out float floatValue))
			{
				if (!CheckIfUserHasEnoughtCredit(user, floatValue))
				{
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtCredits")+ "😥", user.ClientID, true);
					return;
				}

				TSUser? updateUser = _userRepository.FindOneByName(username);
				if (updateUser == null)
				{
					// Handle case where user is not found userNotFound
					//Console.WriteLine("User null");
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "userNotFound"), user.ClientID, true);
					return;
				}
				//Console.WriteLine($"Parsed float: {floatValue}"); // Output: Parsed float: 42.42 from
				updateUser.Score += floatValue;
				user.Score -= floatValue;
				_userRepository.Update(updateUser);
				_userRepository.Update(user);
				await SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "creditsSend")} {updateUser.Name}. {localizationManager.GetTranslation(user.CountryCode, "yourCredits")}: {user.Score}", user.ClientID, true);
				await SendPrivateMessage($"🎉💰{localizationManager.GetTranslation(user.CountryCode, "youRecieved")} {floatValue} {localizationManager.GetTranslation(user.CountryCode, "credits")} {localizationManager.GetTranslation(user.CountryCode, "from")}: {user.Name}", updateUser.ClientID, true);
			}
			else
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "wrongFormatCredit"), user.ClientID, true);
				//Console.WriteLine("Invalid float format!");wrongFormatCredit
			}
		}

		private async Task HandleAddUserCredit(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				//Console.WriteLine("User not found");
				return;
			}
			if (!await IsUserAdministrator(user))
			{
				//Console.WriteLine("Not Admin");
				return;
			}
			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
			{
				// No argument provided
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtArguments"), user.ClientID, true);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();
			string username = parts[2].Trim();

			if (float.TryParse(argument, out float floatValue))
			{
				TSUser? updateUser = _userRepository.FindOneByName(username);
				if (updateUser == null)
				{
					// Handle case where user is not found
					//Console.WriteLine("User null");
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "userNotFound"), user.ClientID, true);
					return;
				}

				updateUser.Score += floatValue;
				_userRepository.Update(updateUser);
				//await SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "creditsSend")} {updateUser.Name}. {localizationManager.GetTranslation(user.CountryCode, "yourCredits")}: {user.Score}", user.ClientID, true);
				await SendPrivateMessage($"🎉💰{localizationManager.GetTranslation(user.CountryCode, "youRecieved")} {floatValue} {localizationManager.GetTranslation(user.CountryCode, "credits")} {localizationManager.GetTranslation(user.CountryCode, "from")}: {user.Name}", updateUser.ClientID, true);
				await SendPrivateMessage($"Updated {updateUser.Name}'s credits: {updateUser.Score}", user.ClientID, true);
				//await _tsFullClient.SendPrivateMessage($"You recived {floatValue} credits from {user.Name}", updateUser.ClientID);
			}
			else
			{
				await SendPrivateMessage("Wrong format, Example: addusercredit 10 Username", user.ClientID, true);
				//Console.WriteLine("Invalid float format!");
			}
		}
		private async Task HandleAddCredit(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				//Console.WriteLine("User null");
				return;
			}
			if (!await IsUserAdministrator(user))
			{
				//Console.WriteLine("Not Admin");
				return;
			}

			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtArguments"), user.ClientID, true);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();

			if (float.TryParse(argument, out float floatValue))
			{
				user.Score += floatValue;
				_userRepository.Update(user);
				await SendPrivateMessage($"Updated, your credits: {user.Score}", user.ClientID, true);
			}
			else
			{
				await SendPrivateMessage("Wrong format, Example: addcredit 10", user.ClientID, true);
			}

		}

		private async Task HandleBuyFeatures(TextMessage e)
		{
			if (e.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(e.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				return;
			}
			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			string ShopPage = @$"
******* [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] *******
--- {localizationManager.GetTranslation(user.CountryCode, "shop")} --- | --- {localizationManager.GetTranslation(user.CountryCode, "shop")} --- | --- {localizationManager.GetTranslation(user.CountryCode, "shop")} ---

[color=green][b]{localizationManager.GetTranslation(user.CountryCode, "feature")}[/b][/color] | [color=green]{localizationManager.GetTranslation(user.CountryCode, "price")}[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "description")} [/color]

Work in Progress! use at your own risk :D

bannermsg | 15 | add a message to the server banner.
addchannel | 35 | additional channel
addbanner | 50 | add your own banner background.
moveright | 50 | The right to move other clients.
banright | 100 | The right to ban other clients.
moderator | 250 | Moderator rights (ban, move, elevated rights).
moderatorplus | 300 | Moderator rights + Create and edit channels.
administrator | 500 | Administrator rights

";

			// Split the message into command and argument
			string[] parts = e.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				//string errorMessage = "Too many arguments";
				await _tsFullClient.SendPrivateMessage(ShopPage, user.ClientID);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();
			string statusMessage = "";
			//Aditinal checks
			switch (argument)
			{
			case "show":
				statusMessage = ShopPage;
				break;
			case "addchannel":
				if (CheckIfUserHasEnoughtCredit(user, 25f))
				{
					//create channel logic
					user.Score -= 25;
					_userRepository.Update(user);
					await CreateAdditionalChannel(user);
					await SendPrivateMessage($"You have successfully bought a new channel your credits: {user.Score}", user.ClientID, true);
				}
				else
				{
					await SendPrivateMessage($"You do not have enought credits to buy a new channel. Your credits: {user.Score}", user.ClientID, true);
				}
				//handle addchannel
				break;
			case "moveright":
				//handle addchannel
				break;
			default:
				//code
				break;
			}
			await SendPrivateMessage(statusMessage, user.ClientID);
		}

		private bool CheckIfUserHasEnoughtCredit(TSUser user, float amount)
		{
			if (user.Score >= amount)
			{
				return true;
			}
			return false;
		}

		private async Task HandleMoveToMyChannel(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				return;
			}
			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			//Has no channel
			if (user.ChannelIDInt != 0)
			{
				if (await _channelManager.DoesChannelExist((ChannelId)user.ChannelIDInt))
				{
					await _tsFullClient.ClientMove(message.InvokerId, (ChannelId)user.ChannelIDInt);
				}
				else
				{
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "noChannelToMove"), user.ClientID, true);
				}
			}
			else
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "noChannelToMove"), user.ClientID, true);
			}
		}

		private async Task HandleSetLanguage(TextMessage e)
		{
			if (e.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(e.InvokerUid.Value);
			if (user == null)
			{
				// Handle case where user is not found
				return;
			}

			// Split the message into command and argument
			string[] parts = e.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				string errorMessage = localizationManager.GetTranslation("en", "noCountryCode");
				await SendPrivateMessage(errorMessage +" "+ localizationManager.GetTranslation("en", "countryCodeUsage"), user.ClientID, true);
				return;
			}

			// Extract the argument (language code)
			string languageCode = parts[1].Trim().ToLower();

			// Check if the provided code is valid
			if (IsValidCountryCode(languageCode))
			{
				// Save the language
				user.CountryCode = languageCode;
				_userRepository.Update(user);

				await SendPrivateMessage(localizationManager.GetTranslation(languageCode, "successSetLanguage"), user.ClientID, true);

			}
			else
			{
				// Handle invalid country code
				string tsCountryCode = await GetUserCountryCodeFromTS(user);

				string errorMessage = localizationManager.GetTranslation(tsCountryCode, "notValidCountryCode");
				//string languagePrompt = localizationManager.GetTranslation(tsCountryCode, "whatIsYourLanguage");description

				await SendPrivateMessage(errorMessage, user.ClientID, true);
			}
		}

		private async Task HandleHelp(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				Console.WriteLine("InvokerUid is null.");
				return;
			}

			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			string adminHelp = "";
			if (user == null)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}
			if (await IsUserAdministrator(user))
			{
				adminHelp = $@"Only visible to admins:

addcredit | <amount> | add credit at your account.
addusercredit | <amount> <user> | add credit to user.
";
			}
			//setLanguage
			await _tsFullClient.SendPrivateMessage(@$"
******* [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] *******
--- {localizationManager.GetTranslation(user.CountryCode, "help")} --- | --- {localizationManager.GetTranslation(user.CountryCode, "help")} --- | --- {localizationManager.GetTranslation(user.CountryCode, "help")} ---

[color=green][b]Command[/b][/color] | [color=green]<argument> optional[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "description")} [/color]

(* commands not work atm)

[color=green][b]help[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendsThisHelp")}[/color]
[color=green][b]hello/hi/sup[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendTestText")}[/color]
[color=green][b]skipsetup[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "toSkipOnboarding")}[/color]
[color=green][b]restart[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "restartOnboarding")}[/color]
[color=green][b]deletemychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "deleteYourOwnChannel")}[/color]
[color=green][b]createmychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "createYourOwnChannel")}[/color]
[color=green][b]mychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "movesIntoYourChannel")}[/color]
[color=green][b]disablestatus[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "disableStatus")}[/color]
[color=green][b]shop/buy[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "shopDescript")}[/color]
[color=green][b]setlanguage[/b][/color] [color=green]<language code>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "setLanguage")} (en/de/tr/ru/ir/cz/pl/ae/pt/hu/fi)[/color]
[color=green][b]sendcredit[/b][/color] [color=green]<amount> <username>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendcredit")}[/color]
[color=green][b]*addsteam[/b][/color] [color=green]<steamid>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "addSteam")}[/color]
{adminHelp}

", message.InvokerId);
		}

		private async Task HandleCreateMyChannel(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				await SendPrivateMessage("User not found in the database.", message.InvokerId);
				return;
			}

			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			if (user.ChannelIDInt != 0)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "alreadHaveChannel"), user.ClientID, true);
				return;
			}

			if (HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, user))
			{
				var dbuser = await _tsFullClient.GetClientDbIdFromUid(user.UserID);
				var newChannelId = await _channelManager.CreateChannel(user.Name);
				if (newChannelId.HasValue)
				{
					user.ChannelIDInt = newChannelId.Value.Value;
					user.ChannelID = newChannelId.Value;
					user.WantsOwnChannel = true;
					_userRepository.Update(user);

					await _tsFullClient.ClientMove(user.ClientID, newChannelId.Value);
					await _tsFullClient.ChannelGroupAddClient((ChannelGroupId)5, newChannelId.Value, dbuser.Value.ClientDbId);
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "addPassword"), user.ClientID, true);
				}
			}
			else
			{
				if (!user.WantsOwnChannel)
				{
					user.WantsOwnChannel = true;
					_userRepository.Update(user);
				}

				await SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "notEnoughTime")} " +
					$"{localizationManager.GetTranslation(user.CountryCode, "onlineTime")}: " +
					$"{(int)user.OnlineTime.TotalMinutes} {localizationManager.GetTranslation(user.CountryCode, "minutes")}", user.ClientID, true);
			}
		}

		private async Task HandleDeleteMyChannel(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null || user.ChannelIDInt == 0)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "noChannelToDelete"), message.InvokerId, true);
				return;
			}
			await _channelManager.KickAllUsersFromChannel(user);
			bool deleted = await _channelManager.DeleteChannel((ChannelId)user.ChannelIDInt);
			if (deleted)
			{
				user.ChannelIDInt = 0;
				_userRepository.Update(user);
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "channelDeleted"), message.InvokerId, true);
				//await tsFullClient.SendPrivateMessage("Your channel has been deleted.", message.InvokerId); channelnoChannelToDeleteDeleted
			}
			else
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "failedTodeleteYourChannel")+" "+
					localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), message.InvokerId, true);
				//await SendPrivateMessage(, message.InvokerId, true);
			}
		}

		private async Task CreateAdditionalChannel(TSUser user)
		{
			if (user == null)
			{
				await SendPrivateMessage("User not found in the database.", user.ClientID, true);
				return;
			}

			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			if (HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, user))
			{
				var generator = new RandomNameGenerator();
				var dbuser = await _tsFullClient.GetClientDbIdFromUid(user.UserID);
				var newChannelId = await _channelManager.CreateChannel(user.Name);
				if (newChannelId.HasValue)
				{
					user.ChannelIDInt = newChannelId.Value.Value;
					user.ChannelID = newChannelId.Value;
					user.WantsOwnChannel = true;
					_userRepository.Update(user);

					await _tsFullClient.ClientMove(user.ClientID, newChannelId.Value);
					await _tsFullClient.ChannelGroupAddClient((ChannelGroupId)5, newChannelId.Value, dbuser.Value.ClientDbId);
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "addPassword"), user.ClientID, true);
				}
			}
			else
			{
				if (!user.WantsOwnChannel)
				{
					user.WantsOwnChannel = true;
					_userRepository.Update(user);
				}

				await SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "notEnoughTime")} " +
					$"{localizationManager.GetTranslation(user.CountryCode, "onlineTime")}: " +
					$"{(int)user.OnlineTime.TotalMinutes} {localizationManager.GetTranslation(user.CountryCode, "minutes")}", user.ClientID, true); //minutes
			}
		}

		private async Task HandleRestartSetup(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);

			if (user.ChannelIDInt == 0)
			{
				user.SetupDate = DateTime.UtcNow;
				user.SetupStep = 0;
				_userRepository.Update(user);
				await SendPrivateMessage($"{localizationManager.GetTranslation(user.CountryCode, "restartSetup")}", user.ClientID, true);
			}
			else
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "stillHaveChannel"), user.ClientID, true);
			}

		}

		private async Task HandleSkipSetup(TextMessage e)
		{
			if (e.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			// Handle !skipsetup stillHaveChannel
			TSUser user = _userRepository.FindOne(e.InvokerUid.Value);
			if (user != null)
			{
				if (user.SetupDone)
				{
					await SendPrivateMessage(localizationManager.GetTranslation("en", "alreadySetupDone"), user.ClientID, true);
					return;
				}
				string TSCountryCode = await GetUserCountryCodeFromTS(user);
				//Send english Backup incase user is on VPN setupSkipped
				string messageEN = "";
				if (TSCountryCode != "en")
				{
					messageEN = "Backup English: " + localizationManager.GetTranslation("en", "setupSkipped");
				}
				user.SkipSetup = true;
				user.CountryCode = TSCountryCode;
				_userRepository.Update(user);
				string message = $"{messageEN} | " + localizationManager.GetTranslation(TSCountryCode, "setupSkipped");
				await SendPrivateMessage(message, user.ClientID, true);
			}
		}

		private async Task HandleHello(TextMessage e)
		{
			if (e.InvokerUid == null)
			{
				//Console.WriteLine("InvokerUid is null.");
				return;
			}
			// Respond to a simple hello command
			TSUser? user = _userRepository.FindOne(e.InvokerUid.Value);
			if (user != null)
			{
				string TSCountryCode = await GetUserCountryCodeFromTS(user);
				string message = localizationManager.GetTranslation(TSCountryCode, "helloMessage");
				await SendPrivateMessage(message, user.ClientID);
			}
		}

		public async Task<bool> IsUserAdministrator(TSUser user)
		{
			var clientInfo = await _tsFullClient.ClientInfo(user.ClientID);
			if (clientInfo.Ok)
			{
				var userServerGroups = clientInfo.Value.ServerGroups;
				if (userServerGroups.Contains(constants.AdminGroup))
					return true;
			}
			return false;
		}

		public bool IsValidCountryCode(string countryCode)
		{
			// Example regex for validating country code, assuming it's a 2-letter ISO country code (e.g., 'US', 'DE', 'IN', etc.)
			string pattern = @"^[A-Za-z]{2}$";

			// Use Regex to check if the country code matches the pattern
			return Regex.IsMatch(countryCode, pattern);
		}

		public async Task<string> GetUserCountryCodeFromTS(TSUser tsuser)
		{
			//Console.WriteLine($"Trying to get Country code from user {tsuser.Name} ID: {tsuser.ClientID}");
			var fulluser = await _tsFullClient.ClientInfo(tsuser.ClientID);
			if (fulluser.Ok)
			{
				return fulluser.Value.CountryCode;
			}
			else
			{
				return "en";
			}
		}

		public bool HasUserSurpassedTimeThreshold(TimeSpan threshold, TSUser tsuser)
		{
			return tsuser.OnlineTime >= threshold;
		}

		private async Task<bool> SendPrivateMessage(string message, ClientId client, bool format = false)
		{
			if (format)
			{
				message = $@"[b][color=red]{message}[/color][/b]";
			}

			string formattetMessage = $@"{constants.messageHeader} {message} {constants.messageFooter}";
			var result = await _tsFullClient.SendPrivateMessage(formattetMessage, client);

			if (result.Ok)
			{
				return true;
			}

			return false;
		}

	}

	//public class User
	//{
	//	public string Id { get; set; }
	//	public string Name { get; set; }
	//	public string UserID { get; set; }
	//	public long Time { get; set; }
	//	public string Nickname { get; set; }
	//	public TimeSpan OnlineTime { get; set; }
	//	public bool IsAfk { get; set; }
	//	public bool IsNoAfkMode { get; set; }
	//	public bool IsAlone { get; set; }
	//	public DateTime LastUpdate { get; set; }
	//	public ServerGroupId RankGroup { get; set; }
	//	public ulong RankGroupInt { get; set; }
	//	public bool UpdateTime { get; set; }
	//}
}

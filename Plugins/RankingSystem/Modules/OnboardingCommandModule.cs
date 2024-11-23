// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Threading.Tasks;
using RankingSystem.Interfaces;
using TSLib.Full;
using TSLib;
using TSLib.Messages;
using RankingSystem.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using static RankingSystem.RankingModule;


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
		private List<ShopItem> _shopItems = new List<ShopItem>();

		public OnboardingCommandModule(IUserRepository userRepository, IServerGroupManager serverGroupManager, IChannelManager channelManager, TsFullClient tsFullClient)
		{
			_userRepository = userRepository;
			_serverGroupManager = serverGroupManager;
			_channelManager = channelManager;
			_tsFullClient = tsFullClient;

			_shopItems.Add(new ShopItem {
				ID = 1,
				Command = "bannermsg",
				Description = "add a message to the server banner.",
				Example = "[color=red]shop[/color] [color=green]bannermsg[/color] TS clan is the best",
				Price = 15,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 2,
				Command = "addchannel",
				Description = "additional channel",
				Example = "[color=red]shop[/color] [color=green]addchannel[/color]",
				Price = 35,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 3,
				Command = "addbanner",
				Description = "add your own banner background",
				Example = "[color=red]shop[/color] [color=green]addbanner[/color] https://imgur.com/hc/article_attachments/26512175039515.jpg",
				Price = 50,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 4,
				Command = "moveright",
				Description = "The right to move other clients {automatic after rank 8}",
				Example = "[color=red]shop[/color] [color=green]moveright[/color]",
				Price = 50,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 5,
				Command = "banright",
				Description = "The right to ban other clients {automatic after rank 25}",
				Example = "[color=red]shop[/color] [color=green]banright[/color]",
				Price = 100,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 6,
				Command = "moderator",
				Description = "Moderator rights (ban, move, elevated rights",
				Example = "[color=red]shop[/color] [color=green]moderator[/color]",
				Price = 250,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 7,
				Command = "moderatorplus",
				Description = "Moderator rights + Create and edit channels",
				Example = "[color=red]shop[/color] [color=green]moderatorplus[/color]",
				Price = 300,
			});
			_shopItems.Add(new ShopItem
			{
				ID = 8,
				Command = "administrator",
				Description = "Administrator rights",
				Example = "[color=red]shop[/color] [color=green]administrator[/color]",
				Price = 500,
			});
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
			commandManager.RegisterCommand("addsteam", HandleAddSteam);
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
				//Console.WriteLine("User not found");
				return;
			}

			// Split the message into command and argument
			string[] parts = message.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				//string errorMessage = "Too many arguments";
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtArguments"), user.ClientID, true);
				return;
			}

			// Extract the argument (language code)steamAddSuccess
			string argument = parts[1].Trim().ToLower();

			user.SteamID = argument;
			_userRepository.Update(user);
			await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "steamAddSuccess"), user.ClientID, true);
		}

		private async Task HandleSetStep(TextMessage message)
		{
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
				//Console.WriteLine("User not found");noCreditToYourself
				return;
			}

			if (user.Name == message.InvokerName)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "noCreditToYourself"), user.ClientID, true);
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
					await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notEnoughtCredits")+ " 😥", user.ClientID, true);
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
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
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
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}
			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}
			//Build Shoppage workInProgress
			string ShopPage = @$"{constants.shopHeader}

[color=red][b]{localizationManager.GetTranslation(user.CountryCode, "feature")}[/b][/color] | [color=green]{localizationManager.GetTranslation(user.CountryCode, "price")}[/color] | [i]{localizationManager.GetTranslation(user.CountryCode, "description")}[/i] - {localizationManager.GetTranslation(user.CountryCode, "example")}

{localizationManager.GetTranslation(user.CountryCode, "workInProgress")} [color=#aa4400]Your available Credits: [b]{(float)Math.Round(user.Score, 2)}[/b][/color]

";
			foreach (var shopitem in _shopItems)
			{
				ShopPage += $"● [{shopitem.ID}] [color=red][b]{shopitem.Command}[/b][/color] | [color=green][b]{shopitem.Price}[/b][/color] | [i]{shopitem.Description}[/i] - {shopitem.Example} \n";
			}
			ShopPage += "\nYou can also just use the ID istead of the command. Example: shop 2";

			// Split the message into command and argument
			string[] parts = e.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2)
			{
				// No argument provided
				//string errorMessage = "Too many arguments";
				await SendPrivateMessage(ShopPage, user.ClientID);
				return;
			}

			// Extract the argument (language code)
			string argument = parts[1].Trim().ToLower();
			string statusMessage = "";
			//Aditinal checks
			// Try to find the shop item by ID or Command
			ShopItem selectedItem = _shopItems.FirstOrDefault(item =>
				item.Command.ToLower() == argument || item.ID.ToString() == argument);


			if (selectedItem != null)
			{
				// Check user credits
				if (CheckIfUserHasEnoughtCredit(user, selectedItem.Price))
				{
					// Handle specific commands
					switch (selectedItem.Command)
					{
					case "addchannel":
						user.Score -= selectedItem.Price;
						_userRepository.Update(user);
						await CreateAdditionalChannel(user);
						await SendPrivateMessage($"You successfully bought {selectedItem.Description}. Remaining credits: {(float)Math.Round(user.Score, 2)}\n{localizationManager.GetTranslation(user.CountryCode, "addPassword")}", user.ClientID, true);
						break;
					case "bannermsg":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "addbanner":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "moveright":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "banright":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "moderator":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "moderatorplus":
						await SendComingSoon(selectedItem.Command, user);
						break;
					case "administrator":
						await SendComingSoon(selectedItem.Command, user);
						break;
					default:
						await SendPrivateMessage($"The command '{selectedItem.Command}' is recognized but not implemented.", user.ClientID, true);
						break;
					}
				}
				else
				{
					await SendPrivateMessage($"You do not have enough credits to buy {selectedItem.Description}. Your credits: {(float)Math.Round(user.Score, 2)}", user.ClientID, true);
				}
			}
			else
			{
				// Invalid command or ID
				await SendPrivateMessage($"The shop item '{argument}' does not exist. Use 'shop' to see available items.", user.ClientID, true);
			}
		}

		private async Task SendComingSoon(string item, TSUser user)
		{
			await SendPrivateMessage($"The item '{item}' is not available yet. Please check back regularly, as the feature might become available soon.", user.ClientID, true);
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
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}
			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}
			if (!user.AcceptedRules)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notAcceptedRules"), user.ClientID, true);
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
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
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
				await SendPrivateMessage(localizationManager.GetTranslation(tsCountryCode, "notValidCountryCode"), user.ClientID, true);
			}
		}

		private async Task HandleHelp(TextMessage message)
		{
			if (message.InvokerUid == null)
			{
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
			await SendPrivateMessage(@$"{constants.helpHeader}

[color=green][b]Command[/b][/color] | [color=green]<argument> optional[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "description")} [/color]

[b]•[/b]| [color=green][b]help[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendsThisHelp")}[/color]
[b]•[/b]| [color=green][b]hello/hi/sup[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendTestText")}[/color]
[b]•[/b]| [color=green][b]skipsetup[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "toSkipOnboarding")}[/color]
[b]•[/b]| [color=green][b]restart[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "restartOnboarding")}[/color]
[b]•[/b]| [color=green][b]deletemychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "deleteYourOwnChannel")}[/color]
[b]•[/b]| [color=green][b]createmychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "createYourOwnChannel")}[/color]
[b]•[/b]| [color=green][b]mychannel[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "movesIntoYourChannel")}[/color]
[b]•[/b]| [color=green][b]disablestatus[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "disableStatus")}[/color]
[b]•[/b]| [color=green][b]shop/buy[/b][/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "shopDescript")}[/color]
[b]•[/b]| [color=green][b]setlanguage[/b][/color] [color=green]<language code>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "setLanguage")} (en/de/tr/ru/ir/cz/pl/ae/pt/hu/fi)[/color]
[b]•[/b]| [color=green][b]sendcredit[/b][/color] [color=green]<amount> <username>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "sendcredit")}[/color]
[b]•[/b]| [color=green][b]addsteam[/b][/color] [color=green]<steamid>[/color] | [color=red]{localizationManager.GetTranslation(user.CountryCode, "addSteam")}[/color]
{adminHelp}", message.InvokerId);
		}

		private async Task HandleCreateMyChannel(TextMessage message)
		{
			TSUser? user = _userRepository.FindOne(message.InvokerUid.Value);
			if (user == null)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}

			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			if (!user.AcceptedRules)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notAcceptedRules"), user.ClientID, true);
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
			if (!user.AcceptedRules)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "notAcceptedRules"), user.ClientID, true);
				return;
			}
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
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}

			if (user.SkipSetup)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "skippedSetupNoChannel"), user.ClientID, true);
				return;
			}

			if (user.ChannelIDInt == 0)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "stillNoChannel"), user.ClientID, true);
			}

			if (HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, user))
			{
				var generator = new RandomNameGenerator();
				var dbuser = await _tsFullClient.GetClientDbIdFromUid(user.UserID);
				var newChannelId = await _channelManager.CreateChannel(user.Name);
				if (newChannelId.HasValue)
				{
					await _tsFullClient.ClientMove(user.ClientID, newChannelId.Value);
					await _tsFullClient.ChannelGroupAddClient((ChannelGroupId)5, newChannelId.Value, dbuser.Value.ClientDbId);
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

			if (user == null)
			{
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "contactAdmin"), user.ClientID, true);
				return;
			}

			if (user.ChannelIDInt == 0)
			{
				user.SetupDate = DateTime.UtcNow;
				user.SetupStep = 0;
				_userRepository.Update(user);
				await SendPrivateMessage(localizationManager.GetTranslation(user.CountryCode, "restartSetup"), user.ClientID, true);
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

		private async Task<bool> SendPrivateMessage(string message, ClientId client, bool format = false, bool noSpace = false)
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

		//static string AddOrUpdateUserInDatabase(string TSID, string SteamID)
		//{
		//	string response = "no response";

		//	string databaseFolderPath = "Data Source=steam_ids.db;Upgrade=true;";
		//	using (var connection = new SQLiteConnection(databaseFolderPath))
		//	{
		//		connection.Open();

		//		// Check if the user with the given Steam ID already exists
		//		using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM SteamIds WHERE steam_id = @steamId;", connection))
		//		{
		//			checkCmd.Parameters.AddWithValue("@steamId", SteamID);
		//			int count = Convert.ToInt32(checkCmd.ExecuteScalar());

		//			if (count > 0)
		//			{
		//				// User with Steam ID exists, update the TeamSpeak ID
		//				using (var updateCmd = new SQLiteCommand("UPDATE SteamIds SET teamspeak_id = @teamspeakId WHERE steam_id = @steamId;", connection))
		//				{
		//					updateCmd.Parameters.AddWithValue("@steamId", SteamID);
		//					updateCmd.Parameters.AddWithValue("@teamspeakId", TSID);
		//					updateCmd.ExecuteNonQuery();
		//					response = "user updated";
		//				}
		//			}
		//			else
		//			{
		//				// User with Steam ID doesn't exist, add a new record
		//				using (var insertCmd = new SQLiteCommand("INSERT INTO SteamIds (steam_id, teamspeak_id) VALUES (@steamId, @teamspeakId);", connection))
		//				{
		//					insertCmd.Parameters.AddWithValue("@steamId", SteamID);
		//					insertCmd.Parameters.AddWithValue("@teamspeakId", TSID);
		//					insertCmd.ExecuteNonQuery();
		//					response = "New user added";
		//				}
		//			}
		//		}
		//	}

		//	return response;
		//}

	}

	public class ShopItem
	{
		public string Command { get; set; }
		public int ID { get; set; }
		public string Example { get; set; }
		public string Description { get; set; }
		public float Price { get; set; }


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

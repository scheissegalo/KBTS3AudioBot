using Microsoft.CodeAnalysis.CSharp.Syntax;
using RankingSystem.Interfaces;
using RankingSystem.Models;
using RankingSystem.Modules;
using RankingSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TSLib;
using TSLib.Full;
using TSLib.Messages;

namespace RankingSystem
{
	internal class OnboardingModule
	{
		public readonly IUserRepository _userRepository;
		private readonly IChannelManager _channelManager;
		private readonly IServerGroupManager _serverGroupManager;
		private readonly ILocalizationManager _localizationManager;
		private readonly IUserStatusUpdater _userStatusUpdater;
		private readonly CommandManager _commandManager = new CommandManager();
		private readonly OnlineCounterModule _onlineCounterModule;
		TsFullClient _tsFullClient;

		private Constants constants = new Constants();

		public OnboardingModule(IUserRepository userRepository,
			CommandManager commandManager,
			IChannelManager channelManager,
			IServerGroupManager serverGroupManager,
			ILocalizationManager localizationManager,
			IUserStatusUpdater userStatusUpdater,
			OnlineCounterModule ocm,
			TsFullClient tsFullClient)
		{
			_userRepository = userRepository;
			_channelManager = channelManager;
			_serverGroupManager = serverGroupManager;
			_localizationManager = localizationManager;
			_userStatusUpdater = userStatusUpdater;
			_onlineCounterModule = ocm;
			_tsFullClient = tsFullClient;
			_commandManager.LoadModule(new OnboardingCommandModule(userRepository, serverGroupManager, channelManager, tsFullClient));

		}

		public void StartOnboardingModule()
		{
			_tsFullClient.OnEachTextMessage += new EventHandler<TextMessage>(OnEachTextMessage);
			_tsFullClient.OnClientEnterView += new NotifyEventHandler<ClientEnterView>(OnClientEnterView);
			Console.WriteLine("Onboarding Module Initialized");
		}

		private async void OnClientEnterView(object sender, IEnumerable<ClientEnterView> e)
		{
			await _userStatusUpdater.CheckUser();
		}

		public bool IsValidCountryCode(string countryCode)
		{
			string pattern = "^[A-Za-z]{2}$";
			return Regex.IsMatch(countryCode, pattern);
		}

		public bool HasUserSurpassedTimeThreshold(TimeSpan threshold, TSUser tsuser)
		{
			return tsuser.OnlineTime >= threshold;
		}

		public int GetUserLevel(TimeSpan onlineTime)
		{
			double totalMinutes = onlineTime.TotalMinutes;
			double num = Math.Log10(totalMinutes + 1.0);
			return (int)(num * 48);
		}

		public async Task<string> GetUserCountryCodeFromTS(TSUser tsuser)
		{
			R<ClientInfo, CommandError> fulluser = await _tsFullClient.ClientInfo(tsuser.ClientID);
			if (fulluser.Ok)
			{
				return fulluser.Value.CountryCode;
			}
			return "en";
		}

		public string FormatTimeSpan(TimeSpan timeSpan)
		{
			List<string> list = new List<string>();
			if (timeSpan.Days >= 365)
			{
				int num = timeSpan.Days / 365;
				list.Add($"{num} year{((num > 1) ? "s" : "")}");
			}
			if (timeSpan.Days % 365 > 0)
			{
				int num2 = timeSpan.Days % 365;
				list.Add($"{num2} day{((num2 > 1) ? "s" : "")}");
			}
			if (timeSpan.Hours > 0)
			{
				list.Add($"{timeSpan.Hours} hour{((timeSpan.Hours > 1) ? "s" : "")}");
			}
			if (timeSpan.Minutes > 0)
			{
				list.Add($"{timeSpan.Minutes} minute{((timeSpan.Minutes > 1) ? "s" : "")}");
			}
			if (timeSpan.Seconds > 0)
			{
				list.Add($"{timeSpan.Seconds} second{((timeSpan.Seconds > 1) ? "s" : "")}");
			}
			Console.WriteLine($"Result: {string.Join(" ", list)} | Orig: {timeSpan}");
			return string.Join(" ", list);
		}

		private async void OnEachTextMessage(object sender, TextMessage e)
		{
			if (_tsFullClient.ClientId == e.InvokerId)
			{
				return;
			}
			TSUser user = _userRepository.FindOne(e.InvokerUid.Value);
			string userCountryCode;
			if (user == null)
			{
				await _tsFullClient.SendPrivateMessage("Something went wrong! Please contact an admin!", e.InvokerId);
			}
			else
			{
				if (await _commandManager.TryHandleCommand(e))
				{
					return;
				}
				string TSCountryCode = await GetUserCountryCodeFromTS(user);
				userCountryCode = user.CountryCode;
				string localizedYes = _localizationManager.GetTranslation(userCountryCode, "yes").ToLower();
				string localizedNo = _localizationManager.GetTranslation(userCountryCode, "no").ToLower();
				switch ((SetupStep)user.SetupStep)
				{
				case SetupStep.Welcome:
					if (userCountryCode != "en")
					{
						string message = _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage");
						await _tsFullClient.SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "hello")} {user.Name}!\n {message}", user.ClientID);
						message = _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
						await _tsFullClient.SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "skipSetup")}\n{message} [b]{TSCountryCode}[/b]!", user.ClientID);
					}
					else
					{
						if (TSCountryCode != "en")
						{
							string messageEN = _localizationManager.GetTranslation("en", "welcomeMessage");
							await _tsFullClient.SendPrivateMessage($"Backup English: {_localizationManager.GetTranslation("en", "hello")} {user.Name}!\n {messageEN}", user.ClientID);
						}
						string message = _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage");
						await _tsFullClient.SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "hello")} {user.Name}!\n {message}", user.ClientID);
						if (TSCountryCode != "en")
						{
							string messageEN = _localizationManager.GetTranslation("en", "whatIsYourLanguage");
							await _tsFullClient.SendPrivateMessage($"Backup English:{_localizationManager.GetTranslation("en", "skipSetup")}\n{messageEN} [b]{TSCountryCode}[/b]!", user.ClientID);
						}
						message = _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
						await _tsFullClient.SendPrivateMessage($"{_localizationManager.GetTranslation(TSCountryCode, "skipSetup")}\n{message} [b]{TSCountryCode}[/b]!", user.ClientID);
					}
					user.SetupStep = 1;
					break;
				case SetupStep.AskPreferredLanguage:
					{
						if (IsValidCountryCode(e.Message))
						{
							user.CountryCode = e.Message;
							_userRepository.Update(user);
							user = _userRepository.FindById(user);
							userCountryCode = user.CountryCode;
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "acceptRules") ?? "", user.ClientID);
							user.SetupStep = 2;
							break;
						}
						if (TSCountryCode != "en")
						{
							string messageEN = _localizationManager.GetTranslation("en", "whatIsYourLanguage");
							await _tsFullClient.SendPrivateMessage("Backup English: " + messageEN + " !\n " + _localizationManager.GetTranslation("en", "skipSetup"), user.ClientID);
						}
						string message = _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
						await _tsFullClient.SendPrivateMessage($"{message} {TSCountryCode}!\n {_localizationManager.GetTranslation(TSCountryCode, "skipSetup")}", user.ClientID);
						await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(TSCountryCode, "notValidCountryCode") + "]", user.ClientID);
						break;
					}
				case SetupStep.AcceptRules:
					{
						Console.WriteLine("Translated yes: " + localizedYes + ", No: " + localizedNo);
						string answer = e.Message.ToLower();
						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.AcceptedRules = answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);
							_userRepository.Update(user);
							user = _userRepository.FindById(user);
							user.SetupStep = 3;
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "rankingDisabled") ?? "", user.ClientID);
						}
						else
						{
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "acceptRules") ?? "", user.ClientID);
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo"), user.ClientID);
						}
						break;
					}
				case SetupStep.AskRankingPreference:
					{
						string answer = e.Message.ToLower();
						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.RankingEnabled = answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);
							user.SetupStep = 4;
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "yourOwnChannel") ?? "", user.ClientID);
						}
						else
						{
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "rankingDisabled") ?? "", user.ClientID);
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo") ?? "", user.ClientID);
						}
						break;
					}
				case SetupStep.AskChannelPreference:
					{
						string answer = e.Message.ToLower();
						if (answer.Equals(localizedYes, StringComparison.OrdinalIgnoreCase) || answer.Equals(localizedNo, StringComparison.OrdinalIgnoreCase))
						{
							user.WantsOwnChannel = e.Message.Equals(localizedYes, StringComparison.OrdinalIgnoreCase);
							user.SetupDone = true;
							user.SetupStep = 5;
							user.SkipSetup = false;
							_userRepository.Update(user);
							user = _userRepository.FindById(user);
							R<ClientDbIdFromUid, CommandError> dbuser = await _tsFullClient.GetClientDbIdFromUid(user.UserID);
							await _tsFullClient.ServerGroupAddClient(constants.memberGroup, dbuser.Value.ClientDbId);
							if (user.WantsOwnChannel)
							{
								if (!HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, user))
								{
									await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "notEnoughTime"), user.ClientID);
								}
								else
								{
									ChannelId? newChannelId = await _channelManager.CreateChannel(user.Name);
									await _tsFullClient.ClientMove(user.ClientID, newChannelId.Value);
									await _tsFullClient.ChannelGroupAddClient((ChannelGroupId)5uL, newChannelId.Value, dbuser.Value.ClientDbId);
									await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "addPassword"), user.ClientID);
									user.ChannelIDInt = newChannelId.Value.Value;
									user.ChannelID = newChannelId.Value;
									_userRepository.Update(user);
								}
							}
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "setupComplete") ?? "", user.ClientID);
						}
						else
						{
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "yourOwnChannel") ?? "", user.ClientID);
							await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "onlyYesOrNo") ?? "", user.ClientID);
						}
						break;
					}
				case SetupStep.Completed:
					{
						string acceptedRules = TranslateBool(user.AcceptedRules);
						string rankingDisabled = TranslateBool(user.RankingEnabled);
						string setupDone = TranslateBool(user.SetupDone);
						string skippedSetup = TranslateBool(user.SkipSetup);
						string hasOwnChannel = TranslateBool(user.WantsOwnChannel);
						string ChannelString;
						if (user.WantsOwnChannel && user.ChannelIDInt != 0)
						{
							R<ChannelInfoResponse[], CommandError> chanInfo = await _tsFullClient.ChannelInfo((ChannelId)user.ChannelIDInt);
							ChannelString = $"[color=green]Your Channel[/color][b]: [color=red]{chanInfo.Value[0].Name} ({user.ChannelIDInt})[/color][/b]";
						}
						else
						{
							ChannelString = "No own Channel";
						}
						await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(userCountryCode, "welcomeBack") + " " + user.Name + "!", user.ClientID);
						await SendPrivateMessage(@$"[b][color=blue]Server Statistics[/color][/b]  
[b]•[/b] Users Online Today: [color=green]{_onlineCounterModule.countToday} [/color]  
[b]•[/b] Current Online Users: [color=#0044aa]{_onlineCounterModule.count}[/color]  

[b][color=blue]═══════════- Personal Daily Status -══════════[/color][/b] 

[b]•[/b] Username: [color=#aa4400]{user.Name}[/color]  
[b]•[/b] Online Time: [color=#00FF00]{FormatTimeSpan(user.OnlineTime)}[/color]  
[b]•[/b] Credits: [color=#00FF00]{user.Score}[/color]  
[b]•[/b] Level: [color=cyan]{GetUserLevel(user.OnlineTime)}[/color]  
[b]•[/b] Channel: [color=white]{ChannelString} [/color]",user.ClientID);
//						await _tsFullClient.SendPrivateMessage(@$"
//******** [b][color=#24336b]North[/color][color=#0095db]Industries[/color][/b] ********
//--- Stats --- | --- Stats --- | --- Stats ---
//[b]{_localizationManager.GetTranslation(userCountryCode, "userStats")}:[/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "countryCode")}[/color][b]: [color=red]{user.CountryCode}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "rulesAccepted")}[/color][b]: [color=red]{acceptedRules}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "rankingEnabled")}[/color][b]: [color=red]{rankingDisabled}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "score")}[/color][b]: [color=red]{user.Score}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "OnlineTime")}[/color][b]: [color=red]{FormatTimeSpan(user.OnlineTime)}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "setupDone")}[/color][b]: [color=red]{setupDone}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "skippedSetup")}[/color][b]: [color=red]{skippedSetup}[/color][/b]
//[color=green]{_localizationManager.GetTranslation(userCountryCode, "level")}[/color][b]: [color=red]{GetUserLevel(user.OnlineTime)}[/color][/b]
//{ChannelString}\n[color=green]{_localizationManager.GetTranslation(userCountryCode, "ownChannel")}[/color][b]: [color=red]{hasOwnChannel}[/color][/b]

//{_localizationManager.GetTranslation(user.CountryCode, "typeHelp")}", user.ClientID);
						break;
					}
				default:
					await _tsFullClient.SendPrivateMessage("Something went wrong with your setup.", user.ClientID);
					break;
				}
				_userRepository.Update(user);
			}
			string TranslateBool(bool condition)
			{
				return _localizationManager.GetTranslation(userCountryCode, condition ? "yes" : "no");
			}
		}

		public async Task SendPrivateMessage(string message, ClientId user)
		{
			//Console.WriteLine($"Sending message {message}");
			string newMessage = $"{constants.messageHeader} {message} {constants.messageFooter}";
			var response = await _tsFullClient.SendPrivateMessage(newMessage, user);
		}


		public void StopOnboardingModule()
		{
			_tsFullClient.OnEachTextMessage -= new EventHandler<TextMessage>(OnEachTextMessage);
		}
	}

	public enum SetupStep
	{
		Welcome,
		AskPreferredLanguage,
		AcceptRules,
		AskRankingPreference,
		AskChannelPreference,
		Completed
	}

	public class RandomNameGenerator
	{
		private static readonly List<string> ThreeLetterWords = new List<string>
	{
		"ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO", "FOXTROT", "GOLF", "HOTEL", "INDIA", "HERO",
		"CLAN", "DUEL", "RUN", "HP", "XP", "DPS", "GUNS", "LIMA", "BLIZ", "TANGO",
		"RACE", "TEAM", "WAR", "ZONE", "RELOAD", "GUN", "CHARGE", "MED", "SQUAD", "INFANTRY",
		"ARENA", "FORT", "BATTALION", "COMPANY", "WIN", "SQUAD"
	};

		private static readonly Random random = new Random();

		public string GenerateRandomName()
		{
			int index = random.Next(ThreeLetterWords.Count);
			return ThreeLetterWords[index];
		}
	}

	public class ServerGroupInfo
	{
		public TimeSpan OnlineTimeThreshold { get; set; }
		public ServerGroupId ServerGroup { get; set; }
	}
}

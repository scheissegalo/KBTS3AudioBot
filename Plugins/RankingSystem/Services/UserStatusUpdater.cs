using System;
using System.Linq;
using System.Threading.Tasks;
using TSLib.Full;
using RankingSystem.Interfaces;
using TSLib;
using RankingSystem.Models;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace RankingSystem.Services
{
	public class UserStatusUpdater : IUserStatusUpdater
	{
		private readonly IUserRepository _userRepository;
		private readonly IServerGroupManager _serverGroupManager;
		private readonly IChannelManager _channelManager;
		private readonly ILocalizationManager _localizationManager;
		private readonly TsFullClient _tsFullClient;
		private readonly Constants constants = new Constants();
		private bool isChecking = false;

		public UserStatusUpdater(
			IUserRepository userRepository,
			IServerGroupManager serverGroupManager,
			IChannelManager channelManager,
			ILocalizationManager localizationManager,
			TsFullClient tsFullClient
		)
		{
			_userRepository = userRepository;
			_serverGroupManager = serverGroupManager;
			_tsFullClient = tsFullClient;
			_channelManager = channelManager;
			_localizationManager = localizationManager;
		}

		public async Task CheckUser()
		{
			if (isChecking)
			{
				return;
			}
			if (_userRepository == null)
			{
				return;
			}
			isChecking = true;
			try
			{
				var allUsers = await _tsFullClient.ClientList();

				foreach (var user in allUsers.Value)
				{

					//Check if user is not a query user
					if (user.ClientType.Equals(ClientType.Full))
					{
						var fulluser = await _tsFullClient.ClientInfo(user.ClientId);
						// Check if is a bot
						ServerGroupId[] userServerGroups = fulluser.Value.ServerGroups;
						if (!userServerGroups.Any(sg => constants.BotGroupsE.Contains(sg)))
						{
							//Console.WriteLine($"User {user.Name} {fulluser.Value.Uid} has ServerGroups: {String.Join(",", fulluser.Value.ServerGroups)}");

							//If user exists in database
							if (_userRepository.FindOne(fulluser.Value.Uid) == null)
							{
								// User does not exist, so add them to the database

								//DBusers.Insert(user);
								TSUser tSUser = new TSUser
								{
									UserID = fulluser.Value.Uid,
									Name = fulluser.Value.Name,
									ClientID = user.ClientId,
									LastUpdate = DateTime.Now,
									OnlineTime = TimeSpan.Zero

								};
								_userRepository.Insert(tSUser);
								if (constants.onboardingEnabled)
								{
									await StartUserOnboarding(tSUser);
								}
								//Console.WriteLine("User Created");

							}
							else
							{
								//Console.WriteLine("User Found");

								int channelUserCount = await _channelManager.GetUserCountFromChannelId(fulluser.Value.ChannelId);
								TSUser? tsuser = _userRepository.FindOne(fulluser.Value.Uid);
								if (tsuser != null)
								{

									//check if username is null or not eqal to current, then add actual name
									if (tsuser.Name != fulluser.Value.Name || tsuser.Name == "" || tsuser.Name == null)
									{
										var username = fulluser.Value.Name;
										if (username.Length > 0)
										{
											tsuser.Name = username;
											_userRepository.Update(tsuser);
										}
									}

									//Console.WriteLine("User Found and not null");
									// Check for notifications
									if (tsuser.ClientID != user.ClientId)
									{
										//Console.WriteLine("ClientID Updated!");
										tsuser.ClientID = user.ClientId;
										_userRepository.Update(tsuser);
									}

									if (!tsuser.NotificationSend && constants.SendDaylyMessage)
									{
										TimeSpan timeDiff = tsuser.LastNotification - DateTime.Now;
										// Check if we are within 12 hours (positive or negative)
										if (Math.Abs(timeDiff.TotalHours) <= 12)
										{
											//Console.WriteLine("We are within 12 hours of the target date.");
										}
										else
										{
											//Console.WriteLine("We are more than 12 hours away from the target date.");

											if (tsuser.SetupDone && tsuser.DailyStatusEnabled)
											{
												string TranslateBool(bool condition) => _localizationManager.GetTranslation(tsuser.CountryCode, condition ? "yes" : "no");
												string localizedYes = _localizationManager.GetTranslation(tsuser.CountryCode, "yes").ToLower();
												string localizedNo = _localizationManager.GetTranslation(tsuser.CountryCode, "no").ToLower();

												string acceptedRules = TranslateBool(tsuser.AcceptedRules);
												string rankingDisabled = TranslateBool(tsuser.RankingEnabled);
												string setupDone = TranslateBool(tsuser.SetupDone);
												string skippedSetup = TranslateBool(tsuser.SkipSetup);
												string hasOwnChannel = TranslateBool(tsuser.WantsOwnChannel);
												//yourTime disableStatusMessage
												await _tsFullClient.SendPrivateMessage(@$"
******* [b][color=#24336b]North[/color][color=#0095db]Industries - {_localizationManager.GetTranslation(tsuser.CountryCode, "dailyPersonalStatus")}[/color][/b] *******
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "user")}[/color] : {tsuser.Name} | [color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "level")}[/color][b]: [color=red]{GetUserLevel(tsuser.OnlineTime)}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "OnlineTime")}[/color][b]: [color=red]{FormatTimeSpan(tsuser.OnlineTime)}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "score")}[/color][b]: [color=red]{tsuser.Score}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "rulesAccepted")}[/color][b]: [color=red]{acceptedRules}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "rankingEnabled")}[/color][b]: [color=red]{rankingDisabled}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "setupDone")}[/color][b]: [color=red]{setupDone}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "skippedSetup")}[/color][b]: [color=red]{skippedSetup}[/color][/b]
[color=green]{_localizationManager.GetTranslation(tsuser.CountryCode, "ownChannel")}[/color][b]: [color=red]{hasOwnChannel}[/color][/b]

{_localizationManager.GetTranslation(tsuser.CountryCode, "disableStatusMessage")}

", tsuser.ClientID);
												tsuser.NotificationSend = true;
												tsuser.LastNotification = DateTime.Now;
												_userRepository.Update(tsuser);
											}
											else
											{
												//send to own channel etc
												await _tsFullClient.SendPrivateMessage("Hey you have still not Completed the onboarding!", tsuser.ClientID);
												tsuser.NotificationSend = true;
												tsuser.LastNotification = DateTime.Now;
												_userRepository.Update(tsuser);
											}

										}
									}

									if (!tsuser.RankingEnabled)
									{
										continue;
									}

									//check if user over 30 min and wants a channel
									if (HasUserSurpassedTimeThreshold(constants.timeToAllowChannelCreation, tsuser) && tsuser.WantsOwnChannel && tsuser.ChannelIDValue != 0 && !tsuser.WantsOwnChannelNotificationSend)
									{
										await _tsFullClient.SendPrivateMessage(_localizationManager.GetTranslation(tsuser.CountryCode, "enoughTime"), tsuser.ClientID);
										tsuser.WantsOwnChannelNotificationSend = true;
										_userRepository.Update(tsuser);
									}

									if (fulluser.Value.ChannelId == constants.AfkChannel)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in AFK Channel");
										continue;
									}

									if (fulluser.Value.ChannelId == (ChannelId)1)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in SPAWN Channel");
										continue;
									}

									if (fulluser.Value.ServerGroups.Contains(constants.NoAfkGroup))
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} Has NoAFK");
										continue;
									}

									if (channelUserCount < 2)
									{
										//Console.WriteLine($"User: {fulluser.Value.Name} is in a channel with less than 2 clients. Count{channelUserCount}");
										continue;
									}

									DateTime currentTime = DateTime.Now;

									// Calculate the difference between the current time and the last update time
									TimeSpan timeDifference = currentTime - tsuser.LastUpdate;

									// Check if the difference is within 5 minutes
									if (timeDifference.TotalMinutes <= 3 && timeDifference.TotalMinutes >= 0)
									{
										//Console.WriteLine("The last update was within the last 5 minutes. Counting user");

										tsuser.OnlineTime += (DateTime.Now - tsuser.LastUpdate);
										tsuser.LastUpdate = DateTime.Now;
										tsuser.ClientID = user.ClientId;
										tsuser.Score += constants.ScorePerTick;

										// Save the updated timestamp back to the database
										_userRepository.Update(tsuser);
									}
									else
									{
										tsuser.LastUpdate = DateTime.Now;
										tsuser.ClientID = user.ClientId;
										_userRepository.Update(tsuser);
										//Console.WriteLine("The last update was more than 5 minutes ago. set user for counting");
									}

									//Console.WriteLine($"User {tsuser.Name} Credits: {tsuser.Score}. Last Checked: {tsuser.LastUpdate.ToString("HH:mm:ss")} Online Time: {tsuser.OnlineTime}");
								}
								//Checking for adding or removing group
								var userGroups = await _tsFullClient.ServerGroupsByClientDbId(fulluser.Value.DatabaseId);

								// Get the current group(s) the user is in
								var currentGroups = userGroups.Value.Select(g => g.ServerGroupId).ToList();

								// Get the new group based on the online time
								ServerGroupId newGroup = GetServerGroup(tsuser.OnlineTime);

								// Check if the user is not in the correct group
								if (!currentGroups.Contains(newGroup))
								{
									//Console.WriteLine($"User {tsuser.Nickname} does not have the right group");

									// Get a list of the groups the user should be in based on their online time
									var groupsToRemove = currentGroups
										.Where(group => constants._serverGroupList.Any(sg => sg.ServerGroup == group))
										.ToList();

									// Remove the user from their old group(s) that are in _serverGroupList but not the correct group
									foreach (var group in groupsToRemove)
									{
										if (group != newGroup)  // Only remove if it's not the new group
										{
											await _tsFullClient.ServerGroupDelClient(group, fulluser.Value.DatabaseId);
											//Console.WriteLine($"User {tsuser.Nickname} removed from group {group}");
										}
									}

									// Add the user to the new group if they're not already in it
									if (!currentGroups.Contains(newGroup))
									{
										await _tsFullClient.ServerGroupAddClient(newGroup, fulluser.Value.DatabaseId);
										//Console.WriteLine($"User {tsuser.Nickname} added to new group {newGroup}");
									}
								}
								else
								{
									//Console.WriteLine($"User {tsuser.Nickname} is already in the correct group.");
								}
							}
						}
					}
				}

				if (_userRepository != null)
				{
					//DBusers.Insert(user);
					//Console.WriteLine("Access to database!");
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			isChecking = false;
		}

		public int GetUserLevel(TimeSpan onlineTime)
		{
			// Convert online time to total days
			double days = onlineTime.TotalMinutes;

			// You can tweak these values as necessary
			// Let's assume 10 days for Level 1, 50 days for Level 2, and so on, but increasing less and less.
			double level = Math.Log10(days + 1);  // +1 to avoid log(0)

			// To make sure levels start from 0 and scale well, we can multiply by a constant, for example 5
			int userLevel = (int)(level * 5);

			return userLevel;
		}

		public string FormatTimeSpan(TimeSpan timeSpan)
		{
			var parts = new List<string>();

			if (timeSpan.Days >= 365)
			{
				int years = timeSpan.Days / 365;
				parts.Add($"{years} year{(years > 1 ? "s" : "")}");
			}

			if (timeSpan.Days % 365 > 0)
			{
				int remainingDays = timeSpan.Days % 365;
				parts.Add($"{remainingDays} day{(remainingDays > 1 ? "s" : "")}");
			}

			if (timeSpan.Hours > 0)
			{
				parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")}");
			}

			if (timeSpan.Minutes > 0)
			{
				parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}");
			}

			if (timeSpan.Seconds > 0)
			{
				parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}");
			}
			Console.WriteLine($"Result: {string.Join(" ", parts)} | Orig: {timeSpan}");
			return string.Join(" ", parts);
		}

		public ServerGroupId GetServerGroup(TimeSpan onlineTime)
		{
			foreach (var serverGroupInfo in constants._serverGroupList)
			{
				if (onlineTime < serverGroupInfo.OnlineTimeThreshold)
				{
					return serverGroupInfo.ServerGroup;
				}
			}

			// If the online time exceeds all thresholds, return the last server group in the list
			Console.WriteLine("Online Time Exceedet");
			return constants._serverGroupList.Last().ServerGroup;
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

		public async Task StartUserOnboarding(TSUser tsUser)
		{
			string message;
			//string answer;
			string TSCountryCode = await GetUserCountryCodeFromTS(tsUser);

			//Send english Backup incase user is on VPN
			if (TSCountryCode != "en")
			{
				string messageEN = _localizationManager.GetTranslation("en", "welcomeMessage");
				await _tsFullClient.SendPrivateMessage($"Backup English: \n{_localizationManager.GetTranslation("en", "hello")} {tsUser.Name}!\n {messageEN}\n", tsUser.ClientID);
			}
			message = _localizationManager.GetTranslation(TSCountryCode, "welcomeMessage");
			await _tsFullClient.SendPrivateMessage($"\n{_localizationManager.GetTranslation(TSCountryCode, "hello")} {tsUser.Name}!\n {message}\n", tsUser.ClientID);

			if (TSCountryCode != "en")
			{
				string messageEN = _localizationManager.GetTranslation("en", "whatIsYourLanguage");
				await _tsFullClient.SendPrivateMessage($"\nBackup English:{_localizationManager.GetTranslation("en", "skipSetup")}\n{messageEN} [b]{TSCountryCode}[/b]!", tsUser.ClientID);
			}
			message = _localizationManager.GetTranslation(TSCountryCode, "whatIsYourLanguage");
			await _tsFullClient.SendPrivateMessage($"\n{_localizationManager.GetTranslation(TSCountryCode, "skipSetup")}\n{message} [b]{TSCountryCode}[/b]!", tsUser.ClientID);

			//Send user to Select Language step
			tsUser.SetupStep = (int)SetupStep.AskPreferredLanguage;
			_userRepository.Update(tsUser);
		}

		public bool HasUserSurpassedTimeThreshold(TimeSpan threshold, TSUser tsuser)
		{
			return tsuser.OnlineTime >= threshold;
		}

		//public async Task UpdateUsers()
		//{
		//	//Console.WriteLine("Updating users...");

		//	var allUsers = await _tsFullClient.ClientList();

		//	foreach (var user in allUsers.Value)
		//	{
		//		// Skip query clients
		//		if (user.ClientType != ClientType.Full) continue;

		//		var fullUser = await _tsFullClient.ClientInfo(user.ClientId);

		//		if (fullUser.Value == null) continue;

		//		// Check if the user exists in the database
		//		TSUser? tsUser = _userRepository.FindOne(fullUser.Value.Uid);
		//		if (tsUser == null)
		//		{
		//			// Add new user
		//			tsUser = new TSUser
		//			{
		//				UserID = fullUser.Value.Uid,
		//				Name = fullUser.Value.Name,
		//				ClientID = user.ClientId,
		//				LastUpdate = DateTime.Now,
		//				OnlineTime = TimeSpan.Zero
		//			};
		//			_userRepository.Insert(tsUser);
		//			//Console.WriteLine($"New user added: {tsUser.Name}");
		//		}
		//		else
		//		{
		//			// Update existing user
		//			tsUser.LastUpdate = DateTime.Now;
		//			tsUser.isOnline = true;

		//			var timeSinceLastUpdate = DateTime.Now - tsUser.LastUpdate;
		//			if (timeSinceLastUpdate.TotalMinutes > 5)
		//			{
		//				tsUser.isOnline = false;
		//			}

		//			_userRepository.Update(tsUser);
		//			Console.WriteLine($"User updated: {tsUser.Name}");
		//		}
		//	}

		//	Console.WriteLine("User updates complete.");
		//}
	}
}
